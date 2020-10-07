using Observatory.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Observatory.Core.Persistence
{
    public interface IProfileDataQuery : IDisposable
    {
        IQueryable<MailFolder> Folders { get; }
        IQueryable<MessageSummary> MessageSummaries { get; }
        IQueryable<MessageDetail> MessageDetails { get; }
    }
}
