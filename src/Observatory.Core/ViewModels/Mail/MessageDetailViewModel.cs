﻿using Observatory.Core.Models;
using Observatory.Core.Persistence;
using Observatory.Core.Persistence.Specifications;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace Observatory.Core.ViewModels.Mail
{
    public class MessageDetailViewModel : ReactiveObject, IDisposable
    {
        private readonly string _id, _folderId;
        private readonly IProfileDataQueryFactory _queryFactory;
        private IDisposable _loadSubscription = null;

        [Reactive]
        public string Subject { get; private set; }

        [Reactive]
        public string Sender { get; private set; }

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
        public IReadOnlyList<string> CcRecipients { get; private set; }

        [Reactive]
        public IReadOnlyList<string> ToRecipients { get; private set; }

        [Reactive]
        public bool IsDraft { get; private set; }

        [Reactive]
        public bool IsFlagged { get; private set; }

        [Reactive]
        public string Body { get; private set; }

        [Reactive]
        public ContentType BodyType { get; private set; }

        [Reactive]
        public bool IsLoading { get; private set; }

        public MessageDetailViewModel(MessageSummary summary, IProfileDataQueryFactory queryFactory)
        {
            _id = summary.Id;
            _folderId = summary.FolderId;
            _queryFactory = queryFactory;
            Refresh(summary);
        }

        public void Refresh(MessageSummary summary)
        {
            IsLoading = true;
            Subject = summary.Subject;
            IsRead = summary.IsRead.Value;
            Importance = summary.Importance.Value;
            HasAttachments = summary.HasAttachments.Value;
            IsDraft = summary.IsDraft.Value;
            IsFlagged = summary.IsFlagged.Value;
            Sender = FormatRecipient(summary.Sender, true);
            CcRecipients = FormatRecipients(summary.CcRecipients);
            ToRecipients = FormatRecipients(summary.ToRecipients);
            ReceivedDateTime = summary.ReceivedDateTime.Value;
            FormattedReceivedDateTime = FormatReceivedDateTime(summary.ReceivedDateTime.Value);
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
                return query.MessageDetails.FirstOrDefault(m => m.Id == _id && m.FolderId == _folderId);
            }, RxApp.TaskpoolScheduler)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(_ => IsLoading = false)
            .Where(x => x != null)
            .Subscribe(x =>
            {
                Body = x.Body;
                BodyType = x.BodyType.Value;
            });
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

        private IReadOnlyList<string> FormatRecipients(IReadOnlyList<Recipient> recipients)
        {
            return recipients.Select((r, i) =>
            {
                return i == recipients.Count - 1
                    ? FormatRecipient(r, false)
                    : FormatRecipient(r, false) + ";";
            })
            .ToList().AsReadOnly();
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
            _loadSubscription?.Dispose();
        }
    }
}
