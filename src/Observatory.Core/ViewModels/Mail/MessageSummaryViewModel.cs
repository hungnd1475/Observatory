using Observatory.Core.Models;
using Observatory.Core.Persistence;
using Observatory.Core.Services;
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
using System.Threading.Tasks;

namespace Observatory.Core.ViewModels.Mail
{
    public class MessageSummaryViewModel : ReactiveObject, IVirtualizableTarget<MessageSummary, string>, IDisposable
    {
        private static readonly Regex NEWLINE_PATTERN = new Regex("\\r?\n|\u200B|\u200C|\u200D", RegexOptions.Compiled);
        private static readonly Regex SPACES_PATTERN = new Regex("\\s\\s+", RegexOptions.Compiled);

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

        [ObservableAsProperty]
        public bool IsTogglingFlag { get; }

        public MessageDetailViewModel Detail => _detail.Value;

        public ReactiveCommand<Unit, Unit> Archive { get; }

        public ReactiveCommand<Unit, Unit> Delete { get; }

        public ReactiveCommand<Unit, Unit> ToggleFlag { get; }

        public ReactiveCommand<Unit, Unit> ToggleRead { get; }

        public ReactiveCommand<Unit, Unit> Move { get; }

        public ReactiveCommand<Unit, Unit> MoveToJunk { get; }

        public MessageSummaryViewModel(MessageSummary state,
            MessageListViewModel container,
            IProfileDataQueryFactory queryFactory,
            IMailService mailService)
        {
            _detail = new Lazy<MessageDetailViewModel>(() => new MessageDetailViewModel(state, queryFactory));
            Id = state.Id;
            Refresh(state);

            ToggleFlag = ReactiveCommand.CreateFromObservable(() =>
            {
                var command = IsFlagged ? container.ClearFlag : container.SetFlag;
                IsFlagged = !IsFlagged;
                return command.Execute(new[] { Id });
            });
            ToggleFlag.IsExecuting
                .ToPropertyEx(this, x => x.IsTogglingFlag)
                .DisposeWith(_disposables);
            ToggleFlag.ThrownExceptions
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(ex =>
                {
                    IsFlagged = !IsFlagged;
                    this.Log().Error(ex);
                })
                .DisposeWith(_disposables);

            ToggleRead = ReactiveCommand.CreateFromTask(async () =>
            {
                IsRead = !IsRead;
                await mailService.UpdateMessage(Id)
                    .Set(m => m.IsRead, IsRead)
                    .ExecuteAsync();
            });
            ToggleRead.ThrownExceptions
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(ex =>
                {
                    IsRead = !IsRead;
                    this.Log().Error(ex);
                })
                .DisposeWith(_disposables);

            Archive = ReactiveCommand.CreateFromTask(() =>
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
            if (_detail.IsValueCreated)
            {
                _detail.Value.Dispose();
            }
            _disposables.Dispose();
        }
    }
}
