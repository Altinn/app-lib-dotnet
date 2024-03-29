namespace Altinn.App.Core.Models
{
    /// <summary>
    /// Attachment metadata list
    /// </summary>
    public class AttachmentList
    {
        /// <summary>
        /// The attachment type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The attachments
        /// </summary>
        public List<Attachment> Attachments { get; set; }
    }
}
