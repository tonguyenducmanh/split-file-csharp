using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using FileToolLib.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

namespace FileToolLib.Services
{
    public class ReferenceAnalysisService
    {
        /// <summary>
        /// Tìm các thành viên private không được sử dụng trong một CSharpCompilation.
        /// </summary>
        /// <param name="compilation">Compilation chứa tất cả các SyntaxTree liên quan (ví dụ: các phần của partial class).</param>
        /// <param name="candidatePrivateMembers">Danh sách các BaseCodeMemberInfo của các thành viên private cần kiểm tra.</param>
        /// <returns>Danh sách các BaseCodeMemberInfo thực sự không được sử dụng.</returns>
        public async Task<List<BaseCodeMemberInfo>> FindUnusedPrivateMembersAsync(
            CSharpCompilation compilation,
            IEnumerable<BaseCodeMemberInfo> candidatePrivateMembers)
        {
            var unusedMembers = new List<BaseCodeMemberInfo>();

            if (compilation == null || !candidatePrivateMembers.Any())
            {
                return unusedMembers;
            }

            // Kiểm tra lỗi compilation trước khi phân tích có thể hữu ích
            var diagnostics = compilation.GetDiagnostics();
            if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                Debug.WriteLine("ReferenceAnalysisService: Compilation có lỗi. Kết quả phân tích tham chiếu có thể không chính xác.");
                foreach (var diagnostic in diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                {
                    Debug.WriteLine($"  - {diagnostic.GetMessage()} tại {diagnostic.Location}");
                }
                // Quyết định: tiếp tục phân tích hay throw lỗi? Tạm thời tiếp tục và ghi log.
            }

            foreach (var candidateMemberInfo in candidatePrivateMembers)
            {
                if (candidateMemberInfo.AccessModifier?.ToLowerInvariant() != "private")
                {
                    continue; // Chỉ quan tâm private members
                }

                SyntaxTree memberTree = compilation.SyntaxTrees.FirstOrDefault(t => t.FilePath == candidateMemberInfo.FilePath);
                if (memberTree == null)
                {
                    Debug.WriteLine($"Không tìm thấy SyntaxTree cho file: {candidateMemberInfo.FilePath} trong compilation.");
                    continue;
                }

                var rootNode = await memberTree.GetRootAsync();
                var memberNodeSyntax = rootNode.DescendantNodes(candidateMemberInfo.DeclarationSpan)
                                             .OfType<MemberDeclarationSyntax>() // Đảm bảo nó là một MemberDeclaration
                                             .FirstOrDefault(n => n.Span == candidateMemberInfo.DeclarationSpan);

                if (memberNodeSyntax == null)
                {
                    Debug.WriteLine($"Không tìm thấy MemberDeclarationSyntax tại vị trí cho: {candidateMemberInfo.MemberName} trong {candidateMemberInfo.FilePath}");
                    continue;
                }

                var semanticModel = compilation.GetSemanticModel(memberTree);
                ISymbol memberSymbol = semanticModel.GetDeclaredSymbol(memberNodeSyntax);

                if (memberSymbol == null)
                {
                    Debug.WriteLine($"Không thể lấy ISymbol cho: {candidateMemberInfo.MemberName} trong {candidateMemberInfo.FilePath}");
                    continue;
                }

                // Bỏ qua các phương thức là entry point (Main)
                if (IsEntryPoint(memberSymbol))
                {
                    continue;
                }

                // Bỏ qua các phương thức thực thi interface hoặc override
                if (IsImplementingInterfaceOrOverriding(memberSymbol))
                {
                    continue;
                }

                // Bỏ qua các phương thức xử lý sự kiện (event handlers) có thể được gọi động bởi UI framework
                if (IsPotentialEventHandler(memberSymbol, memberNodeSyntax))
                {
                    continue;
                }

                // Bỏ qua các constructor đặc biệt (static constructor)
                if (memberSymbol is IMethodSymbol method && method.MethodKind == MethodKind.StaticConstructor)
                {
                    continue;
                }

                bool isReferenced = false;
                // Sử dụng SymbolFinder để tìm tham chiếu hiệu quả hơn
                // Cần một Solution để dùng SymbolFinder, có thể tạo AdhocWorkspace nếu cần thiết
                // Hoặc, chúng ta có thể lặp thủ công qua tất cả các IdentifierNameSyntax trong compilation như cách đã làm

                // Cách thủ công (có thể chậm hơn SymbolFinder với solution lớn nhưng không cần AdhocWorkspace):
                foreach (var syntaxTreeInCompilation in compilation.SyntaxTrees)
                {
                    var modelForSearch = compilation.GetSemanticModel(syntaxTreeInCompilation);
                    var rootForSearch = await syntaxTreeInCompilation.GetRootAsync();

                    foreach (var identifierNode in rootForSearch.DescendantNodes().OfType<IdentifierNameSyntax>())
                    {
                        // Tránh chính vị trí khai báo của symbol đó
                        var parentDeclaration = identifierNode.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().FirstOrDefault();
                        if (parentDeclaration == memberNodeSyntax)
                        {
                            // Nếu identifierNode là tên của member trong chính khai báo của nó, bỏ qua
                            if (identifierNode.Identifier.ValueText == memberSymbol.Name &&
                               (parentDeclaration is MethodDeclarationSyntax mds && mds.Identifier == identifierNode.Identifier ||
                                parentDeclaration is PropertyDeclarationSyntax pds && pds.Identifier == identifierNode.Identifier ||
                                parentDeclaration is FieldDeclarationSyntax fds && fds.Declaration.Variables.Any(v => v.Identifier == identifierNode.Identifier)))
                            {
                                continue;
                            }
                        }

                        var symbolInfo = modelForSearch.GetSymbolInfo(identifierNode);
                        ISymbol referencedSymbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();

                        if (referencedSymbol != null && SymbolEqualityComparer.Default.Equals(referencedSymbol.OriginalDefinition, memberSymbol.OriginalDefinition))
                        {
                            isReferenced = true;
                            break;
                        }
                    }
                    if (isReferenced) break;
                }


                if (!isReferenced)
                {
                    unusedMembers.Add(candidateMemberInfo);
                }
            }

            return unusedMembers;
        }

