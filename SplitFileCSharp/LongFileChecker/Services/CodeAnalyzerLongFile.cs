using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using LongFileChecker.Models;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace LongFileChecker.Services
{
    public class CodeAnalyzerLongFile
    {
        public event Action<string> OnProgressUpdate;
        private CSharpCompilation _compilation;
        private Dictionary<string, SyntaxTree> _syntaxTrees = new Dictionary<string, SyntaxTree>();

        private List<string> GetFilesFromPattern(string folderPath, string filePattern, CancellationToken token)
        {
            var allFiles = new List<string>();
            try
            {
                OnProgressUpdate?.Invoke($"Đang tìm kiếm file với pattern: '{filePattern}' trong thư mục: {folderPath}");
                token.ThrowIfCancellationRequested();
                allFiles.AddRange(Directory.GetFiles(folderPath, filePattern, SearchOption.AllDirectories));
                token.ThrowIfCancellationRequested();
                OnProgressUpdate?.Invoke($"Tìm thấy tổng cộng {allFiles.Count} file khớp pattern (bao gồm cả non-.cs files).");
            }
            catch (OperationCanceledException)
            {
                OnProgressUpdate?.Invoke("Hoạt động tìm kiếm file bị hủy.");
                throw;
            }
            catch (Exception ex)
            {
                OnProgressUpdate?.Invoke($"Lỗi khi tìm file: {ex.Message}");
            }
            // Chỉ xử lý file .cs
            var csFiles = allFiles.Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)).ToList();
            OnProgressUpdate?.Invoke($"Trong đó có {csFiles.Count} file C# sẽ được xử lý.");
            return csFiles;
        }

        public async Task<List<FileData>> AnalyzeDirectoryAsync(string folderPath, string filePattern, bool performDetailedAnalysis, long maxLengthThreshold, CancellationToken token)
        {
            OnProgressUpdate?.Invoke("Bắt đầu quá trình phân tích thư mục...");
            _syntaxTrees.Clear();
            _compilation = null;
            var analyzedFileDatas = new List<FileData>();

            List<string> csFilesToProcess = GetFilesFromPattern(folderPath, filePattern, token);
            token.ThrowIfCancellationRequested();

            if (!csFilesToProcess.Any())
            {
                OnProgressUpdate?.Invoke("Không tìm thấy file C# nào để phân tích.");
                return analyzedFileDatas;
            }

            if (!performDetailedAnalysis)
            {
                OnProgressUpdate?.Invoke("Chế độ quét nhanh: Chỉ lấy thông tin kích thước file.");
                int filesScanned = 0;
                foreach (var filePath in csFilesToProcess)
                {
                    token.ThrowIfCancellationRequested();
                    filesScanned++;
                    OnProgressUpdate?.Invoke($"Đang kiểm tra kích thước file ({filesScanned}/{csFilesToProcess.Count}): {Path.GetFileName(filePath)}...");
                    long currentFileLength = 0;
                    try
                    {
                        currentFileLength = new FileInfo(filePath).Length;
                    }
                    catch (Exception ex)
                    {
                        OnProgressUpdate?.Invoke($"Không thể lấy kích thước file cho: {Path.GetFileName(filePath)}. Lỗi: {ex.Message}. Bỏ qua file này.");
                        continue;
                    }

                    if (maxLengthThreshold <= 0 || currentFileLength > maxLengthThreshold)
                    {
                         OnProgressUpdate?.Invoke($"Thêm file vào danh sách: {Path.GetFileName(filePath)} (Size: {currentFileLength}).");
                        analyzedFileDatas.Add(new FileData
                        {
                            Path = filePath,
                            Length = currentFileLength,
                            CodeItems = new List<CodeItemLongFile>() // Empty list
                        });
                    }
                    else
                    {
                        OnProgressUpdate?.Invoke($"Bỏ qua file (nhỏ hơn hoặc bằng ngưỡng): {Path.GetFileName(filePath)} (Size: {currentFileLength} <= Ngưỡng: {maxLengthThreshold}).");
                    }
                }
            }
            else // performDetailedAnalysis is true
            {
                OnProgressUpdate?.Invoke("Chế độ phân tích chi tiết: Sẽ phân tích cú pháp và thành phần file.");
                var syntaxTreesForCompilation = new List<SyntaxTree>();
                int filesReadCount = 0;
                foreach (var filePath in csFilesToProcess)
                {
                    token.ThrowIfCancellationRequested();
                    filesReadCount++;
                    OnProgressUpdate?.Invoke($"Đang đọc và phân tích cú pháp file ({filesReadCount}/{csFilesToProcess.Count}): {Path.GetFileName(filePath)}...");
                    try
                    {
                        var fileContent = await File.ReadAllTextAsync(filePath, token);
                        token.ThrowIfCancellationRequested();
                        var syntaxTree = CSharpSyntaxTree.ParseText(fileContent, path: filePath, cancellationToken: token);
                        syntaxTreesForCompilation.Add(syntaxTree);
                        _syntaxTrees[filePath] = syntaxTree;
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        OnProgressUpdate?.Invoke($"Lỗi khi đọc hoặc phân tích cú pháp file {Path.GetFileName(filePath)}: {ex.Message}");
                    }
                }
                token.ThrowIfCancellationRequested();

                if (!_syntaxTrees.Any())
                {
                    OnProgressUpdate?.Invoke("Không có file nào được phân tích cú pháp thành công.");
                    return analyzedFileDatas;
                }
                OnProgressUpdate?.Invoke($"Đã phân tích cú pháp {_syntaxTrees.Count} file.");

                OnProgressUpdate?.Invoke("Chuẩn bị tạo CSharp Compilation cho phân tích chi tiết...");
                if (_syntaxTrees.Any())
                {
                    try
                    {
                        token.ThrowIfCancellationRequested();
                        await Task.Run(() =>
                        {
                            OnProgressUpdate?.Invoke($"Đang tạo CSharp Compilation (có thể mất thời gian tùy số lượng file: {_syntaxTrees.Count})...");
                            _compilation = CSharpCompilation.Create(
                                assemblyName: Path.GetRandomFileName(),
                                syntaxTrees: _syntaxTrees.Values,
                                references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
                                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                            );
                             token.ThrowIfCancellationRequested();
                        }, token);
                        OnProgressUpdate?.Invoke("CSharp Compilation đã được tạo thành công.");
                    }
                    catch (OperationCanceledException)
                    {
                         OnProgressUpdate?.Invoke("Tạo CSharp Compilation bị hủy.");
                        _compilation = null;
                    }
                    catch (Exception ex)
                    {
                        OnProgressUpdate?.Invoke($"Lỗi nghiêm trọng khi tạo CSharp Compilation: {ex.Message}");
                        _compilation = null;
                    }
                }
                else
                {
                    OnProgressUpdate?.Invoke("Không có syntax tree nào để tạo compilation.");
                    _compilation = null;
                }

                if (_compilation == null && performDetailedAnalysis)
                {
                    OnProgressUpdate?.Invoke("CẢNH BÁO: Không thể tạo CSharp Compilation. Phân tích chi tiết thành phần sẽ bị hạn chế (không có thông tin semantic).");
                }

                int filesProcessedCount = 0;
                foreach (var tree in _syntaxTrees.Values)
                {
                    token.ThrowIfCancellationRequested();
                    filesProcessedCount++;
                    string currentFilePath = tree.FilePath;
                    long currentFileLength = new FileInfo(currentFilePath).Length;

                    if (maxLengthThreshold > 0 && currentFileLength <= maxLengthThreshold)
                    {
                        OnProgressUpdate?.Invoke($"File {Path.GetFileName(currentFilePath)} (Size: {currentFileLength}) không vượt ngưỡng, nhưng đã được parse. Sẽ không thêm vào KQ chi tiết.");
                        continue; 
                    }

                    var fileData = new FileData
                    {
                        Path = currentFilePath,
                        Length = currentFileLength,
                        CodeItems = new List<CodeItemLongFile>()
                    };
                    OnProgressUpdate?.Invoke($"Bắt đầu phân tích chi tiết thành phần file ({filesProcessedCount}/{_syntaxTrees.Count}): {Path.GetFileName(currentFilePath)}...");

                    var root = (CompilationUnitSyntax)await tree.GetRootAsync(token);
                    token.ThrowIfCancellationRequested();
                    SemanticModel semanticModel = _compilation?.GetSemanticModel(tree);

                    foreach (var classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                    {
                        token.ThrowIfCancellationRequested();
                        var classSymbol = semanticModel?.GetDeclaredSymbol(classDeclaration);
                        var classItem = new CodeItemLongFile
                        {
                            Type = "Class",
                            Name = classDeclaration.Identifier.Text,
                            Length = classDeclaration.FullSpan.Length,
                            AccessModifier = classDeclaration.Modifiers.ToString(),
                            IsUsed = true,
                            FilePath = currentFilePath,
                            ClassName = null,
                            OriginalStartOffset = classDeclaration.FullSpan.Start,
                            OriginalFullSpanLength = classDeclaration.FullSpan.Length
                        };
                        
                        fileData.CodeItems.Add(classItem);

                        foreach (var memberDecl in classDeclaration.Members.OfType<MemberDeclarationSyntax>())
                        {
                            token.ThrowIfCancellationRequested();
                            string memberName = "";
                            string memberType = "";
                            string returnTypeSyntax = "";
                            string parametersSyntax = "";
                            string accessModifierSyntax = memberDecl.Modifiers.ToString();
                            bool isMethod = false;

                            ISymbol memberSymbol = semanticModel?.GetDeclaredSymbol(memberDecl);

                            if (memberDecl is MethodDeclarationSyntax methodDecl)
                            {
                                memberName = $"{classDeclaration.Identifier.Text}.{methodDecl.Identifier.Text}";
                                memberType = "Method";
                                isMethod = true;
                                returnTypeSyntax = methodDecl.ReturnType.ToString();
                                parametersSyntax = string.Join(", ", methodDecl.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}"));
                            }
                            else if (memberDecl is PropertyDeclarationSyntax propDecl)
                            {
                                memberName = $"{classDeclaration.Identifier.Text}.{propDecl.Identifier.Text}";
                                memberType = "Property";
                                returnTypeSyntax = propDecl.Type.ToString();
                            }
                            else if (memberDecl is FieldDeclarationSyntax fieldDecl)
                            {
                                memberName = $"{classDeclaration.Identifier.Text}.{string.Join(", ", fieldDecl.Declaration.Variables.Select(v => v.Identifier.Text))}";
                                memberType = "Field";
                                returnTypeSyntax = fieldDecl.Declaration.Type.ToString();
                            }
                            else { continue; }

                            var codeItem = new CodeItemLongFile
                            {
                                Type = memberType,
                                Name = memberName,
                                Length = memberDecl.FullSpan.Length,
                                IsMethod = isMethod,
                                AccessModifier = memberSymbol?.DeclaredAccessibility.ToString().ToLowerInvariant() ?? accessModifierSyntax,
                                ReturnType = (memberSymbol as IMethodSymbol)?.ReturnType.ToDisplayString() ?? (memberSymbol as IPropertySymbol)?.Type.ToDisplayString() ?? (memberSymbol as IFieldSymbol)?.Type.ToDisplayString() ?? returnTypeSyntax,
                                Parameters = (memberSymbol as IMethodSymbol)?.Parameters.Any() == true ? 
                                             string.Join(", ", (memberSymbol as IMethodSymbol).Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}")) : 
                                             parametersSyntax,
                                IsUsed = true,
                                FilePath = currentFilePath,
                                ClassName = classDeclaration.Identifier.Text,
                                OriginalStartOffset = memberDecl.FullSpan.Start,
                                OriginalFullSpanLength = memberDecl.FullSpan.Length
                            };

                            if (performDetailedAnalysis && memberSymbol != null && _compilation != null &&
                                (memberSymbol.DeclaredAccessibility == Microsoft.CodeAnalysis.Accessibility.Private || 
                                 memberSymbol.DeclaredAccessibility == Microsoft.CodeAnalysis.Accessibility.Internal || 
                                 memberSymbol.DeclaredAccessibility == Microsoft.CodeAnalysis.Accessibility.ProtectedAndInternal) 
                               )
                            {
                                OnProgressUpdate?.Invoke($"    Đang phân tích sử dụng cho: {memberName} ({codeItem.AccessModifier})...");
                                codeItem.IsUsed = await IsSymbolReferencedAsync(memberSymbol, _compilation, token);
                                OnProgressUpdate?.Invoke($"    -> {memberName} IsUsed: {codeItem.IsUsed}");
                            }
                            else if (performDetailedAnalysis && memberSymbol == null && semanticModel != null) 
                            {
                                 OnProgressUpdate?.Invoke($"    Không lấy được Symbol cho {memberName} để phân tích sử dụng chính xác.");
                            }

                            fileData.CodeItems.Add(codeItem);
                        }
                    }
                    analyzedFileDatas.Add(fileData);
                    OnProgressUpdate?.Invoke($"Hoàn tất phân tích chi tiết thành phần file: {Path.GetFileName(currentFilePath)}.");
                }
            }

            token.ThrowIfCancellationRequested();
            OnProgressUpdate?.Invoke("Hoàn tất toàn bộ quá trình phân tích thư mục.");
            return analyzedFileDatas;
        }
        
        private async Task<bool> IsSymbolReferencedAsync(ISymbol symbol, CSharpCompilation compilation, CancellationToken token)
        {
            if (symbol == null || compilation == null) 
            {
                OnProgressUpdate?.Invoke($"    Skipping reference check for null symbol or compilation.");
                return true; // Default to true if info is missing, to be safe
            }
            token.ThrowIfCancellationRequested();

            // To use SymbolFinder.FindReferencesAsync, we need a Solution.
            // Create a temporary AdhocWorkspace and Solution containing this compilation.
            var workspace = new AdhocWorkspace();
            var projectId = ProjectId.CreateNewId();
            var documentInfos = new List<DocumentInfo>();

            foreach (var tree in compilation.SyntaxTrees)
            {
                token.ThrowIfCancellationRequested();
                // Ensure tree.FilePath is not null or empty for DocumentInfo.Create
                string docPath = string.IsNullOrEmpty(tree.FilePath) ? Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".cs") : tree.FilePath;
                 var docInfo = DocumentInfo.Create(
                    DocumentId.CreateNewId(projectId), 
                    Path.GetFileName(docPath), // Use a valid file name
                    loader: TextLoader.From(TextAndVersion.Create(await tree.GetTextAsync(token), VersionStamp.Create(), docPath)),
                    filePath: docPath
                );
                documentInfos.Add(docInfo);
            }

            var projectInfo = ProjectInfo.Create(projectId, VersionStamp.Default, "TempProject", "TempProject", LanguageNames.CSharp)
                                         .WithCompilationOptions(compilation.Options)
                                         .WithMetadataReferences(compilation.References)
                                         .WithDocuments(documentInfos);
            
            var solution = workspace.AddProject(projectInfo).Solution;
            token.ThrowIfCancellationRequested();

            // Now find references using the symbol and the constructed solution.
            var references = await Microsoft.CodeAnalysis.FindSymbols.SymbolFinder.FindReferencesAsync(symbol, solution, token);
            token.ThrowIfCancellationRequested();
            foreach (var reference in references)
            {
                token.ThrowIfCancellationRequested();
                if (reference.Locations.Any()) return true;
            }
            return false;
        }

        public async Task<(bool success, string message, List<string> modifiedFiles)> RemoveMembersAsync(List<CodeItemLongFile> itemsToRemove, bool previewOnly, CancellationToken token)
        {
            OnProgressUpdate?.Invoke($"Bắt đầu quá trình loại bỏ thành viên. Chế độ Preview: {previewOnly}");
            var modifiedFiles = new HashSet<string>();
            int itemsProcessed = 0;
            int totalItems = itemsToRemove.Count;

            var groupedItems = itemsToRemove.GroupBy(item => item.FilePath);

            foreach (var group in groupedItems)
            {
                token.ThrowIfCancellationRequested();
                string filePath = group.Key;
                OnProgressUpdate?.Invoke($"Đang xử lý file: {Path.GetFileName(filePath)} ({group.Count()} mục cần xóa)");

                if (!_syntaxTrees.TryGetValue(filePath, out var originalTree))
                {
                    OnProgressUpdate?.Invoke($"Lỗi: Không tìm thấy SyntaxTree đã phân tích cho file {filePath}. Bỏ qua file này.");
                    continue;
                }

                var root = (CompilationUnitSyntax)await originalTree.GetRootAsync();
                var editor = new SyntaxEditor(root, new AdhocWorkspace());

                foreach (var itemToRemove in group.OrderByDescending(i => i.OriginalStartOffset))
                {
                    token.ThrowIfCancellationRequested();
                    itemsProcessed++;
                    OnProgressUpdate?.Invoke($"    ({itemsProcessed}/{totalItems}) Đang chuẩn bị xóa: {itemToRemove.Name} ({itemToRemove.Type}) tại offset {itemToRemove.OriginalStartOffset} trong {Path.GetFileName(filePath)}...");
                    
                    SyntaxNode nodeToRemove = null;
                    var targetSpan = new TextSpan(itemToRemove.OriginalStartOffset, itemToRemove.OriginalFullSpanLength);
                    var candidateNodes = root.DescendantNodes(targetSpan)
                                            .Where(n => n.FullSpan == targetSpan);

                    if (itemToRemove.Type == "Method")
                    {
                        nodeToRemove = candidateNodes.OfType<MethodDeclarationSyntax>()
                                             .FirstOrDefault(m => m.Identifier.Text == itemToRemove.Name.Split('.').LastOrDefault() && 
                                                                  classDeclarationName(m.Parent as ClassDeclarationSyntax) == itemToRemove.ClassName);
                    }
                    else if (itemToRemove.Type == "Property")
                    {
                        nodeToRemove = candidateNodes.OfType<PropertyDeclarationSyntax>()
                                             .FirstOrDefault(p => p.Identifier.Text == itemToRemove.Name.Split('.').LastOrDefault() &&
                                                                  classDeclarationName(p.Parent as ClassDeclarationSyntax) == itemToRemove.ClassName);
                    }
                    else if (itemToRemove.Type == "Field")
                    {
                        nodeToRemove = candidateNodes.OfType<FieldDeclarationSyntax>()
                                             .FirstOrDefault(f => f.Declaration.Variables.Any(v => v.Identifier.Text == itemToRemove.Name.Split('.').LastOrDefault()) &&
                                                                  classDeclarationName(f.Parent as ClassDeclarationSyntax) == itemToRemove.ClassName);
                         if (nodeToRemove is FieldDeclarationSyntax fieldDeclSyntax && fieldDeclSyntax.Declaration.Variables.Count > 1)
                         {
                            var variableToKeep = fieldDeclSyntax.Declaration.Variables.Where(v => v.Identifier.Text != itemToRemove.Name.Split('.').LastOrDefault());
                            if (variableToKeep.Any())
                            {
                                var newDeclaration = fieldDeclSyntax.Declaration.WithVariables(SyntaxFactory.SeparatedList(variableToKeep));
                                editor.ReplaceNode(fieldDeclSyntax.Declaration, newDeclaration);
                                OnProgressUpdate?.Invoke($"        Đã cập nhật khai báo field (giữ lại các biến khác): {itemToRemove.Name}");
                                continue;
                            }
                         }
                    }
                    else if (itemToRemove.Type == "Class")
                    {
                         nodeToRemove = candidateNodes.OfType<ClassDeclarationSyntax>()
                                             .FirstOrDefault(c => c.Identifier.Text == itemToRemove.Name);
                    }

                    if (nodeToRemove != null)
                    {
                        editor.RemoveNode(nodeToRemove, SyntaxRemoveOptions.KeepExteriorTrivia | SyntaxRemoveOptions.KeepEndOfLine);
                        OnProgressUpdate?.Invoke($"        Đã lên lịch xóa (bao gồm trivia mặc định): {itemToRemove.Name}");
                    }
                    else
                    {
                        OnProgressUpdate?.Invoke($"        Lỗi: Không tìm thấy node khớp chính xác cho {itemToRemove.Name} tại offset {itemToRemove.OriginalStartOffset}. Có thể file đã thay đổi hoặc thông tin item không chính xác.");
                    }
                }

                if (!previewOnly)
                {
                    var newRoot = editor.GetChangedRoot();
                    var newContent = newRoot.ToFullString();
                    try
                    {
                        await File.WriteAllTextAsync(filePath, newContent);
                        _syntaxTrees[filePath] = CSharpSyntaxTree.ParseText(newContent, path: filePath);
                        modifiedFiles.Add(filePath);
                        OnProgressUpdate?.Invoke($"    Đã ghi thay đổi vào file: {Path.GetFileName(filePath)}");
                    }
                    catch (Exception ex)
                    {
                        OnProgressUpdate?.Invoke($"    Lỗi khi ghi file {Path.GetFileName(filePath)}: {ex.Message}");
                        return (false, $"Lỗi khi ghi file {Path.GetFileName(filePath)}: {ex.Message}", new List<string>(modifiedFiles));
                    }
                }
                else
                {
                    OnProgressUpdate?.Invoke($"    Chế độ Preview: Các thay đổi cho {Path.GetFileName(filePath)} không được ghi.");
                }
            }

            if (!previewOnly && modifiedFiles.Any() && _compilation != null)
            {
                token.ThrowIfCancellationRequested();
                OnProgressUpdate?.Invoke("Cập nhật lại CSharp Compilation sau khi xóa thành viên...");
                var newSyntaxTreesForCompilation = _syntaxTrees.Values.ToList();
                 await Task.Run(() =>
                {
                    try
                    {
                         _compilation = CSharpCompilation.Create(
                            assemblyName: _compilation.AssemblyName,
                            syntaxTrees: newSyntaxTreesForCompilation,
                            references: _compilation.References,
                            options: (CSharpCompilationOptions)_compilation.Options
                        );
                        token.ThrowIfCancellationRequested();
                        OnProgressUpdate?.Invoke("CSharp Compilation đã được cập nhật.");
                    }
                    catch (OperationCanceledException) { OnProgressUpdate?.Invoke("Cập nhật Compilation bị hủy."); _compilation = null; }
                    catch (Exception ex)
                    {
                        OnProgressUpdate?.Invoke($"Lỗi khi cập nhật CSharp Compilation: {ex.Message}");
                        _compilation = null;
                    }
                }, token);
            }

            string finalMessage = previewOnly ? "Hoàn tất quá trình preview xóa thành viên." : "Hoàn tất quá trình xóa thành viên.";
            OnProgressUpdate?.Invoke(finalMessage);
            return (true, finalMessage, new List<string>(modifiedFiles));
        }

        private string classDeclarationName(ClassDeclarationSyntax classSyntax)
        {
            return classSyntax?.Identifier.Text;
        }

        public List<CodeItemLongFile> AnalyzeFile(string content, bool findUnused, string filePathForContext = "", CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            OnProgressUpdate?.Invoke("Bắt đầu phân tích nội dung file (phân tích cú pháp đơn giản)...");
            var items = new List<CodeItemLongFile>();
            var tree = CSharpSyntaxTree.ParseText(content);
            var root = tree.GetCompilationUnitRoot();

            foreach (var classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var classText = classDeclaration.ToString();
                var classItem = new CodeItemLongFile
                {
                    Type = "Class",
                    Name = classDeclaration.Identifier.Text,
                    Length = classText.Length,
                    IsMethod = false,
                    AccessModifier = classDeclaration.Modifiers.ToString(),
                    IsUsed = !classDeclaration.Modifiers.ToString().Contains("private") || !findUnused || IsTypeReferenced_Old(classDeclaration, root)
                };
                items.Add(classItem);

                foreach (var methodDeclaration in classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    var methodText = methodDeclaration.ToString();
                    var isPrivate = methodDeclaration.Modifiers.ToString().Contains("private");
                    var methodItem = new CodeItemLongFile
                    {
                        Type = "Method",
                        Name = $"{classDeclaration.Identifier.Text}.{methodDeclaration.Identifier.Text}",
                        Length = methodText.Length,
                        IsMethod = true,
                        AccessModifier = methodDeclaration.Modifiers.ToString(),
                        ReturnType = methodDeclaration.ReturnType.ToString(),
                        Parameters = string.Join(", ", methodDeclaration.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}")),
                        IsUsed = !isPrivate || !findUnused || IsMethodUsed_Old(methodDeclaration, classDeclaration)
                    };
                    items.Add(methodItem);
                }
            }
            OnProgressUpdate?.Invoke($"Hoàn tất phân tích nội dung file, tìm thấy {items.Count} thành phần.");
            return items;
        }

        private bool IsTypeReferenced_Old(TypeDeclarationSyntax typeDeclaration, CompilationUnitSyntax root)
        {
            var typeName = typeDeclaration.Identifier.Text;
            return root.DescendantNodes()
                .Where(n => n != typeDeclaration)
                .OfType<IdentifierNameSyntax>()
                .Any(id => id.Identifier.Text == typeName);
        }

        private bool IsMethodUsed_Old(MethodDeclarationSyntax methodToCheck, ClassDeclarationSyntax containingClass)
        {
            var methodName = methodToCheck.Identifier.Text;
            
            foreach (var method in containingClass.Members.OfType<MethodDeclarationSyntax>().Where(m => m != methodToCheck))
            {
                if (IsMethodCalledInNode_Old(method, methodName)) return true;
            }
            foreach (var property in containingClass.Members.OfType<PropertyDeclarationSyntax>())
            {
                if (property.AccessorList != null)
                {
                    foreach (var accessor in property.AccessorList.Accessors)
                    {
                        if (IsMethodCalledInNode_Old(accessor, methodName)) return true;
                    }
                }
                if (property.ExpressionBody != null && IsMethodCalledInNode_Old(property.ExpressionBody, methodName)) return true;
            }
            foreach (var constructor in containingClass.Members.OfType<ConstructorDeclarationSyntax>())
            {
                if (IsMethodCalledInNode_Old(constructor, methodName)) return true;
            }
            foreach (var evt in containingClass.Members.OfType<EventDeclarationSyntax>())
            {
                if (IsMethodCalledInNode_Old(evt, methodName)) return true;
            }
            foreach (var field in containingClass.Members.OfType<FieldDeclarationSyntax>())
            {
                foreach (var variable in field.Declaration.Variables)
                {
                    if (variable.Initializer != null && IsMethodCalledInNode_Old(variable.Initializer, methodName)) return true;
                }
            }
            return false;
        }

        private bool IsMethodCalledInNode_Old(SyntaxNode node, string methodName)
        {
            var invocations = node.DescendantNodes().OfType<InvocationExpressionSyntax>();
            foreach (var invocation in invocations)
            {
                if (invocation.Expression is IdentifierNameSyntax identifier && identifier.Identifier.Text == methodName) return true;
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                    memberAccess.Name.Identifier.Text == methodName &&
                    (memberAccess.Expression is ThisExpressionSyntax || memberAccess.Expression is IdentifierNameSyntax)) return true;
            }
            var methodGroups = node.DescendantNodes().OfType<IdentifierNameSyntax>()
                .Where(id => id.Identifier.Text == methodName && !(id.Parent is InvocationExpressionSyntax));
            if (methodGroups.Any()) return true;
            var lambdas = node.DescendantNodes().Where(n => n is LambdaExpressionSyntax || n is AnonymousMethodExpressionSyntax);
            foreach (var lambda in lambdas)
            {
                if (lambda.DescendantNodes().OfType<IdentifierNameSyntax>().Any(id => id.Identifier.Text == methodName)) return true;
            }
            return false;
        }
    }
}