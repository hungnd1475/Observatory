using Observatory.Core.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Text;

namespace Observatory.Core.ViewModels.Mail
{
    public class MessageDetailViewModel : ReactiveObject
    {
        public string Subject => throw new NotImplementedException();

        public Recipient Sender => throw new NotImplementedException();

        public DateTimeOffset ReceivedDateTime => throw new NotImplementedException();

        public bool IsRead => throw new NotImplementedException();

        public Importance Importance => throw new NotImplementedException();

        public bool HasAttachment => throw new NotImplementedException();

        public ReadOnlyObservableCollection<Recipient> CcRecipients => throw new NotImplementedException();

        public ReadOnlyObservableCollection<Recipient> ToRecipients => throw new NotImplementedException();

        public bool IsDraft => throw new NotImplementedException();

        public bool IsFlagged => throw new NotImplementedException();

        public ReactiveCommand<Unit, Unit> ArchiveCommand => throw new NotImplementedException();

        public ReactiveCommand<Unit, Unit> DeleteCommand => throw new NotImplementedException();

        public ReactiveCommand<Unit, Unit> ToggleFlagCommand => throw new NotImplementedException();

        public ReactiveCommand<Unit, Unit> ToggleReadCommand => throw new NotImplementedException();

        public ReactiveCommand<string, Unit> MoveCommand => throw new NotImplementedException();

        public ReactiveCommand<Unit, Unit> MoveToJunkCommand => throw new NotImplementedException();
    }
}
