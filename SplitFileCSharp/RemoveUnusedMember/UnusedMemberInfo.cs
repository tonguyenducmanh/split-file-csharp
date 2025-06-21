namespace RemoveUnusedMember
{
    public enum MemberType
    {
        Method,
        Variable,
        Property, // Có thể mở rộng sau
        Field, // Có thể mở rộng sau
        Event,
        Other
    }

    public class UnusedMemberInfo
    {
        public bool IsSelected { get; set; } = true; // Mặc định là check
        public MemberType Type { get; }
        public string Name { get; }
        public string FilePath { get; }
        public int LineNumber { get; }
        public string ContainingType { get; } // Namespace.ClassName cho methods/fields, hoặc Namespace.ClassName.MethodName cho local vars
        public string Accessibility { get; } // Thêm thuộc tính này

        public UnusedMemberInfo(MemberType type, string name, string filePath, int lineNumber, string containingType, string accessibility = "") // Thêm vào constructor
        {
            Type = type;
            Name = name;
            FilePath = filePath;
            LineNumber = lineNumber;
            ContainingType = containingType;
            Accessibility = accessibility; // Gán giá trị
        }
    }
}