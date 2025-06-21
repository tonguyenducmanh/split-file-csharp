using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RemoveUnusedMember
{
    public class CodeRemover
    {
        private readonly List<string> _logMessages = new List<string>();
        public IEnumerable<string> LogMessages => _logMessages;

        private void Log(string message)
        {
            _logMessages.Add(message);
        }

        public async Task<bool> RemoveMembersAsync(string filePath, IEnumerable<UnusedMemberInfo> membersToRemoveInFile, AdhocWorkspace workspace)
        {
            _logMessages.Clear();
            if (!membersToRemoveInFile.Any()) return false;

            Log($"Bắt đầu xóa thành phần trong tệp: {filePath}");

            try
            {
                var document = workspace.CurrentSolution.Projects.SelectMany(p => p.Documents).FirstOrDefault(d => d.FilePath == filePath);
                if (document == null)
                {
                    // Nếu không tìm thấy document trong workspace (ví dụ tệp không nằm trong project đã load), 
                    // thử load trực tiếp syntax tree.
                    // Điều này ít lý tưởng hơn vì không có semantic model đầy đủ.
                    Log($"Không tìm thấy Document cho tệp {filePath} trong workspace. Thử đọc và phân tích trực tiếp (ít tin cậy hơn).");
                    string contentFallback = await File.ReadAllTextAsync(filePath);
                    var treeFallback = CSharpSyntaxTree.ParseText(contentFallback, path: filePath);
                    var rootFallback = await treeFallback.GetRootAsync();
                    var newRootFallback = rootFallback;

                    foreach (var memberInfo in membersToRemoveInFile.OrderByDescending(m => m.LineNumber)) // Xóa từ dưới lên
                    {
                        SyntaxNode? memberNodeToRemove = FindSyntaxNodeByLineNumber(newRootFallback, memberInfo.LineNumber, memberInfo.Type, memberInfo.Name);
                        if (memberNodeToRemove != null)
                        {
                            Log($"  Đang xóa (fallback) {memberInfo.Type} '{memberInfo.Name}' dòng {memberInfo.LineNumber} - FullSpan: {memberNodeToRemove.FullSpan}");
                            newRootFallback = newRootFallback.RemoveNode(memberNodeToRemove, SyntaxRemoveOptions.KeepNoTrivia);
                        }
                        else
                        {
                            Log($"  Không tìm thấy SyntaxNode (fallback) cho {memberInfo.Type} '{memberInfo.Name}' dòng {memberInfo.LineNumber}. Bỏ qua.");
                        }
                    }
                    await File.WriteAllTextAsync(filePath, newRootFallback.ToFullString());
                    Log($"Đã cập nhật (fallback) tệp: {filePath}");
                    return true;
                }

                var tree = await document.GetSyntaxTreeAsync();
                var root = await tree.GetRootAsync();
                if (root == null) 
                {
                    Log($"Không thể lấy SyntaxRoot cho {filePath}.");
                    return false;
                }

                var newRoot = root;

                // Sắp xếp các thành viên cần xóa theo số dòng giảm dần để tránh việc xóa làm thay đổi vị trí các thành viên khác
                foreach (var memberInfo in membersToRemoveInFile.OrderByDescending(m => m.LineNumber))
                {
                    SyntaxNode? memberNodeToRemove = null;
                    // Tìm node dựa trên thông tin từ UnusedMemberInfo (LineNumber, Type, Name)
                    // Đây là một cách tiếp cận, có thể cần cải thiện độ chính xác
                    memberNodeToRemove = FindSyntaxNodeByLineNumber(newRoot, memberInfo.LineNumber, memberInfo.Type, memberInfo.Name);

                    if (memberNodeToRemove != null)
                    {
                        SyntaxNode nodeToActuallyRemove = memberNodeToRemove;

                        if (memberNodeToRemove is VariableDeclaratorSyntax varDeclarator)
                        {
                            if (varDeclarator.Parent is VariableDeclarationSyntax variableDeclaration)
                            {
                                if (variableDeclaration.Variables.Count == 1)
                                {
                                    // Đây là biến duy nhất trong khai báo (ví dụ: string msg = "a"; hoặc private int _count = 0;)
                                    // Chúng ta cần xóa toàn bộ statement cha của VariableDeclarationSyntax
                                    if (variableDeclaration.Parent is LocalDeclarationStatementSyntax localDeclStatement)
                                    {
                                        nodeToActuallyRemove = localDeclStatement;
                                        Log($"  Xóa toàn bộ LocalDeclarationStatement cho variable '{varDeclarator.Identifier.Text}' (từ {memberInfo.Name})");
                                    }
                                    else if (variableDeclaration.Parent is FieldDeclarationSyntax fieldDeclStatement)
                                    {
                                        nodeToActuallyRemove = fieldDeclStatement;
                                        Log($"  Xóa toàn bộ FieldDeclaration cho field '{varDeclarator.Identifier.Text}' (từ {memberInfo.Name})");
                                    }
                                    // Thêm các trường hợp khai báo khác nếu cần, ví dụ: EventFieldDeclarationSyntax
                                    else
                                    {
                                        // Fallback: Nếu không xác định được cha cụ thể, chỉ xóa VariableDeclarator (có thể không lý tưởng)
                                        // Hoặc log và bỏ qua để an toàn hơn.
                                        Log($"  CẢNH BÁO: Không thể xác định statement cha (LocalDeclaration/FieldDeclaration) cho variable '{varDeclarator.Identifier.Text}' (từ {memberInfo.Name}) là biến duy nhất. Xem xét cấu trúc code. Tạm thời chỉ xóa VariableDeclarator.");
                                        // Hoặc: continue;
                                    }
                                }
                                else // Khai báo nhiều biến trên cùng một dòng (ví dụ: int a, b, c;)
                                {
                                    Log($"  CẢNH BÁO: Variable '{varDeclarator.Identifier.Text}' (từ {memberInfo.Name}) là một phần của khai báo nhiều biến ('{variableDeclaration.ToString()}'). Việc xóa một phần sẽ gây lỗi cú pháp. Bỏ qua việc xóa thành phần này để đảm bảo an toàn.");
                                    continue; // Bỏ qua việc xóa node này để tránh lỗi cú pháp
                                }
                            }
                            else
                            {
                                // Trường hợp VariableDeclarator không có cha là VariableDeclarationSyntax (khó xảy ra với C# thông thường)
                                Log($"  CẢNH BÁO: VariableDeclarator '{varDeclarator.Identifier.Text}' (từ {memberInfo.Name}) không có VariableDeclarationSyntax làm cha trực tiếp. Bỏ qua.");
                                continue;
                            }
                        }
                        // Các loại node khác (MethodDeclaration, PropertyDeclaration, etc.) sẽ được xử lý trực tiếp
                        // bằng cách xóa chính memberNodeToRemove.

                        var leadingTrivia = nodeToActuallyRemove.GetLeadingTrivia();
                        var xmlCommentTrivia = leadingTrivia
                            .LastOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) || 
                                                t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));

                        TextSpan spanToRemove = nodeToActuallyRemove.FullSpan;
                        if (xmlCommentTrivia != default(SyntaxTrivia) && xmlCommentTrivia.FullSpan.End <= nodeToActuallyRemove.FullSpan.Start)
                        {                            
                            // Mở rộng span để bao gồm cả comment XML nếu nó nằm ngay trước
                            var combinedSpanStart = Math.Min(xmlCommentTrivia.FullSpan.Start, nodeToActuallyRemove.FullSpan.Start);
                            // Kiểm tra xem có trivia nào khác giữa comment và node không (ví dụ dòng trắng)
                            // Để đơn giản, nếu có dòng trắng, có thể chúng ta không muốn xóa nó cùng.
                            // Tạm thời, chúng ta sẽ tìm vị trí bắt đầu của comment và kết thúc của node.
                            // Tuy nhiên, RemoveNode hoạt động trên node, không phải span. 
                            // Cách tốt hơn là tìm node cha chung và format lại, hoặc xây dựng lại node cha.
                            // Vì RemoveNode chỉ xóa node, việc xóa trivia trước đó cần thủ thuật khác.

                            // Lựa chọn 1: Xóa node, sau đó tìm và xóa trivia nếu có thể (phức tạp)
                            // Lựa chọn 2 (Đơn giản hơn nhưng có thể không hoàn hảo): 
                            // Nếu nodeToActuallyRemove là MethodDeclarationSyntax hoặc FieldDeclarationSyntax,
                            // và có XML doc, thì chúng ta có thể lấy FullSpan của cả hai.
                            // Tuy nhiên, newRoot.RemoveNode không làm việc với span.

                            // Chúng ta sẽ dựa vào việc RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia) 
                            // thường sẽ xóa các trivia gắn liền với node đó (bao gồm cả XML doc nếu nó được coi là leading trivia của node đó).
                            // Nếu XML doc là trivia của một node bao ngoài (vídeo parent), thì nó sẽ không bị xóa.
                            // Đây là một điểm yếu của việc chỉ dùng RemoveNode.
                            Log($"  Node '{memberInfo.Name}' có XML Doc Comment tiềm năng phía trước. SyntaxRemoveOptions.KeepNoTrivia có thể sẽ xử lý.");
                        }

                        Log($"  Đang xóa {memberInfo.Type} '{memberInfo.Name}' (Node: {nodeToActuallyRemove.GetType().Name}) dòng {memberInfo.LineNumber} - FullSpan: {nodeToActuallyRemove.FullSpan}");
                        newRoot = newRoot.RemoveNode(nodeToActuallyRemove, SyntaxRemoveOptions.KeepNoTrivia); 
                    }
                    else
                    {
                        Log($"  Không tìm thấy SyntaxNode cho {memberInfo.Type} '{memberInfo.Name}' dòng {memberInfo.LineNumber} để xóa. Bỏ qua.");
                    }
                }

                // Ghi lại cây cú pháp đã thay đổi vào tệp
                await File.WriteAllTextAsync(filePath, newRoot.ToFullString());
                Log($"Đã cập nhật tệp: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"Lỗi trong quá trình xóa tại tệp {filePath}: {ex.Message} {ex.StackTrace}");
                return false;
            }
        }

        private SyntaxNode? FindSyntaxNodeByLineNumber(SyntaxNode root, int lineNumberOneBased, MemberType memberType, string memberName)
        {
            // lineNumberOneBased là 1-based, vị trí trong Roslyn là 0-based
            int targetLineZeroBased = lineNumberOneBased - 1;

            var candidateNodes = root.DescendantNodes(descendIntoTrivia: true)
                .Where(node => 
                {
                    var lineSpan = node.GetLocation().GetLineSpan();
                    if (!lineSpan.IsValid || lineSpan.Path != root.SyntaxTree.FilePath) return false; // Đảm bảo node thuộc cùng file
                    
                    if (lineSpan.StartLinePosition.Line == targetLineZeroBased) // Bắt đầu trên cùng dòng
                    {
                        switch (memberType)
                        {
                            case MemberType.Method:
                                if (node is MethodDeclarationSyntax methodNode && methodNode.Identifier.Text == memberName.Split('.').Last()) return true;
                                break;
                            case MemberType.Field: 
                                if (node is VariableDeclaratorSyntax fieldNode && fieldNode.Identifier.Text == memberName.Split('.').Last()) 
                                {
                                    // Trả về VariableDeclaratorSyntax để logic bên trên quyết định xóa FieldDeclarationSyntax hay chỉ VariableDeclaratorSyntax
                                    return true; 
                                }
                                if (node is FieldDeclarationSyntax fds && fds.Declaration.Variables.Count == 1 && fds.Declaration.Variables[0].Identifier.Text == memberName.Split('.').Last()) 
                                {
                                    // Nếu FieldDeclaration chỉ chứa một biến trùng tên, trả về nó
                                    return true; // Vẫn trả về VariableDeclarator để thống nhất
                                }
                                break;
                            case MemberType.Variable: 
                                // memberName cho biến cục bộ có dạng "MethodName.VariableName"
                                if (node is VariableDeclaratorSyntax varDeclNode && varDeclNode.Identifier.Text == memberName.Split('.').Last()) return true;
                                break;
                            // Thêm các case cho Property, Event, v.v. nếu cần
                        }
                    }
                    return false;
                }).ToList();
            
            // Nếu có nhiều node trên cùng 1 dòng, cố gắng chọn node gần nhất với tên
            // Đây là heuristic, có thể cần SemanticModel để xác định chính xác symbol
            if (candidateNodes.Count > 1) 
            {
                 return candidateNodes.FirstOrDefault(n => n.ToString().Contains(memberName.Split('.').Last())) ?? candidateNodes.First();
            }
            return candidateNodes.FirstOrDefault();
        }
    }
}