using Observatory.Core.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Text.RegularExpressions;

namespace Observatory.Core.ViewModels.Mail
{
    public class MessageSummaryViewModel : ReactiveObject
    {
        private static readonly Regex NEWLINE_PATTERN = new Regex("\\r?\n|\u200B|\u200C|\u200D", RegexOptions.Compiled);
        private static readonly Regex SPACES_PATTERN = new Regex("\\s\\s+", RegexOptions.Compiled);

        public string Subject { get; private set; }

        public string Preview { get; private set; }

        public string Correspondents { get; private set; }

        public DateTimeOffset ReceivedDateTime { get; private set; }

        public bool IsRead { get; private set; }

        public Importance Importance { get; private set; }

        public bool HasAttachments { get; private set; }

        public bool IsFlagged { get; private set; }

        public bool IsDraft { get; private set; }

        public string ThreadId { get; private set; }

        public int ThreadPosition { get; private set; }

        public MessageDetailViewModel Detail { get; private set; }

        public ReactiveCommand<Unit, Unit> LoadDetailCommand => throw new NotImplementedException();

        public ReactiveCommand<Unit, Unit> ArchiveCommand => throw new NotImplementedException();

        public ReactiveCommand<Unit, Unit> DeleteCommand => throw new NotImplementedException();

        public ReactiveCommand<Unit, Unit> ToggleFlagCommand => throw new NotImplementedException();

        public ReactiveCommand<Unit, Unit> ToggleReadCommand => throw new NotImplementedException();

        public ReactiveCommand<string, Unit> MoveCommand => throw new NotImplementedException();

        public ReactiveCommand<Unit, Unit> MoveToJunkCommand => throw new NotImplementedException();

        public ReactiveCommand<Unit, Unit> IgnoreCommand => throw new NotImplementedException();

        public MessageSummaryViewModel(MessageSummary state)
        {
            Subject = state.Subject;
            IsRead = state.IsRead;
            Importance = state.Importance;
            HasAttachments = state.HasAttachments;
            IsDraft = state.IsDraft;
            Preview = state.BodyPreview != null 
                ? SPACES_PATTERN.Replace(NEWLINE_PATTERN.Replace(state.BodyPreview, " "), " ") 
                : null;
            IsFlagged = state.IsFlagged;
            Correspondents = state.IsDraft 
                ? string.Join(", ", state.ToRecipients.Select(r => r.DisplayName))
                : state.Sender.DisplayName;
            ReceivedDateTime = state.ReceivedDateTime;
        }
    }
}
