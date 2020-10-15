using Microsoft.EntityFrameworkCore.ChangeTracking;
using Observatory.Core.Models;
using Observatory.Providers.Exchange.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using MG = Microsoft.Graph;

namespace Observatory.Providers.Exchange
{
    public static class ExchangeExtensions
    {
        public static MailFolder Convert(this MG.MailFolder source, FolderType type = FolderType.None, bool isFavorite = false)
        {
            return new MailFolder()
            {
                Id = source.Id,
                Name = source.DisplayName,
                ParentId = source.ParentFolderId,
                Type = type,
                IsFavorite = isFavorite,
            };
        }

        public static void UpdateFrom(this EntityEntry<MailFolder> targetEntry, MG.MailFolder source)
        {
            targetEntry.UpdateIfChanged(f => f.Name, source.DisplayName);
            targetEntry.UpdateIfChanged(f => f.ParentId, source.ParentFolderId);
        }

        public static void UpdateIfChanged<TEntity, TValue>(this EntityEntry<TEntity> entityEntry,
            Expression<Func<TEntity, TValue>> propertyExpression,
            TValue value) where TEntity: class
        {
            if (value != null)
            {
                entityEntry.Property(propertyExpression).CurrentValue = value;
            }
        }

        public static MessageSummary ConvertToSummary(this MG.Message source)
        {
            return new MessageSummary()
            {
                Id = source.Id ?? throw new ArgumentNullException(),
                Subject = source.Subject ?? throw new ArgumentNullException(),
                Sender = source.Sender?.Convert() ?? throw new ArgumentNullException(),
                ReceivedDateTime = source.ReceivedDateTime ?? throw new ArgumentNullException(),
                IsRead = source.IsRead ?? throw new ArgumentNullException(),
                Importance = source.Importance?.Convert() ?? throw new ArgumentNullException(),
                HasAttachments = source.HasAttachments ?? throw new ArgumentNullException(),
                ToRecipients = source.ToRecipients?.Convert() ?? throw new ArgumentNullException(),
                CcRecipients = source.CcRecipients?.Convert() ?? throw new ArgumentNullException(),
                ThreadId = source.ConversationId ?? throw new ArgumentNullException(),
                IsDraft = source.IsDraft ?? throw new ArgumentNullException(),
                FolderId = source.ParentFolderId ?? throw new ArgumentNullException(),
                BodyPreview = source.BodyPreview ?? throw new ArgumentNullException(),
                IsFlagged = source.Flag?.Convert() ?? throw new ArgumentNullException(),
            };
        }

        public static void UpdateFrom(this EntityEntry<MessageSummary> targetEntry, MG.Message source)
        {
            targetEntry.UpdateIfChanged(s => s.Subject, source.Subject);
            targetEntry.UpdateIfChanged(s => s.Sender, source.Sender?.Convert());
            targetEntry.UpdateIfChanged(s => s.ReceivedDateTime, source.ReceivedDateTime);
            targetEntry.UpdateIfChanged(s => s.IsRead, source.IsRead);
            targetEntry.UpdateIfChanged(s => s.Importance, source.Importance?.Convert());
            targetEntry.UpdateIfChanged(s => s.HasAttachments, source.HasAttachments);
            targetEntry.UpdateIfChanged(s => s.ToRecipients, source.ToRecipients?.Convert());
            targetEntry.UpdateIfChanged(s => s.CcRecipients, source.CcRecipients?.Convert());
            targetEntry.UpdateIfChanged(s => s.ThreadId, source.ConversationId);
            targetEntry.UpdateIfChanged(s => s.IsDraft, source.IsDraft);
            targetEntry.UpdateIfChanged(s => s.FolderId, source.ParentFolderId);
            targetEntry.UpdateIfChanged(s => s.BodyPreview, source.BodyPreview);
            targetEntry.UpdateIfChanged(s => s.IsFlagged, source.Flag?.Convert());
        }

        public static MessageDetail ConvertToDetail(this MG.Message source)
        {
            if (source.Body != null)
            {
                return new MessageDetail()
                {
                    Id = source.Id,
                    Body = source.Body.Content ?? throw new ArgumentNullException(),
                    BodyType = source.Body.ContentType?.Convert() ?? throw new ArgumentNullException(),
                };
            }
            return null;
        }

        public static void UpdateFrom(this EntityEntry<MessageDetail> targetEntry, MG.Message source)
        {
            targetEntry.UpdateIfChanged(d => d.Body, source.Body.Content);
            targetEntry.UpdateIfChanged(d => d.BodyType, source.Body.ContentType?.Convert());
        }

        public static ContentType? Convert(this MG.BodyType bodyType)
        {
            return bodyType switch
            {
                MG.BodyType.Html => ContentType.Html,
                MG.BodyType.Text => ContentType.Text,
                _ => null,
            };
        }

        public static Importance? Convert(this MG.Importance importance)
        {
            return importance switch
            {
                MG.Importance.Low => Importance.Low,
                MG.Importance.Normal => Importance.Normal,
                MG.Importance.High => Importance.High,
                _ => null,
            };
        }

        public static Recipient Convert(this MG.Recipient recipient)
        {
            return new Recipient()
            {
                 EmailAddress = recipient.EmailAddress.Address,
                 DisplayName = recipient.EmailAddress.Name,
            };
        }

        public static List<Recipient> Convert(this IEnumerable<MG.Recipient> recipients)
        {
            return recipients.Select(r => r.Convert()).ToList();
        }

        public static bool Convert(this MG.FollowupFlag flag)
        {
            return flag.FlagStatus.HasValue && flag.FlagStatus.Value == MG.FollowupFlagStatus.Flagged;
        }

        public static bool IsRemoved(this MG.Entity graphEntity)
        {
            return graphEntity.AdditionalData?.ContainsKey(ExchangeMailService.REMOVED_FLAG) ?? false;
        }

        public static string GetDeltaLink<T>(this MG.ICollectionPage<T> page)
        {
            return page.AdditionalData[ExchangeMailService.DELTA_LINK].ToString();
        }

        public static string GetNextLink<T>(this MG.ICollectionPage<T> page)
        {
            return page.AdditionalData[ExchangeMailService.NEXT_LINK].ToString();
        }
    }
}
