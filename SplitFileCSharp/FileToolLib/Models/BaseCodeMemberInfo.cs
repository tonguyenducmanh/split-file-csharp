using Microsoft.CodeAnalysis; // Added
using Microsoft.CodeAnalysis.Text; // Cần cho TextSpan

namespace FileToolLib.Models
{
    public class BaseCodeMemberInfo
    {
        /// <summary>
        /// Đường dẫn đầy đủ đến tệp chứa thành viên này.
        /// </summary>
        public string FilePath { get; private set; }

        /// <summary>
        /// Tên của thành viên (ví dụ: "MyMethod", "_myField", "MyProperty").
        /// </summary>
        public string MemberName { get; private set; }

        /// <summary>
        /// Loại thành viên (ví dụ: "Method", "Field", "Property").
        /// Có thể dùng enum sau này nếu cần.
        /// </summary>
        public string MemberType { get; private set; }

        /// <summary>
        /// Vị trí (Span) của khai báo thành viên trong cây cú pháp của tệp.
        /// Dùng để lấy lại SyntaxNode hoặc ISymbol sau này nếu cần.
        /// </summary>
        public TextSpan DeclarationSpan { get; private set; }

        /// <summary>
        /// Visibility của member (ví dụ: "private", "public").
        /// Quan trọng để chỉ kiểm tra các member "private".
        /// </summary>
        public string AccessModifier { get; private set; }

        /// <summary>
        /// Tên của class chứa member này (có thể bao gồm namespace nếu cần phân biệt rõ ràng)
        /// </summary>
        public string ContainingTypeName { get; private set; }

        public BaseCodeMemberInfo(string filePath, string memberName, string memberType, TextSpan declarationSpan, string accessModifier, string containingTypeName)
        {
            FilePath = filePath;
            MemberName = memberName;
            MemberType = memberType;
            DeclarationSpan = declarationSpan;
            AccessModifier = accessModifier;
            ContainingTypeName = containingTypeName;
        }
    }
}