        private bool IsEntryPoint(ISymbol memberSymbol)
        {
            if (memberSymbol is IMethodSymbol methodSymbol && methodSymbol.IsStatic && methodSymbol.Name == "Main")
            {
                var parameters = methodSymbol.Parameters;
                if ((parameters.Length == 0 ||
                     (parameters.Length == 1 && parameters[0].Type.ToDisplayString() == "string[]")) &&
                    (methodSymbol.ReturnType.SpecialType == SpecialType.System_Void ||
                     methodSymbol.ReturnType.SpecialType == SpecialType.System_Int32))
                {
                    return true; // Coi là Main method
                }
            }
            return false;
        }

        private bool IsImplementingInterfaceOrOverriding(ISymbol memberSymbol)
        {
            if (memberSymbol is IMethodSymbol methodSymbol)
            {
                if (methodSymbol.IsOverride || methodSymbol.ExplicitInterfaceImplementations.Any()) return true;
                // Kiểm tra kế thừa ngầm
                if (memberSymbol.ContainingType != null)
                {
                    foreach (var iface in memberSymbol.ContainingType.AllInterfaces)
                    {
                        foreach (var ifaceMember in iface.GetMembers(memberSymbol.Name).OfType<IMethodSymbol>())
                        {
                            // Sử dụng SymbolEqualityComparer.Default
                            ISymbol implementation = memberSymbol.ContainingType.FindImplementationForInterfaceMember(ifaceMember);
                            if (implementation != null && SymbolEqualityComparer.Default.Equals(implementation.OriginalDefinition, methodSymbol.OriginalDefinition))
                                return true;
                        }
                    }
                }
            }
            else if (memberSymbol is IPropertySymbol propertySymbol)
            {
                if (propertySymbol.IsOverride || propertySymbol.ExplicitInterfaceImplementations.Any()) return true;
                if (memberSymbol.ContainingType != null)
                {
                    foreach (var iface in memberSymbol.ContainingType.AllInterfaces)
                    {
                        foreach (var ifaceMember in iface.GetMembers(memberSymbol.Name).OfType<IPropertySymbol>())
                        {
                            // Sử dụng SymbolEqualityComparer.Default
                            ISymbol implementation = memberSymbol.ContainingType.FindImplementationForInterfaceMember(ifaceMember);
                            if (implementation != null && SymbolEqualityComparer.Default.Equals(implementation.OriginalDefinition, propertySymbol.OriginalDefinition))
                                return true;
                        }
                    }
                }
            }
            // Có thể mở rộng cho Events nếu cần
            return false;
        }

        /// <summary>
        /// Heuristic để kiểm tra xem một private method có thể là event handler hay không.
        /// Điều này không hoàn hảo nhưng có thể giảm false positives.
        /// </summary>
        private bool IsPotentialEventHandler(ISymbol memberSymbol, MemberDeclarationSyntax memberNodeSyntax)
        {
            if (memberSymbol is IMethodSymbol methodSymbol && methodSymbol.DeclaredAccessibility == Accessibility.Private && methodSymbol.ReturnsVoid)
            {
                // 1. Tên phương thức thường theo mẫu: ControlName_EventName (ví dụ: btnSave_Click)
                if (methodSymbol.Name.Contains("_"))
                {
                    // 2. Tham số thường là (object sender, EventArgs e) hoặc các kiểu kế thừa EventArgs
                    if (methodSymbol.Parameters.Length == 2)
                    {
                        var param1Type = methodSymbol.Parameters[0].Type;
                        var param2Type = methodSymbol.Parameters[1].Type;
                        if (param1Type.SpecialType == SpecialType.System_Object &&
                            (param2Type.ToDisplayString() == "System.EventArgs" || param2Type.BaseType?.ToDisplayString() == "System.EventArgs" || param2Type.AllInterfaces.Any(i => i.ToDisplayString() == "System.EventArgs")))
                        {
                            // 3. Kiểm tra xem có attribute nào liên quan đến UI không (ví dụ, nếu bạn dùng attributes để gán event handlers)
                            // Điều này tùy thuộc vào framework.
                            // 4. Kiểm tra xem method có được gán cho một event trong code không.
                            // Điều này phức tạp hơn, cần phân tích cú pháp sử dụng += operator.
                            // Hiện tại, heuristic này dựa trên tên và signature.
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}