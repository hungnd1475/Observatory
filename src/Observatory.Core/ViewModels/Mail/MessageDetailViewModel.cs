using Observatory.Core.Models;
using Observatory.Core.Persistence;
using Observatory.Core.Persistence.Specifications;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

namespace Observatory.Core.ViewModels.Mail
{
    public class MessageDetailViewModel : ReactiveObject, IDisposable
    {
        private readonly string _id;
        private readonly IProfileDataQueryFactory _queryFactory;
        private IDisposable _loadSubscription = null;

        [Reactive]
        public string Subject { get; set; }

        [Reactive]
        public Recipient Sender { get; private set; }

        [Reactive]
        public DateTimeOffset ReceivedDateTime { get; private set; }

        [Reactive]
        public bool IsRead { get; private set; }

        [Reactive]
        public Importance Importance { get; private set; }

        [Reactive]
        public bool HasAttachments { get; private set; }

        [Reactive]
        public ObservableCollection<Recipient> CcRecipients { get; private set; }

        [Reactive]
        public ObservableCollection<Recipient> ToRecipients { get; private set; }

        [Reactive]
        public bool IsDraft { get; private set; }

        [Reactive]
        public bool IsFlagged { get; private set; }

        [Reactive]
        public string Body { get; set; }

        [Reactive]
        public ContentType BodyType { get; private set; }

        [Reactive]
        public bool IsLoading { get; private set; }

        public MessageDetailViewModel(MessageSummary summary, IProfileDataQueryFactory queryFactory)
        {
            _id = summary.Id;
            _queryFactory = queryFactory;
            Refresh(summary);
        }

        public void Refresh(MessageSummary summary)
        {
            IsLoading = true;
            Subject = summary.Subject;
            IsRead = summary.IsRead;
            Importance = summary.Importance;
            HasAttachments = summary.HasAttachments;
            IsDraft = summary.IsDraft;
            IsFlagged = summary.IsFlagged;
            Sender = summary.Sender;
            CcRecipients = new ObservableCollection<Recipient>(summary.CcRecipients);
            ToRecipients = new ObservableCollection<Recipient>(summary.ToRecipients);
            ReceivedDateTime = summary.ReceivedDateTime;
            Body = "";
            BodyType = ContentType.Html;
            LoadBody();
        }

        private void LoadBody()
        {
            IsLoading = true;
            _loadSubscription?.Dispose();
            _loadSubscription = Observable.Start(() =>
            {
                using var query = _queryFactory.Connect();
                return query.MessageDetails.FirstOrDefault(m => m.Id == _id);
            }, RxApp.TaskpoolScheduler)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(_ => IsLoading = false)
            .Where(x => x != null)
            .Subscribe(x =>
            {
                Body = x.Body;
                BodyType = x.BodyType;
            });
        }

        public void Dispose()
        {
            _loadSubscription?.Dispose();
        }
    }
}
