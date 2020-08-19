using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Bot.Schema;

namespace Bot_Before_Chime
{
    public class AttachmentData
    {
        public List<Attachment> Attachments { get; set; } = new List<Attachment>();
        public string AttachmentLayout { get; set; } = AttachmentLayoutTypes.List;

        public bool HasAttachments => Attachments.Count > 0;

        public AttachmentData(params Attachment[] attachments)
        {
            Attachments = (attachments ?? new Attachment[0]).ToList();
        }

        public AttachmentData([NotNull] IEnumerable<Attachment> attachments)
        {
            Attachments = attachments.ToList();
        }

        public AttachmentData(string layout, params Attachment[] attachments)
        {
            AttachmentLayout = layout;
            Attachments = (attachments ?? new Attachment[0]).ToList();
        }
        public AttachmentData(string layout, [NotNull] IEnumerable<Attachment> attachments)
        {
            AttachmentLayout = layout;
            Attachments = attachments.ToList();
        }
    }
}
