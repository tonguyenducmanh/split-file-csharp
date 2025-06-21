namespace LongFileChecker.Models
{
    public class UnusedItemInfo
    {
        public string FilePath { get; set; }
        public string ClassName { get; set; } // Tên class chứa item
        public string ItemName { get; set; }  // Tên của method, property, field
        public string Type { get; set; }      // "Method", "Property", "Field"
        public long Length { get; set; }
        public string AccessModifier { get; set; }

        // Thông tin gốc từ CodeItemLongFile để đối chiếu hoặc tạo lại nếu cần
        public int OriginalStart { get; set; } // OriginalStartOffset từ CodeItemLongFile
        public int OriginalLength { get; set; } // OriginalFullSpanLength từ CodeItemLongFile

        // Tham chiếu trực tiếp đến CodeItemLongFile gốc
        // Đây là cách tốt nhất để truyền lại cho RemoveMembersAsync
        public CodeItemLongFile CodeItemReference { get; set; }

        // Thuộc tính để hiển thị trên DataGridView (ví dụ)
        public string DisplayName => string.IsNullOrEmpty(ClassName) || ClassName == ItemName ? ItemName : $"{ClassName}.{ItemName}";
        
        // Thuộc tính để binding với CheckBox trên DataGridView
        public bool IsSelected { get; set; } = true; // Mặc định là chọn để xóa
    }
}