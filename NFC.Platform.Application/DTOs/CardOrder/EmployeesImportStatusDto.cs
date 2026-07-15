namespace NFC.Platform.Application.DTOs.CardOrder
{
    public class EmployeesImportStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public int Imported { get; set; }
        public int Skipped { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
