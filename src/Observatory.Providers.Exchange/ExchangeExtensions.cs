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
        const string REMOVED_FLAG = "@removed";
        const string DELTA_LINK = "@odata.deltaLink";
        const string NEXT_LINK = "@odata.nextLink";

        public static bool IsRemoved(this MG.Entity graphEntity)
        {
            return graphEntity.AdditionalData?.ContainsKey(REMOVED_FLAG) ?? false;
        }

        public static string GetDeltaLink<T>(this MG.ICollectionPage<T> page)
        {
            return page.AdditionalData[DELTA_LINK].ToString();
        }

        public static string GetNextLink<T>(this MG.ICollectionPage<T> page)
        {
            return page.AdditionalData[NEXT_LINK].ToString();
        }
    }
}
