using Observatory.Core.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;

namespace Observatory.Application.ViewModels.Mail
{
    public class MessageSummaryViewModel : ReactiveObject
    {
        public string Subject => throw new NotImplementedException();

        public string Preview => throw new NotImplementedException();

        public string Correspondents => throw new NotImplementedException();

        public DateTimeOffset ReceivedDateTime => throw new NotImplementedException();

        public bool IsRead => throw new NotImplementedException();

        public Importance Importance => throw new NotImplementedException();

        public bool HasAttachments => throw new NotImplementedException();

        public bool IsFlagged => throw new NotImplementedException();

        public bool IsDraft => throw new NotImplementedException();

        public string ThreadId => throw new NotImplementedException();

        public int ThreadPosition => throw new NotImplementedException();

        public MessageDetailViewModel Detail => throw new NotImplementedException();

        public ReactiveCommand<Unit, Unit> LoadDetailCommand => throw new NotImplementedException();

        public ReactiveCommand<Unit, Unit> ArchiveCommand => throw new NotImplementedException();

        public ReactiveCommand<Unit, Unit> DeleteCommand => throw new NotImplementedException();

        public ReactiveCommand<Unit, Unit> ToggleFlagCommand => throw new NotImplementedException();

        public ReactiveCommand<Unit, Unit> ToggleReadCommand => throw new NotImplementedException();

        public ReactiveCommand<string, Unit> MoveCommand => throw new NotImplementedException();

        public ReactiveCommand<Unit, Unit> MoveToJunkCommand => throw new NotImplementedException();

        public ReactiveCommand<Unit, Unit> IgnoreCommand => throw new NotImplementedException();
    }
}
