namespace Altinn.App.Core.Features.Payment.Providers.Nets.Models
{
    public class NetsWebhookEvent
    {
        public string Id { get; set; }
        public int MercantId { get; set; }
        public string Timestamp { get; set; }
        public string Event { get; set; }
        public object Data { get; set; }
    }
}