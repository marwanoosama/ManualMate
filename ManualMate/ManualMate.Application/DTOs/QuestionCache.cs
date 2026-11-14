namespace ManualMate.Application.DTOs
{
    public record QuestionCache
    {
        public int ItemId { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }
    }
}
