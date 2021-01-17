using Observatory.Core.ViewModels.Mail;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Observatory.UI.Controls
{
    public class MessageListView : ListView
    {
        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            base.ClearContainerForItemOverride(element, item);
            ((MessageListViewItem)element).Clear();
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new MessageListViewItem();
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
            ((MessageListViewItem)element).Prepare(item as MessageSummaryViewModel);
        }

        protected override bool IsItemItsOwnContainerOverride(object item) => false;
    }
}
