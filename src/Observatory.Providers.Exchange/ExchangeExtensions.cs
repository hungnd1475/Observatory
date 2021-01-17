using Microsoft.Graph;
using Observatory.Core.Models;
using Observatory.Providers.Exchange.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Observatory.Providers.Exchange
{
    public static class ExchangeExtensions
    {
        const string REMOVED_FLAG = "@removed";
        const string DELTA_LINK = "@odata.deltaLink";
        const string NEXT_LINK = "@odata.nextLink";

        public static bool IsRemoved(this Entity graphEntity)
        {
            return graphEntity.AdditionalData?.ContainsKey(REMOVED_FLAG) ?? false;
        }

        public static string GetDeltaLink<T>(this ICollectionPage<T> page)
        {
            return page.AdditionalData[DELTA_LINK].ToString();
        }

        public static string GetNextLink<T>(this ICollectionPage<T> page)
        {
            return page.AdditionalData[NEXT_LINK].ToString();
        }

        public static string ToFolderId(this FolderType type)
        {
            return type switch
            {
                FolderType.Inbox => ExchangeMailService.SpecialFolders.INBOX,
                FolderType.DeletedItems => ExchangeMailService.SpecialFolders.DELETED_ITEMS,
                FolderType.Archive => ExchangeMailService.SpecialFolders.ARCHIVE,
                FolderType.Junk => ExchangeMailService.SpecialFolders.JUNK,
                _ => throw new NotSupportedException($"Folder type {type} is not supported."),
            };
        }

        public static BatchRequestStep ToBatchStep(this IBaseRequest request, string requestId,
            List<string> dependsOn = null)
        {
            return new BatchRequestStep(requestId, request.GetHttpRequestMessage(), dependsOn);
        }

        public static BatchRequestStep ToBatchStep(this IBaseRequest request, string requestId,
            HttpMethod method, HttpContent content, List<string> dependsOn = null)
        {
            var httpRequest = request.GetHttpRequestMessage();
            httpRequest.Method = method;
            httpRequest.Content = content;
            return new BatchRequestStep(requestId, httpRequest, dependsOn);
        }
    }
}
