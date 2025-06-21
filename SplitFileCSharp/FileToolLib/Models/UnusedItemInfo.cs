namespace LongFileChecker.Models
{
    public class UnusedItemInfo
    {
        public string FilePath { get; set; }
        public string ClassName { get; set; }
        public string ItemName { get; set; }
        public string Type { get; set; }
        public int Length { get; set; }
        public string AccessModifier { get; set; }
        public bool Selected { get; set; } = true;

        public override string ToString()
        {
            return $"{ClassName}.{ItemName} ({Type}) - {Path.GetFileName(FilePath)}";
        }
    }
}