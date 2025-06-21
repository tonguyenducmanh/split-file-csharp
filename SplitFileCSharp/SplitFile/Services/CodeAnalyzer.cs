using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using SplitFile.Models;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SplitFile.Services
{
    public class CodeAnalyzer
    {
        #region "Declare"
        private readonly Dictionary<string, SyntaxTree> _syntaxTrees;
        private readonly Dictionary<string, CompilationUnitSyntax> _roots;
        private readonly Dictionary<string, ClassDeclarationSyntax> _classNodes;
        private readonly HashSet<SyntaxNode> _allUsings;
        private Dictionary<string, bool> _memberUsages;
        private readonly List<CodeItem> _codeItems;
        private NamespaceDeclarationSyntax _namespaceNode;
        private readonly Dictionary<(string methodName, string fullSignature), string> _methodFileMapping = new Dictionary<(string, string), string>();

        private CompilationUnitSyntax _root => MainFilePath != null ? _roots[MainFilePath]
            : throw new InvalidOperationException("Chưa phân tích file chính. Vui lòng gọi AnalyzeFiles trước.");
        private ClassDeclarationSyntax _classNode => MainFilePath != null ? _classNodes[MainFilePath]
            : throw new InvalidOperationException("Chưa phân tích file chính. Vui lòng gọi AnalyzeFiles trước.");
        #endregion

        #region "Property"
        public string MainFilePath { get; private set; }
        public List<FileEntry> Files { get; private set; }
        public List<AnalyzedMethod> Methods { get; private set; }
        public List<CodeItem> CodeItems => _codeItems;
        public Dictionary<string, ClassDeclarationSyntax> ClassNodes => _classNodes;
        #endregion

        #region "Construct"
        public CodeAnalyzer()
        {
            _syntaxTrees = new Dictionary<string, SyntaxTree>();
            _roots = new Dictionary<string, CompilationUnitSyntax>();
            _classNodes = new Dictionary<string, ClassDeclarationSyntax>();
            _allUsings = new HashSet<SyntaxNode>();
            Files = new List<FileEntry>();
            Methods = new List<AnalyzedMethod>();
            _codeItems = new List<CodeItem>();
        }
        #endregion

        #region "Public Methods"
        public async Task<AnalizeResult> AnalyzeFiles(string mainFile, List<string> additionalFiles)
        {
            var result = new AnalizeResult();
            try
            {
                _syntaxTrees.Clear();
                _roots.Clear();
                _classNodes.Clear();
                _allUsings.Clear();
                Files.Clear();
                Methods.Clear();
                _codeItems.Clear();

                if (!File.Exists(mainFile))
                    throw new FileNotFoundException($"File chính không tồn tại: {mainFile}");

                additionalFiles = additionalFiles.Where(File.Exists).ToList();

                Debug.WriteLine($"Đang phân tích file chính: {mainFile}");
                MainFilePath = mainFile;
                await AnalyzeFile(mainFile, true);
                result.ClassName = _classNode.Identifier.Text;

                foreach (var file in additionalFiles)
                {
                    try
                    {
                        Debug.WriteLine($"Đang phân tích file: {file}");
                        await AnalyzeFile(file, false);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Lỗi khi phân tích {file}: {ex.Message}");
                        if (!ex.Message.Contains("namespace"))
                            throw;
                    }
                }

                Methods = new List<AnalyzedMethod>();
                foreach (var classEntry in _classNodes)
                {
                    var methods = classEntry.Value.Members
                        .OfType<MethodDeclarationSyntax>()
                        .Select(m => new AnalyzedMethod(m.Identifier.Text, classEntry.Key, m));
                    Methods.AddRange(methods);
                }

                foreach (var file in Files)
                {
                    file.IsAnalyzed = true;
                }

                AnalyzeCode();
                return result;
            }
            catch
            {
                _syntaxTrees.Clear();
                _roots.Clear();
                _classNodes.Clear();
                _allUsings.Clear();
                Files.Clear();
                Methods.Clear();
                _codeItems.Clear();
                throw;
            }
        }

        public List<MemberDeclarationSyntax> FindUnusedMembers()
        {
            _memberUsages = new Dictionary<string, bool>();
            AnalyzeMemberUsages();

            var unusedMembers = _classNode.Members
                .Where(m => m is FieldDeclarationSyntax || m is PropertyDeclarationSyntax || m is MethodDeclarationSyntax)
                .Where(m =>
                {
                    string memberName = GetMemberName(m);
                    return _memberUsages.ContainsKey(memberName) && !_memberUsages[memberName];
                })
                .ToList();

            return unusedMembers;
        }

        public async Task RemoveUnusedMembers(List<MemberDeclarationSyntax> unusedMembers)
        {
            if (!unusedMembers.Any())
                return;

            var newRoot = _root.RemoveNodes(unusedMembers, SyntaxRemoveOptions.KeepExteriorTrivia);
            _roots[MainFilePath] = newRoot;

            var newClassNode = newRoot.DescendantNodes().OfType<ClassDeclarationSyntax>().First();
            _classNodes[MainFilePath] = newClassNode;
            _namespaceNode = newClassNode.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();

            await File.WriteAllTextAsync(MainFilePath, newRoot.ToFullString());
        }

        public async Task CreateNewFiles(string sourceFile, List<(string targetFile, List<string> methods, string description)> outputs)
        {
            // Đọc và phân tích lại file nguồn để đảm bảo làm việc trên cây cú pháp mới nhất
            if (!File.Exists(sourceFile))
                throw new FileNotFoundException($"File nguồn không tồn tại: {sourceFile}");

            var currentSourceCode = await File.ReadAllTextAsync(sourceFile);
            var currentSourceSyntaxTree = CSharpSyntaxTree.ParseText(currentSourceCode);
            var currentSourceRoot = await currentSourceSyntaxTree.GetRootAsync() as CompilationUnitSyntax;

            if (currentSourceRoot == null)
                throw new Exception($"Không thể phân tích cú pháp file nguồn: {sourceFile} khi tạo file mới.");

            var currentSourceClassNode = currentSourceRoot.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            if (currentSourceClassNode == null)
                throw new Exception($"File nguồn {Path.GetFileName(sourceFile)} không chứa class nào để xử lý khi tạo file mới.");

            // if (!_roots.ContainsKey(sourceFile)) // Kiểm tra này có thể không cần thiết nữa nếu chúng ta luôn đọc lại file
            //     throw new ArgumentException($"File nguồn {sourceFile} chưa được phân tích");

            // var root = _roots[sourceFile]; // Sẽ sử dụng currentSourceRoot
            // var classNode = _classNodes[sourceFile]; // Sẽ sử dụng currentSourceClassNode
            _methodFileMapping.Clear(); // Reset the mapping for new batch

            var allMethodNames = outputs.SelectMany(o => o.methods).Distinct().ToList();
            var methodsToExtract = new List<MethodDeclarationSyntax>();
            var methodsToRemove = new List<MethodDeclarationSyntax>(); // Đổi tên thành methodsToRemoveFromSource để rõ ràng hơn

            foreach (var methodName in allMethodNames)
            {
                // Lấy tất cả các phương thức có cùng tên từ currentSourceClassNode
                var matchingMethods = currentSourceClassNode.Members // Sử dụng currentSourceClassNode
                    .OfType<MethodDeclarationSyntax>()
                    .Where(m => m.Identifier.Text == methodName)
                    .ToList();

                foreach (var method in matchingMethods)
                {
                    methodsToExtract.Add(method);
                    methodsToRemove.Add(method); // Thêm vào danh sách cần xóa khỏi file nguồn
                }
            }

            foreach (var output in outputs)
            {
                var methodsToProcess = new List<MethodDeclarationSyntax>();
                var filteredMethods = methodsToExtract // methodsToExtract giờ được lấy từ currentSourceClassNode
                    .Where(m => output.methods.Contains(m.Identifier.Text))
                    .ToList();

                foreach (var method in filteredMethods)
                {
                    var fullSignature = GetMethodFullSignature(method);
                    var key = (method.Identifier.Text, fullSignature);

                    if (!_methodFileMapping.ContainsKey(key))
                    {
                        methodsToProcess.Add(method);
                        _methodFileMapping[key] = output.targetFile;
                    }
                    else
                    {
                        Debug.WriteLine($"Phương thức {method.Identifier.Text} ({fullSignature}) đã tồn tại trong file {_methodFileMapping[key]}, bỏ qua không thêm vào {output.targetFile}");
                    }
                }

                if (methodsToProcess.Any())
                {
                    // CreateNewFileFromMethods sử dụng _namespaceNode (từ file chính) và _classNode (từ file chính)
                    // để lấy thông tin class (tên, modifiers, base list) cho file partial mới.
                    // Điều này vẫn ổn vì file mới là partial của class chính.
                    await CreateNewFileFromMethods(sourceFile, output.targetFile, methodsToProcess, output.description);
                }
            }

            if (methodsToRemove.Any())
            {
                // Xóa phương thức từ currentSourceClassNode và currentSourceRoot
                var updatedSourceClassNode = currentSourceClassNode.RemoveNodes(methodsToRemove, SyntaxRemoveOptions.KeepDirectives);
                var updatedSourceRoot = currentSourceRoot.ReplaceNode(currentSourceClassNode, updatedSourceClassNode);

                await File.WriteAllTextAsync(sourceFile, updatedSourceRoot.ToFullString());

                // Cập nhật cache với trạng thái đã được lưu vào đĩa
                _roots[sourceFile] = updatedSourceRoot;
                _classNodes[sourceFile] = updatedSourceClassNode;
                _syntaxTrees[sourceFile] = updatedSourceRoot.SyntaxTree; // Cập nhật cả syntax tree
            }
        }
        #endregion

        #region "Private Methods"
        private string GetMethodFullSignature(MethodDeclarationSyntax method)
        {
            var parameters = method.ParameterList.Parameters.Select(p =>
            {
                var modifiers = p.Modifiers.ToString().Trim();
                var type = p.Type.ToString();
                var defaultValue = p.Default != null ? " = " + p.Default.Value.ToString() : "";
                return $"{modifiers} {type}{(p.Modifiers.Any(m => m.IsKind(SyntaxKind.OutKeyword) || m.IsKind(SyntaxKind.RefKeyword)) ? "" : "")} {p.Identifier.Text}{defaultValue}".Trim();
            });

            var modifiersText = method.Modifiers.ToString();
            var returnType = method.ReturnType.ToString();
            var parametersText = string.Join(", ", parameters);

            return $"{modifiersText} {returnType} {method.Identifier.Text}({parametersText})";
        }

        private async Task AnalyzeFile(string filePath, bool isMainFile)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"File không tồn tại: {filePath}");

                var fileEntry = new FileEntry(filePath, isMainFile);
                Files.Add(fileEntry);

                var sourceCode = await File.ReadAllTextAsync(filePath);
                var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
                var root = await syntaxTree.GetRootAsync() as CompilationUnitSyntax;

                if (root == null)
                    throw new Exception($"Không thể phân tích cú pháp file: {filePath}");

                var usings = root.Usings;
                foreach (var usingDir in usings)
                {
                    _allUsings.Add(usingDir);
                }

                var classNodes = root.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .ToList();

                if (!classNodes.Any())
                    throw new Exception($"File {Path.GetFileName(filePath)} không chứa class nào để phân tích");
                if (classNodes.Count > 1)
                    throw new Exception($"File {Path.GetFileName(filePath)} chứa nhiều class");

                var classNode = classNodes.First();
                var namespaceNode = classNode.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();

                if (namespaceNode == null)
                    throw new Exception($"Không tìm thấy namespace trong file {Path.GetFileName(filePath)}");

                foreach (var usingDir in namespaceNode.Usings)
                {
                    _allUsings.Add(usingDir);
                }

                if (isMainFile)
                {
                    _namespaceNode = namespaceNode;
                }
                else if (namespaceNode.Name.ToString() != _namespaceNode.Name.ToString())
                {
                    throw new Exception($"File {Path.GetFileName(filePath)} có namespace '{namespaceNode.Name}' khác với file chính '{_namespaceNode.Name}'");
                }

                if (!classNode.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                {
                    var leadingTrivia = classNode.GetLeadingTrivia();
                    var partialToken = SyntaxFactory.Token(SyntaxKind.PartialKeyword)
                                           .WithTrailingTrivia(SyntaxFactory.Space);
                    var newClassNode = classNode
                        .WithoutLeadingTrivia()
                        .AddModifiers(partialToken)
                        .WithLeadingTrivia(leadingTrivia);
                    root = root.ReplaceNode(classNode, newClassNode);
                    classNode = newClassNode;
                    await File.WriteAllTextAsync(filePath, root.ToFullString());
                }

                _syntaxTrees[filePath] = syntaxTree;
                _roots[filePath] = root;
                _classNodes[filePath] = classNode;

                Debug.WriteLine($"Đã phân tích thành công {(isMainFile ? "file chính" : "file")} {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi khi phân tích {filePath}: {ex.Message}");
                throw;
            }
        }

        private async Task CreateNewFileFromMethods(string sourceFile, string filePath, List<MethodDeclarationSyntax> methods, string description)
        {
            // Parse existing file if it exists
            CompilationUnitSyntax existingRoot = null;
            ClassDeclarationSyntax existingClass = null;
            var existingUsings = new HashSet<string>();
            string existingNamespace = null;

            if (File.Exists(filePath))
            {
                var existingContent = await File.ReadAllTextAsync(filePath);
                var existingTree = CSharpSyntaxTree.ParseText(existingContent);
                existingRoot = await existingTree.GetRootAsync() as CompilationUnitSyntax;

                if (existingRoot != null)
                {
                    // Get existing usings
                    foreach (var usingDir in existingRoot.Usings)
                    {
                        existingUsings.Add(usingDir.ToString().Trim());
                    }

                    // Get existing class and namespace
                    var namespaceDecl = existingRoot.DescendantNodes().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
                    if (namespaceDecl != null)
                    {
                        existingNamespace = namespaceDecl.Name.ToString();
                        existingClass = namespaceDecl.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                    }
                }
            }

            var standardUsings = new[] {
        "using System;",
        "using System.Collections.Generic;",
        "using System.Linq;",
        "using System.Text;",
        "using System.Threading.Tasks;"
    };

            var currentUsings = _allUsings
                .Cast<UsingDirectiveSyntax>()
                .Select(u => u.ToString().Trim())
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Distinct();

            var systemUsings = standardUsings
                .Where(u => _allUsings.Any(existing =>
                    existing.ToString().Contains(u.TrimEnd(';'))));

            var microsoftUsings = currentUsings
                .Where(u => u.Contains("Microsoft."))
                .OrderBy(u => u);

            var otherUsings = currentUsings
                .Where(u => !u.Contains("Microsoft.") && !standardUsings.Contains(u))
                .OrderBy(u => u);

            // Combine existing and new usings
            var formattedUsings = systemUsings
                .Concat(microsoftUsings)
                .Concat(otherUsings)
                .Select(u => u.EndsWith(";") ? u : u + ";")
                .Distinct()
                .Where(u => !existingUsings.Contains(u));  // Only add usings that don't exist

            var namespaceText = string.Join(".",
                _namespaceNode.Name.ToString()
                    .Split('.')
                    .Select(part => part.Trim())
                    .Where(part => !string.IsNullOrWhiteSpace(part)));

            var methodsText = string.Join(Environment.NewLine + Environment.NewLine,
                methods.Select(m => {
                    var methodText = m.ToFullString();
                    var lines = methodText.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
                    var processedLines = new List<string>();
                    bool inComment = false;
                    bool addedSourceInfo = false;
                    bool hasComment = false;
                    string indent = "        ";

                    foreach (var line in lines)
                    {
                        var trimmed = line.TrimStart();

                        // Skip #region and #endregion directives
                        if (trimmed.StartsWith("#region") || trimmed.StartsWith("#endregion"))
                        {
                            continue;
                        }

                        // Detect comment type and presence
                        if (trimmed.StartsWith("/**") || trimmed.StartsWith("///"))
                        {
                            hasComment = true;
                        }

                        if (trimmed.StartsWith("/**"))
                        {
                            inComment = true;
                            processedLines.Add(line);
                        }
                        else if (trimmed.StartsWith("*/"))
                        {
                            inComment = false;
                            if (!addedSourceInfo)
                            {
                                var currentIndent = new string(' ', line.Length - line.TrimStart().Length);
                                processedLines.Add($"{currentIndent}* (Được chuyển từ {Path.GetFileName(sourceFile)}, ngày chuyển: {DateTime.Now:dd/MM/yyyy HH:mm})");
                                addedSourceInfo = true;
                            }
                            processedLines.Add(line);
                        }
                        else if (trimmed.StartsWith("///") && trimmed.Contains("</summary>"))
                        {
                            if (!addedSourceInfo)
                            {
                                var currentIndent = new string(' ', line.Length - line.TrimStart().Length);
                                processedLines.Add($"{currentIndent}/// (Được chuyển từ {Path.GetFileName(sourceFile)}, ngày chuyển: {DateTime.Now:dd/MM/yyyy HH:mm})");
                                addedSourceInfo = true;
                            }
                            processedLines.Add(line);
                        }
                        else if (string.IsNullOrWhiteSpace(line))
                        {
                            processedLines.Add(line);
                        }
                        else
                        {
                            if (!hasComment && !inComment && !addedSourceInfo && !line.TrimStart().StartsWith("///"))
                            {
                                // Add new XML comment if no comment exists
                                processedLines.Add($"{indent}/// <summary>");
                                processedLines.Add($"{indent}/// (Được chuyển từ {Path.GetFileName(sourceFile)}, ngày chuyển: {DateTime.Now:dd/MM/yyyy HH:mm})");
                                processedLines.Add($"{indent}/// </summary>");
                                addedSourceInfo = true;
                                hasComment = true;
                            }
                            processedLines.Add(line);

                            //if (inComment)
                            //{
                            //    // Handle multi-line comment content
                            //    if (trimmed.StartsWith("*"))
                            //    {
                            //        processedLines.Add(line);
                            //    }
                            //    else
                            //    {
                            //        var currentIndent = new string(' ', line.Length - line.TrimStart().Length);
                            //        processedLines.Add($"{currentIndent}* {trimmed}");
                            //    }
                            //}
                            //else
                            //{
                            //    processedLines.Add(line);
                            //}

                        }
                    }

                    // Remove any trailing empty lines after removing #region directives
                    while (processedLines.Count > 0 && string.IsNullOrWhiteSpace(processedLines[processedLines.Count - 1]))
                    {
                        processedLines.RemoveAt(processedLines.Count - 1);
                    }

                    return string.Join(Environment.NewLine, processedLines);
                }));

            string fileContent;
            if (existingRoot != null && existingClass != null && existingNamespace == namespaceText)
            {
                // If file exists and has matching namespace, append methods to existing class
                var textSpan = existingClass.GetLocation().SourceSpan;
                var existingContent = await File.ReadAllTextAsync(filePath);
                var sourceText = SourceText.From(existingContent);
                var classSpan = existingClass.Span;

                // Tìm vị trí dấu } cuối cùng của class bằng cách duyệt từ vị trí kết thúc của class
                var linePosition = sourceText.Lines.GetLinePosition(classSpan.End);
                var currentLine = linePosition.Line;
                var classEndLine = -1;
                var bracketCount = 1; // Bắt đầu với 1 vì chúng ta đang ở trong class

                // Duyệt từng dòng từ vị trí cuối class cho đến khi tìm thấy dấu } đóng class
                while (currentLine < sourceText.Lines.Count && bracketCount > 0)
                {
                    var line = sourceText.Lines[currentLine].ToString();
                    foreach (char c in line)
                    {
                        if (c == '{') bracketCount++;
                        if (c == '}') bracketCount--;
                        if (bracketCount == 0)
                        {
                            classEndLine = currentLine;
                            break;
                        }
                    }
                    if (classEndLine != -1) break;
                    currentLine++;
                }

                if (classEndLine != -1)
                {
                    var insertPosition = sourceText.Lines[classEndLine].Start;

                    // Add new usings if any
                    var usingInsertPoint = existingContent.IndexOf("namespace");
                    if (formattedUsings.Any())
                    {
                        existingContent = existingContent.Insert(usingInsertPoint,
                            string.Join(Environment.NewLine, formattedUsings) + Environment.NewLine + Environment.NewLine);
                        // Cập nhật lại vị trí chèn do đã thêm usings
                        insertPosition += formattedUsings.Sum(u => u.Length + Environment.NewLine.Length) + Environment.NewLine.Length;
                    }

                    // Add new methods
                    fileContent = existingContent.Insert(insertPosition,
                        Environment.NewLine + methodsText + Environment.NewLine);
                }
                else
                {
                    fileContent = existingContent;  // Keep existing content if structure is invalid
                }
            }
            else
            {
                // Create new file with complete structure
                var classModifiers = string.Join(" ",
                    _classNode.Modifiers
                        .Where(m => !m.IsKind(SyntaxKind.PartialKeyword))
                        .Select(m => m.Text))
                    .Trim();
                var className = _classNode.Identifier.Text;
                var baseList = _classNode.BaseList?.ToString() ?? "";

                fileContent = $@"{string.Join(Environment.NewLine, formattedUsings)}

namespace {namespaceText}
{{
    /// <summary>
    /// {description}
    /// (Được tách từ file {Path.GetFileName(sourceFile)})
    /// Ngày tách: {DateTime.Now:dd/MM/yyyy HH:mm:ss}
    /// </summary>
    {classModifiers} partial class {className}{baseList}
    {{
{methodsText}
    }}
}}";
            }

            await File.WriteAllTextAsync(filePath, fileContent);
        }

        private void AnalyzeMemberUsages()
        {
            foreach (var member in _classNode.Members)
            {
                string memberName = GetMemberName(member);
                if (!string.IsNullOrEmpty(memberName))
                {
                    _memberUsages[memberName] = false;
                }
            }

            foreach (var root in _roots.Values)
            {
                var allNodes = root.DescendantNodes();
                foreach (var node in allNodes)
                {
                    if (node is IdentifierNameSyntax identifier)
                    {
                        var name = identifier.Identifier.Text;
                        if (_memberUsages.ContainsKey(name))
                        {
                            _memberUsages[name] = true;
                        }
                    }

                    if (node is MemberAccessExpressionSyntax memberAccess)
                    {
                        var name = memberAccess.Name.Identifier.Text;
                        if (_memberUsages.ContainsKey(name))
                        {
                            _memberUsages[name] = true;
                        }
                    }
                }
            }

            foreach (var member in _classNode.Members)
            {
                string memberName = GetMemberName(member);
                if (!string.IsNullOrEmpty(memberName))
                {
                    if (member is ConstructorDeclarationSyntax ||
                        memberName == "Dispose" ||
                        member.Modifiers.Any(m => m.IsKind(SyntaxKind.OverrideKeyword)))
                    {
                        _memberUsages[memberName] = true;
                    }
                }
            }
        }

        private void AnalyzeCode()
        {
            _codeItems.Clear();

            foreach (var field in _classNode.Members.OfType<FieldDeclarationSyntax>())
            {
                foreach (var variable in field.Declaration.Variables)
                {
                    var item = new CodeItem
                    {
                        Name = variable.Identifier.Text,
                        ItemType = "Field",
                        Length = field.ToString().Length,
                        ReturnType = field.Declaration.Type.ToString(),
                        AccessModifier = field.Modifiers.ToString(),
                        Content = field.ToString(),
                        IsExtracted = false
                    };
                    _codeItems.Add(item);
                }
            }

            foreach (var prop in _classNode.Members.OfType<PropertyDeclarationSyntax>())
            {
                var item = new CodeItem
                {
                    Name = prop.Identifier.Text,
                    ItemType = "Property",
                    Length = prop.ToString().Length,
                    ReturnType = prop.Type.ToString(),
                    AccessModifier = prop.Modifiers.ToString(),
                    Content = prop.ToString(),
                    IsExtracted = false
                };
                _codeItems.Add(item);
            }

            foreach (var method in _classNode.Members.OfType<MethodDeclarationSyntax>())
            {
                var item = new CodeItem
                {
                    Name = method.Identifier.Text,
                    ItemType = "Method",
                    Length = method.ToString().Length,
                    ReturnType = method.ReturnType.ToString(),
                    Parameters = string.Join(", ", method.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}")),
                    AccessModifier = method.Modifiers.ToString(),
                    Content = method.ToString(),
                    IsExtracted = false
                };
                _codeItems.Add(item);
            }

            foreach (var ctor in _classNode.Members.OfType<ConstructorDeclarationSyntax>())
            {
                var item = new CodeItem
                {
                    Name = ctor.Identifier.Text,
                    ItemType = "Constructor",
                    Length = ctor.ToString().Length,
                    Parameters = string.Join(", ", ctor.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}")),
                    AccessModifier = ctor.Modifiers.ToString(),
                    Content = ctor.ToString(),
                    IsExtracted = false
                };
                _codeItems.Add(item);
            }

            _codeItems.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
        }

        private string GetMemberName(MemberDeclarationSyntax member)
        {
            return member switch
            {
                MethodDeclarationSyntax method => method.Identifier.Text,
                PropertyDeclarationSyntax property => property.Identifier.Text,
                FieldDeclarationSyntax field => field.Declaration.Variables.First().Identifier.Text,
                ConstructorDeclarationSyntax ctor => ctor.Identifier.Text,
                _ => string.Empty
            };
        }
        #endregion
    }
}