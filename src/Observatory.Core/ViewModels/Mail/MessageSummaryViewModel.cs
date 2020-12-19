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
        private readonly string _folderId;

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

        public ReactiveCommand<string, Unit> Move { get; }

        public ReactiveCommand<Unit, Unit> MoveToJunk { get; }

        public MessageSummaryViewModel(MessageSummary state,
            IProfileDataQueryFactory queryFactory,
            IMailService mailService)
        {
            _detail = new Lazy<MessageDetailViewModel>(() => new MessageDetailViewModel(state, queryFactory));
            _folderId = state.FolderId;
            Id = state.Id;
            Refresh(state);

            ToggleFlag = ReactiveCommand.CreateFromTask(async () =>
            {
                IsFlagged = !IsFlagged;
                await mailService.UpdateMessage(_folderId, Id)
                    .Set(m => m.IsFlagged, IsFlagged)
                    .ExecuteAsync();
            });
            ToggleFlag.IsExecuting
                .ToPropertyEx(this, x => x.IsTogglingFlag)
                .DisposeWith(_disposables);
            ToggleFlag.ThrownExceptions
                .Subscribe(ex =>
                {
                    IsFlagged = !IsFlagged;
                    this.Log().Error(ex);
                })
                .DisposeWith(_disposables);

            ToggleRead = ReactiveCommand.CreateFromTask(async () =>
            {
                IsRead = !IsRead;
                await mailService.UpdateMessage(_folderId, Id)
                    .Set(m => m.IsRead, IsRead)
                    .ExecuteAsync();
            });
            ToggleRead.ThrownExceptions
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
            IsRead = state.IsRead.Value;
            Importance = state.Importance.Value;
            HasAttachments = state.HasAttachments.Value;
            IsDraft = state.IsDraft.Value;
            Preview = state.BodyPreview != null
                ? SPACES_PATTERN.Replace(NEWLINE_PATTERN.Replace(state.BodyPreview, " "), " ")
                : null;
            IsFlagged = state.IsFlagged.Value;
            Correspondents = state.IsDraft.Value
                ? string.Join(", ", state.ToRecipients.Select(r => r.DisplayName))
                : state.Sender.DisplayName;
            ReceivedDateTime = state.ReceivedDateTime.Value;
            FormattedReceivedDateTime = FormatReceivedDateTime(state.ReceivedDateTime.Value);

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
