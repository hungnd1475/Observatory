using Observatory.Core.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Observatory.Core.ViewModels.Mail
{
    public class MessageSummaryViewModel : ReactiveObject, IDisposable
    {
        private static readonly Regex NEWLINE_PATTERN = new Regex("\\r?\n|\u200B|\u200C|\u200D", RegexOptions.Compiled);
        private static readonly Regex SPACES_PATTERN = new Regex("\\s\\s+", RegexOptions.Compiled);

        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public string Subject { get; private set; }

        public string Preview { get; private set; }

        public string Correspondents { get; private set; }

        public DateTimeOffset ReceivedDateTime { get; private set; }

        public bool IsRead { get; private set; }

        public Importance Importance { get; private set; }

        public bool HasAttachments { get; private set; }

        [Reactive]
        public bool IsFlagged { get; set; }

        public bool IsDraft { get; private set; }

        public string ThreadId { get; private set; }

        public int ThreadPosition { get; private set; }

        [ObservableAsProperty]
        public bool IsTogglingFlag { get; }

        public MessageDetailViewModel Detail { get; private set; }

        public ReactiveCommand<Unit, Unit> LoadDetailCommand { get; }

        public ReactiveCommand<Unit, Unit> ArchiveCommand { get; }

        public ReactiveCommand<Unit, Unit> DeleteCommand { get; }

        public ReactiveCommand<Unit, Unit> ToggleFlagCommand { get; }

        public ReactiveCommand<Unit, Unit> ToggleReadCommand { get; }

        public ReactiveCommand<string, Unit> MoveCommand { get; }

        public ReactiveCommand<Unit, Unit> MoveToJunkCommand { get; }

        public ReactiveCommand<Unit, Unit> IgnoreCommand { get; }

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

            ToggleFlagCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await Task.Delay(200);
                IsFlagged = !IsFlagged;
            });
            ToggleFlagCommand.IsExecuting
                .ToPropertyEx(this, x => x.IsTogglingFlag)
                .DisposeWith(_disposables);

            ArchiveCommand = ReactiveCommand.CreateFromTask(() =>
            {
                return Task.Delay(500);
            });
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
