namespace Altinn.App.Core.Models
{
    public class ProcessChangeResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorType { get; set; }
        
        public ProcessStateChange? ProcessStateChange { get; set; }
    }
}
