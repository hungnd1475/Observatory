using Observatory.Core.ViewModels.Mail;
using ReactiveUI;
using Splat;
using System;
using System.Reactive.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Observatory.UI.Controls
{
    public partial class MessageListViewItem : ListViewItem
    {
        public static DependencyProperty ViewModelProperty { get; } =
            DependencyProperty.Register(nameof(ViewModel), typeof(MessageSummaryViewModel), typeof(MessageListViewItem), new PropertyMetadata(null));

        public MessageSummaryViewModel ViewModel
        {
            get { return (MessageSummaryViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public void Prepare(MessageSummaryViewModel item)
        {
            ViewModel = item;
        }

        public void Clear()
        {
            ViewModel = null;
        }
    }
}
