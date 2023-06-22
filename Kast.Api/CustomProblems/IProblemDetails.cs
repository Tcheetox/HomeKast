namespace Kast.Api.Problems
{
    internal interface IProblemDetails
    {
        public string? Title { get; set; }
        public int? Status { get; set; }
        public string? Description { get; set; }
    }
}