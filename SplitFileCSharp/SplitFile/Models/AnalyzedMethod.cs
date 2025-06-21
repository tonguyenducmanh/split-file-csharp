using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SplitFile.Models
{
    public class AnalyzedMethod
    {
        /// <summary>
        /// Tên method
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// File chứa method
        /// </summary>
        public string SourceFile { get; set; }

        /// <summary>
        /// Tên file không bao gồm đường dẫn
        /// </summary>
        public string SourceFileName => Path.GetFileName(SourceFile);

        /// <summary>
        /// Độ dài code của method
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// Kiểu trả về
        /// </summary>
        public string ReturnType { get; set; }

        /// <summary>
        /// Danh sách tham số
        /// </summary>
        public string Parameters { get; set; }

        /// <summary>
        /// Modifiers (public, private, etc.)
        /// </summary>
        public string AccessModifier { get; set; }

        /// <summary>
        /// Nội dung đầy đủ của method
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Node gốc của method trong syntax tree
        /// </summary>
        public MethodDeclarationSyntax Node { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public AnalyzedMethod(string name, string sourceFile, MethodDeclarationSyntax node)
        {
            Name = name;
            SourceFile = sourceFile;
            Node = node;
            Content = node.ToString();
            Length = Content.Length;
            ReturnType = node.ReturnType.ToString();
            Parameters = string.Join(", ", node.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}"));
            AccessModifier = string.Join(" ", node.Modifiers);
        }
    }
}