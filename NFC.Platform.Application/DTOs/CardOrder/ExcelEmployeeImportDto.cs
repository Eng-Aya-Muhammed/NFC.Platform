namespace NFC.Platform.Application.DTOs.CardOrder
{
    public class ExcelEmployeeImportDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
    }
}
