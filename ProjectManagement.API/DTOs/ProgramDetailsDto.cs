namespace ProjectManagement.API.DTOs
{
    public class ProgramDetailsDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Budget { get; set; }
        public int Status { get; set; }
        public int ProgressPercentage { get; set; }
        public string SponsorName { get; set; } = string.Empty;
        public string ManagerName { get; set; } = string.Empty;
        public string PortfolioName { get; set; } = string.Empty;
        public int PortfolioId { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<string> AttachedDocumentUrls { get; set; } = new List<string>();

        // العدادات الإحصائية المطلوبة للجدول والبطاقات العلويّة
        public int ProjectsCount { get; set; }
        public int TasksCount { get; set; }
    }
}
