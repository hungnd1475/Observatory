using Observatory.Core.Models;
using Observatory.Core.Persistence;
using Observatory.Core.Persistence.Specifications;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Core.ViewModels.Mail
{
    public class MessageDetailViewModel : ReactiveObject, IDisposable
    {
        private readonly string _id;
        private readonly IProfileDataQueryFactory _queryFactory;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private IDisposable _loadSubscription = null;

        public string Subject { get; }

        public string Sender { get; }

        public DateTimeOffset ReceivedDateTime { get; }

        public string FormattedReceivedDateTime { get; }

        public bool IsRead { get; }

        public Importance Importance { get; }

        public bool HasAttachments { get; }

        public IReadOnlyList<string> CcRecipients { get; }

        public IReadOnlyList<string> ToRecipients { get; }

        public bool IsDraft { get; }

        public bool IsFlagged { get; }

        [Reactive]
        public string Body { get; private set; }

        [Reactive]
        public ContentType BodyType { get; private set; }

        [Reactive]
        public bool IsLoading { get; private set; }

        public ReactiveCommand<Unit, Unit> Archive { get; }

        public ReactiveCommand<Unit, Unit> Delete { get; }

        public ReactiveCommand<Unit, Unit> ToggleFlag { get; }

        public ReactiveCommand<Unit, Unit> ToggleRead { get; }

        public ReactiveCommand<string, Unit> Move { get; }

        public ReactiveCommand<Unit, Unit> MoveToJunk { get; }

        public MessageDetailViewModel(MessageSummary summary, IProfileDataQueryFactory queryFactory)
        {
            _id = summary.Id;
            _queryFactory = queryFactory;

            Subject = summary.Subject;
            IsRead = summary.IsRead;
            Importance = summary.Importance;
            HasAttachments = summary.HasAttachments;
            IsDraft = summary.IsDraft;
            IsFlagged = summary.IsFlagged;
            Sender = FormatRecipient(summary.Sender, true);
            CcRecipients = summary.CcRecipients.Select((r, i) =>
            {
                return i == summary.CcRecipients.Count - 1
                    ? FormatRecipient(r, false)
                    : FormatRecipient(r, false) + ";";
            }).ToList().AsReadOnly();
            ToRecipients = summary.ToRecipients.Select((r, i) =>
            {
                return i == summary.ToRecipients.Count - 1
                    ? FormatRecipient(r, false)
                    : FormatRecipient(r, false) + ";";
            }).ToList().AsReadOnly();
            ReceivedDateTime = summary.ReceivedDateTime;
            FormattedReceivedDateTime = FormatReceivedDateTime(summary.ReceivedDateTime);
            Body = "";
            BodyType = ContentType.Html;
        }

        public void LoadIfUninitialized()
        {
            if (_loadSubscription == null)
            {
                IsLoading = true;
                _loadSubscription = Observable.Start(() =>
                {
                    using var query = _queryFactory.Connect();
                    return query.MessageDetails.FirstOrDefault(m => m.Id == _id);
                }, RxApp.TaskpoolScheduler)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(r =>
                {
                    if (r != null)
                    {
                        Body = r.Body;
                        BodyType = r.BodyType;
                    }
                    IsLoading = false;
                });
            }
        }

        private string FormatRecipient(Recipient recipient, bool isFull)
        {
            if (string.IsNullOrEmpty(recipient.DisplayName))
            {
                return recipient.EmailAddress;
            }
            else if (isFull)
            {
                return $"{recipient.DisplayName} <{recipient.EmailAddress}>";
            }
            else
            {
                return recipient.DisplayName;
            }
        }

        private string FormatReceivedDateTime(DateTimeOffset receivedDateTime)
        {
            var now = DateTimeOffset.Now;
            if (now.Date == receivedDateTime.Date)
            {
                return receivedDateTime.ToString("hh:mm tt");
            }

            return receivedDateTime.ToString("g");
        }

        public void Dispose()
        {
            _disposables.Dispose();
            _loadSubscription?.Dispose();
        }
    }
}
