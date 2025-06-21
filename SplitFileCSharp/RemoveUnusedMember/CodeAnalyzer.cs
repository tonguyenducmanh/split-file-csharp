using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;

namespace RemoveUnusedMember
{
    public class CodeAnalyzer
    {
        public async Task<List<UnusedMemberInfo>> AnalyzeProjectAsync(string directoryPath, AdhocWorkspace workspace, IProgress<string> progress, bool onlyMethods)
        {
            progress.Report("Bắt đầu quá trình phân tích..." + (onlyMethods ? " (chỉ phương thức)" : ""));
            var unusedMembers = new List<UnusedMemberInfo>();
            var csharpFiles = Directory.GetFiles(directoryPath, "*.cs", SearchOption.AllDirectories);

            if (!csharpFiles.Any())
            {
                progress.Report("Không tìm thấy tệp C# nào để phân tích.");
                return unusedMembers;
            }

            progress.Report($"Tìm thấy {csharpFiles.Length} tệp C# trong thư mục: {directoryPath}");
            int filesProcessedCount = 0;

            var projectId = ProjectId.CreateNewId(debugName: "AnalysisProject");
            var documents = new List<Document>();
            var syntaxTrees = new List<SyntaxTree>();

            // Tạo một project ảo trong AdhocWorkspace
            var projectInfo = ProjectInfo.Create(projectId, VersionStamp.Create(), "AnalysisProject", "AnalysisProject", LanguageNames.CSharp)
                .WithMetadataReferences(new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) }); // Thêm mscorlib
            var project = workspace.AddProject(projectInfo);

