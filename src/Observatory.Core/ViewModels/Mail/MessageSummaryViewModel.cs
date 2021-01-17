using Observatory.Core.Models;
using Observatory.Core.Persistence;
using Observatory.Core.Virtualization;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using System;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.RegularExpressions;

namespace Observatory.Core.ViewModels.Mail
{
    public class MessageSummaryViewModel : ReactiveObject, IVirtualizableTarget<MessageSummary, string>, IDisposable
    {
        private static readonly Regex NEWLINE_PATTERN = new Regex("\\r?\n|\u200B|\u200C|\u200D", RegexOptions.Compiled);
        private static readonly Regex SPACES_PATTERN = new Regex("\\s\\s+", RegexOptions.Compiled);

        private readonly SerialDisposable _markingAsReadSubscription = new SerialDisposable();
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly Lazy<MessageDetailViewModel> _detail;

        public string Id { get; }

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

        public MessageDetailViewModel Detail => _detail.Value;

        public ReactiveCommand<Unit, Unit> Archive { get; }

        public ReactiveCommand<Unit, Unit> Delete { get; }

        public ReactiveCommand<Unit, Unit> ToggleFlag { get; }

        public ReactiveCommand<Unit, Unit> ToggleRead { get; }

        public ReactiveCommand<Unit, Unit> Move { get; }

        public ReactiveCommand<Unit, Unit> MoveToJunk { get; }

        public MessageSummaryViewModel(MessageSummary state,
            MessageListViewModel list,
            IProfileDataQueryFactory queryFactory)
        {
            _detail = new Lazy<MessageDetailViewModel>(() => new MessageDetailViewModel(state, queryFactory));
            Id = state.Id;
            Refresh(state);

            Archive = ReactiveCommand.CreateFromObservable(() => list.Archive.Execute(new[] { Id }));

            Delete = ReactiveCommand.CreateFromObservable(() => list.Delete.Execute(new[] { Id }));

            ToggleFlag = ReactiveCommand.CreateFromObservable(() =>
            {
                var command = IsFlagged ? list.ClearFlag : list.SetFlag;
                IsFlagged = !IsFlagged;
                return command.Execute(new[] { Id });
            });
            ToggleFlag.ThrownExceptions
                .ObserveOn(RxApp.MainThreadScheduler)
                .Do(_ => IsFlagged = !IsFlagged)
                .Subscribe()
                .DisposeWith(_disposables);

            ToggleRead = ReactiveCommand.CreateFromObservable(() =>
            {
                var command = IsRead ? list.MarkAsUnread : list.MarkAsRead;
                IsRead = !IsRead;
                return command.Execute(new[] { Id })
                    .Do(_ => _markingAsReadSubscription.Disposable = null);
            });
            ToggleRead.ThrownExceptions
                .ObserveOn(RxApp.MainThreadScheduler)
                .Do(_ => IsRead = !IsRead)
                .Subscribe()
                .DisposeWith(_disposables);

            Move = ReactiveCommand.CreateFromObservable(() => list.Move.Execute(new[] { Id }));

            MoveToJunk = ReactiveCommand.CreateFromObservable(() => list.Move.Execute(new[] { Id }));
        }

        public void StartMarkingAsRead(int seconds = 0)
        {
            if (IsRead) return;
            _markingAsReadSubscription.Disposable = Observable
                .Timer(TimeSpan.FromSeconds(seconds))
                .Select(_ => Unit.Default)
                .ObserveOn(RxApp.MainThreadScheduler)
                .InvokeCommand(ToggleRead);
        }

        public void StopMarkingAsRead()
        {
            _markingAsReadSubscription.Disposable = null;
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
            if (_detail.IsValueCreated)
            {
                _detail.Value.Dispose();
            }
            _disposables.Dispose();
        }
    }
}
