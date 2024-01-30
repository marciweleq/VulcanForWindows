using System;
using System.Linq;
using System.Collections.Generic;
using Vulcanova.Core.Data;
using Vulcanova.Uonet.Api.MessageBox;

namespace Vulcanova.Features.Messages;

public class Message
{
    public AccountEntityId<Guid> Id { get; set; }
    public Guid GlobalKey { get; set; }
    public Guid ThreadKey { get; set; }
    public Guid MessageBoxId { get; set; }
    public MessageBoxFolder Folder { get; set; }
    public string Subject { get; set; }
    public string Content { get; set; }
    public DateTime DateSent { get; set; }
    public DateTime? DateRead { get; set; }
    public int Status { get; set; }
    public Correspondent Sender { get; set; }
    public List<Correspondent> Receiver { get; set; }
    public List<Attachment> Attachments { get; set; }
    public int Importance { get; set; }

    public bool HasAttachments => Attachments.Count > 0;
    public string AttachmentsText => $"{Attachments.Count} {(Attachments.Count == 1 ? "za��cznik" : ((Attachments.Count > 4) ? "za��cznik�w" : "za��czniki"))}";
    public string AttachmentsTooltip => string.Join(", ", Attachments.Select(r => r.Name));

}