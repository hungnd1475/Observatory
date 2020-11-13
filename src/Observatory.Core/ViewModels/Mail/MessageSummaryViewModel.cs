using Observatory.Core.Models;
using Observatory.Core.Persistence;
using Observatory.Core.Virtualization;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Observatory.Core.ViewModels.Mail
{
    public class MessageSummaryViewModel : ReactiveObject, IVirtualizable<MessageSummary>, IDisposable
    {
        private static readonly Regex NEWLINE_PATTERN = new Regex("\\r?\n|\u200B|\u200C|\u200D", RegexOptions.Compiled);
        private static readonly Regex SPACES_PATTERN = new Regex("\\s\\s+", RegexOptions.Compiled);

        private readonly IProfileDataQueryFactory _queryFactory;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly Lazy<MessageDetailViewModel> _detail;

        [Reactive]
        public string Subject { get; private set; }

        [Reactive]
        public string Preview { get; private set; }

        [Reactive]
        public string Correspondents { get; private set; }

        [Reactive]
        public DateTimeOffset ReceivedDateTime { get; private set; }

        [Reactive]
        public string FormattedReceivedDateTime { get; private set; }

        [Reactive]
        public bool IsRead { get; private set; }

        [Reactive]
        public Importance Importance { get; private set; }

        [Reactive]
        public bool HasAttachments { get; private set; }

        [Reactive]
        public bool IsFlagged { get; set; }

        [Reactive]
        public bool IsDraft { get; private set; }

        [Reactive]
        public string ThreadId { get; private set; }

        [Reactive]
        public int ThreadPosition { get; private set; }

        [ObservableAsProperty]
        public bool IsTogglingFlag { get; }

        public MessageDetailViewModel Detail => _detail.Value;

        public ReactiveCommand<Unit, Unit> LoadDetailCommand { get; }

        public ReactiveCommand<Unit, Unit> ArchiveCommand { get; }

        public ReactiveCommand<Unit, Unit> DeleteCommand { get; }

        public ReactiveCommand<Unit, Unit> ToggleFlagCommand { get; }

        public ReactiveCommand<Unit, Unit> ToggleReadCommand { get; }

        public ReactiveCommand<string, Unit> MoveCommand { get; }

        public ReactiveCommand<Unit, Unit> MoveToJunkCommand { get; }

        public ReactiveCommand<Unit, Unit> IgnoreCommand { get; }

        public MessageSummaryViewModel(MessageSummary state, IProfileDataQueryFactory queryFactory)
        {
            _queryFactory = queryFactory;
            _detail = new Lazy<MessageDetailViewModel>(() => new MessageDetailViewModel(state, queryFactory));
            Refresh(state);

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

        public void Refresh(MessageSummary state)
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
            FormattedReceivedDateTime = FormatReceivedDateTime(state.ReceivedDateTime);

            if (_detail.IsValueCreated)
            {
                _detail.Value.Refresh(state);
            }
        }

        public string FormatReceivedDateTime(DateTimeOffset receivedDateTime)
        {
            var now = DateTimeOffset.Now;
            if (now.Date == receivedDateTime.Date)
            {
                return receivedDateTime.ToString("t");
            }

            var delta = now.DateTime - receivedDateTime.DateTime;
            if (delta < TimeSpan.FromDays(7))
            {
                var shortTimeFormatter = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern;
                return receivedDateTime.ToString($"ddd {shortTimeFormatter}");
            }

            return receivedDateTime.ToString("g");
        }

        public void Dispose()
        {
            _disposables.Dispose();
            if (_detail.IsValueCreated)
            {
                _detail.Value.Dispose();
            }
        }
    }
}
