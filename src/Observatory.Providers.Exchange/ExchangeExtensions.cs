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
        public static MailFolder Create(this MG.MailFolder folder, FolderType type = FolderType.None, bool isFavorite = false)
        {
            return new MailFolder()
            {
                Id = folder.Id,
                Name = folder.DisplayName,
                ParentId = folder.ParentFolderId,
                Type = type,
                IsFavorite = isFavorite,
            };
        }

        public static void Update(this EntityEntry<MailFolder> folderEntry, MG.MailFolder folder)
        {
            folderEntry.UpdateIfChanged(f => f.Name, folder.DisplayName);
            folderEntry.UpdateIfChanged(f => f.ParentId, folder.ParentFolderId);
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

        public static MessageSummary ConvertToSummary(this MG.Message message)
        {
            return new MessageSummary()
            {
                Id = message.Id ?? throw new ArgumentNullException(),
                Subject = message.Subject ?? throw new ArgumentNullException(),
                Sender = message.Sender?.Convert() ?? throw new ArgumentNullException(),
                ReceivedDateTime = message.ReceivedDateTime ?? throw new ArgumentNullException(),
                IsRead = message.IsRead ?? throw new ArgumentNullException(),
                Importance = message.Importance?.Convert() ?? throw new ArgumentNullException(),
                HasAttachments = message.HasAttachments ?? throw new ArgumentNullException(),
                ToRecipients = message.ToRecipients?.Convert() ?? throw new ArgumentNullException(),
                CcRecipients = message.CcRecipients?.Convert() ?? throw new ArgumentNullException(),
                ThreadId = message.ConversationId ?? throw new ArgumentNullException(),
                IsDraft = message.IsDraft ?? throw new ArgumentNullException(),
                FolderId = message.ParentFolderId ?? throw new ArgumentNullException(),
                BodyPreview = message.BodyPreview ?? throw new ArgumentNullException(),
                IsFlagged = message.Flag?.Convert() ?? throw new ArgumentNullException(),
            };
        }

        public static MessageSummary Update(this MessageSummary state, MG.Message message)
        {
            state.Id = message.Id ?? state.Id;
            state.Subject = message.Subject ?? state.Subject;
            state.Sender = message.Sender.Convert();
            state.ReceivedDateTime = message.ReceivedDateTime ?? state.ReceivedDateTime;
            state.IsRead = message.IsRead ?? state.IsRead;
            state.Importance = message.Importance?.Convert() ?? state.Importance;
            state.HasAttachments = message.HasAttachments ?? state.HasAttachments;
            state.ToRecipients = message.ToRecipients?.Convert() ?? state.ToRecipients;
            state.CcRecipients = message.CcRecipients?.Convert() ?? state.CcRecipients;
            state.ThreadId = message.ConversationId ?? state.ThreadId;
            state.IsDraft = message.IsDraft ?? state.IsDraft;
            state.FolderId = message.ParentFolderId ?? state.FolderId;
            state.BodyPreview = message.BodyPreview ?? state.BodyPreview;
            state.IsFlagged = message.Flag?.Convert() ?? state.IsFlagged;
            return state;
        }

        public static MessageDetail ConvertToDetail(this MG.Message message)
        {
            if (message.Body != null)
            {
                return new MessageDetail()
                {
                    Id = message.Id,
                    Body = message.Body.Content ?? throw new ArgumentNullException(),
                    BodyType = message.Body.ContentType?.Convert() ?? throw new ArgumentNullException(),
                };
            }
            return null;
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