            foreach (var filePath in csharpFiles)
            {
                try
                {
                    progress.Report($"Đang đọc tệp: {Path.GetFileName(filePath)}");
                    string fileContent = await File.ReadAllTextAsync(filePath);
                    var sourceText = SourceText.From(fileContent, System.Text.Encoding.UTF8);
                    var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, path: filePath);
                    syntaxTrees.Add(syntaxTree);

                    var documentId = DocumentId.CreateNewId(project.Id, debugName: filePath);
                    var document = workspace.AddDocument(project.Id, filePath, sourceText);
                    documents.Add(document);
                    project = document.Project; // Lấy project đã được cập nhật với document mới
                }
                catch (IOException ex) // Bắt lỗi cụ thể hơn
                {
                    progress.Report($"Lỗi IO khi đọc tệp {filePath}: {ex.Message}");
                }
                catch (Exception ex) // Bắt các lỗi khác
                {
                    progress.Report($"Lỗi khi xử lý tệp {filePath}: {ex.Message}");
                }
            }
            
            progress.Report("Đã đọc xong các tệp. Bắt đầu tạo compilation...");
            var compilation = await project.GetCompilationAsync();
            if (compilation == null)
            {
                progress.Report("Không thể tạo compilation từ các tệp đã cung cấp.");
                return unusedMembers;
            }
            progress.Report("Compilation đã được tạo.");

            var diagnostics = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            if (diagnostics.Any())
            {
                progress.Report($"Có {diagnostics.Count} lỗi trong quá trình compilation. Kết quả phân tích có thể không chính xác:");
                foreach (var diag in diagnostics.Take(10)) 
                {
                    progress.Report($"- {diag.GetMessage()} (tại {diag.Location.GetLineSpan().Path ?? "unknown file"} dòng {diag.Location.GetLineSpan().StartLinePosition.Line + 1})");
                }
                 if (diagnostics.Count > 10) progress.Report("...");
            }

            progress.Report("Bắt đầu phân tích semantic từng tệp...");
            foreach (var documentInProject in project.Documents) 
            {
                SyntaxTree tree = null;
                SemanticModel semanticModel = null;
                filesProcessedCount++;
                string currentFileName = Path.GetFileName(documentInProject.FilePath);
                progress.Report($"Đang phân tích tệp {filesProcessedCount}/{csharpFiles.Length}: {currentFileName}");

                try
                {
                    tree = await documentInProject.GetSyntaxTreeAsync();
                    if (tree == null)
                    {
                        progress.Report($"Không thể lấy SyntaxTree cho tệp: {currentFileName}. Bỏ qua.");
                        continue;
                    }

                    semanticModel = await documentInProject.GetSemanticModelAsync();
                    if (semanticModel == null)
                    {
                        progress.Report($"Không thể lấy SemanticModel cho tệp: {currentFileName}. Bỏ qua.");
                        continue;
                    }

                    var root = await tree.GetRootAsync();
                    var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

                    foreach (var classDecl in classDeclarations)
                    {
                        var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);
                        if (classSymbol == null) continue;

                        // Analyze Methods - always do this part
                        var methods = classDecl.Members.OfType<MethodDeclarationSyntax>();
                        foreach (var methodSyntax in methods)
                        {
                            var methodSymbol = semanticModel.GetDeclaredSymbol(methodSyntax);
                            if (methodSymbol == null || methodSymbol.MethodKind == MethodKind.StaticConstructor || methodSymbol.MethodKind == MethodKind.Destructor) continue;

                            // Chỉ xét các private methods (hoặc protected nếu cần sau này)
                            if (methodSymbol.DeclaredAccessibility == Microsoft.CodeAnalysis.Accessibility.Private)
                            {
                                // Sử dụng SymbolFinder để tìm các tham chiếu đến method này
                                // Cần Solution để SymbolFinder hoạt động hiệu quả nhất.
                                // Với AdhocWorkspace, việc tìm tham chiếu có thể bị giới hạn trong compilation hiện tại.
                                var references = await SymbolFinder.FindReferencesAsync(methodSymbol, workspace.CurrentSolution);
                                bool isReferenced = false;
                                foreach (var reference in references)
                                {
                                    if (reference.Locations.Any()) // Chỉ cần có bất kỳ vị trí tham chiếu nào
                                    {
                                        isReferenced = true;
                                        break;
                                    }
                                }
                                
                                // Kiểm tra thêm nếu method là một phần của một explicit interface implementation
                                if (methodSymbol.ExplicitInterfaceImplementations.Any())
                                { 
                                    isReferenced = true; // Coi như được sử dụng nếu là explicit interface implementation
                                }

                                // Kiểm tra các thuộc tính đặc biệt (ví dụ: event handlers được gán trong designer.cs)
                                // Đây là một điểm cần cải thiện, có thể cần phân tích các tệp designer hoặc quy ước đặt tên.
                                if (methodSyntax.AttributeLists.SelectMany(al => al.Attributes).Any(attr => attr.Name.ToString().Contains("EventHandler")))
                                {
                                    // Heuristic: Nếu có attribute giống event handler, tạm coi là được dùng
                                    // isReferenced = true;
                                }

                                if (!isReferenced)
                                {
                                    var lineSpan = tree.GetLineSpan(methodSyntax.Span);
                                    string accessibility = methodSymbol.DeclaredAccessibility.ToString();
                                    if (methodSymbol.IsOverride) accessibility += " (override)";
                                    if (methodSymbol.IsStatic) accessibility += " (static)";
                                    // Consider other modifiers like abstract, virtual, sealed as needed

                                    unusedMembers.Add(new UnusedMemberInfo(
                                        MemberType.Method,
                                        $"{classSymbol.Name}.{methodSymbol.Name}",
                                        tree.FilePath,
                                        lineSpan.StartLinePosition.Line + 1,
                                        classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                                        accessibility
                                    ));
                                    progress.Report($"Phát hiện method không sử dụng: {accessibility} {classSymbol.Name}.{methodSymbol.Name} tại {Path.GetFileName(tree.FilePath)} dòng {lineSpan.StartLinePosition.Line + 1}");
                                }
                            }
                        }

                        // Conditional analysis for other member types
                        if (!onlyMethods)
                        {
                            // Analyze Fields
                        var fields = classDecl.Members.OfType<FieldDeclarationSyntax>();
                        foreach (var fieldSyntax in fields)
                        {
                            foreach (var variableDeclarator in fieldSyntax.Declaration.Variables)
                            {
                                var fieldSymbol = semanticModel.GetDeclaredSymbol(variableDeclarator) as IFieldSymbol;
                                if (fieldSymbol == null) continue;

                                // Chỉ phân tích private fields, có thể mở rộng sau
                                if (fieldSymbol.DeclaredAccessibility == Microsoft.CodeAnalysis.Accessibility.Private)
                                {
                                    var references = await SymbolFinder.FindReferencesAsync(fieldSymbol, workspace.CurrentSolution);
                                    bool isReferenced = references.Any(r => r.Locations.Any());

                                    if (fieldSymbol.GetAttributes().Any(attr => 
                                        attr.AttributeClass?.Name.Contains("SerializedField") == true || 
                                        attr.AttributeClass?.Name.Contains("Inject") == true ||
                                        attr.AttributeClass?.Name.Contains("NonSerialized") == true ))
                                    {
                                        isReferenced = true; 
                                    }

                                    if (!isReferenced)
                                    {
                                        var lineSpan = tree.GetLineSpan(variableDeclarator.Span);
                                        string accessibility = fieldSymbol.DeclaredAccessibility.ToString();
                                        if (fieldSymbol.IsStatic) accessibility += " (static)";
                                        if (fieldSymbol.IsReadOnly) accessibility += " (readonly)";
                                        if (fieldSymbol.IsConst) accessibility = "const"; // Const implies static

                                        unusedMembers.Add(new UnusedMemberInfo(
                                            MemberType.Field,
                                            $"{classSymbol.Name}.{fieldSymbol.Name}",
                                            tree.FilePath,
                                            lineSpan.StartLinePosition.Line + 1,
                                            classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                                            accessibility
                                        ));
                                            progress.Report($"Phát hiện field không sử dụng: {accessibility} {fieldSymbol.Name} ({fieldSymbol.Type}) tại {Path.GetFileName(tree.FilePath)} dòng {lineSpan.StartLinePosition.Line + 1}");
                                    }
                                }
                            }
                        }

                            // Analyze Properties
                            var properties = classDecl.Members.OfType<PropertyDeclarationSyntax>();
                            foreach (var propertySyntax in properties)
                            {
                                var propertySymbol = semanticModel.GetDeclaredSymbol(propertySyntax);
                                if (propertySymbol == null) continue;

                                if (propertySymbol.DeclaredAccessibility == Microsoft.CodeAnalysis.Accessibility.Private)
                                {
                                    var references = await SymbolFinder.FindReferencesAsync(propertySymbol, workspace.CurrentSolution);
                                    bool isReferenced = references.Any(r => r.Locations.Any());

                                    if (!isReferenced)
                                    {
                                        var lineSpan = tree.GetLineSpan(propertySyntax.Span);
                                        string accessibility = propertySymbol.DeclaredAccessibility.ToString();
                                        if (propertySymbol.IsStatic) accessibility += " (static)";

                                        unusedMembers.Add(new UnusedMemberInfo(
                                            MemberType.Property,
                                            $"{classSymbol.Name}.{propertySymbol.Name}",
                                            tree.FilePath,
                                            lineSpan.StartLinePosition.Line + 1,
                                            classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "." + propertySymbol.Name,
                                            accessibility
                                        ));
                                        progress.Report($"Phát hiện property không sử dụng: {accessibility} {propertySymbol.Name} tại {Path.GetFileName(tree.FilePath)} dòng {lineSpan.StartLinePosition.Line + 1}");
                            }
                                }
                            }

                            // Analyze Events (EventFieldDeclarationSyntax)
                            var eventFields = classDecl.Members.OfType<EventFieldDeclarationSyntax>();
                            foreach (var eventFieldSyntax in eventFields)
                                {
                                var eventSymbol = semanticModel.GetDeclaredSymbol(eventFieldSyntax);
                                if (eventSymbol == null) continue;

                                if (eventSymbol.DeclaredAccessibility == Microsoft.CodeAnalysis.Accessibility.Private)
                                {
                                    var references = await SymbolFinder.FindReferencesAsync(eventSymbol, workspace.CurrentSolution);
                                    bool isReferenced = references.Any(r => r.Locations.Any());

                                    if (!isReferenced)
                                    {
                                        var lineSpan = tree.GetLineSpan(eventFieldSyntax.Span);
                                        string accessibility = eventSymbol.DeclaredAccessibility.ToString();
                                        if (eventSymbol.IsStatic) accessibility += " (static)";

                                            unusedMembers.Add(new UnusedMemberInfo(
                                            MemberType.Event,
                                            $"{classSymbol.Name}.{eventSymbol.Name}",
                                                tree.FilePath,
                                                lineSpan.StartLinePosition.Line + 1,
                                            classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "." + eventSymbol.Name,
                                            accessibility
                                            ));
                                        progress.Report($"Phát hiện event không sử dụng: {accessibility} {classSymbol.Name}.{eventSymbol.Name} tại {Path.GetFileName(tree.FilePath)} dòng {lineSpan.StartLinePosition.Line + 1}");
                                    }
                                }
                            }
                        }
                    } // Đóng foreach classDecl
                } // Đóng foreach documentInProject
                catch (ArgumentException ex) // Bắt lỗi cụ thể GetSemanticModel
                {
                    progress.Report($"Lỗi ArgumentException khi xử lý tệp {currentFileName}: {ex.Message}. Kiểm tra xem SyntaxTree có thuộc Compilation không. Bỏ qua tệp này.");
                    continue;
                }
                catch (Exception ex) // Bắt các lỗi chung khác khi xử lý một tệp
                {
                    progress.Report($"Lỗi không xác định khi phân tích tệp {currentFileName}: {ex.Message}. Bỏ qua tệp này.");
                    continue;
                }
            }
            progress.Report($"Hoàn thành phân tích {csharpFiles.Length} tệp.");
            return unusedMembers;
        }
    }
}