using Observatory.Core.ViewModels.Mail;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using Uno.Extensions.ValueType;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Observatory.UI.Shared.Controls
{
    public class MessageListViewItem : ListViewItem
    {
        public static readonly DependencyProperty IsPointerOverProperty =
            DependencyProperty.Register(nameof(IsPointerOver), typeof(bool), typeof(MessageListViewItem), new PropertyMetadata(false));

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(MessageSummaryViewModel), typeof(MessageListViewItem), new PropertyMetadata(null));

        private readonly CompositeDisposable _disposables =
            new CompositeDisposable();

        public MessageSummaryViewModel Message
        {
            get { return (MessageSummaryViewModel)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        public bool IsPointerOver
        {
            get { return (bool)GetValue(IsPointerOverProperty); }
            set { SetValue(IsPointerOverProperty, value); }
        }

        public void Prepare(MessageSummaryViewModel message)
        {
            Message = message;
        }

        protected override void OnPointerEntered(PointerRoutedEventArgs e)
        {
            base.OnPointerEntered(e);
            IsPointerOver = true;
        }

        protected override void OnPointerExited(PointerRoutedEventArgs e)
        {
            base.OnPointerExited(e);
            IsPointerOver = false;
        }

        public void Clear()
        {
            _disposables.Dispose();
            Message = null;
        }

        public FontWeight ConvertIsReadToFontWeight(bool isRead)
        {
            return isRead ? FontWeights.Normal : FontWeights.SemiBold;
        }

        public string DisplayReceivedDateTime(DateTimeOffset receivedDateTime)
        {
            var now = DateTimeOffset.Now;
            if (now.Date == receivedDateTime.Date)
            {
                return receivedDateTime.ToString("hh:mm tt");
            }

            var delta = now.DateTime - receivedDateTime.DateTime;
            if (delta < TimeSpan.FromDays(7))
            {
                return receivedDateTime.ToString("ddd hh:mm tt");
            }

            if (now.Year == receivedDateTime.Year)
            {
                return receivedDateTime.ToString("dd/MM hh:mm tt");
            }

            return receivedDateTime.ToString("dd/MM/yyyy hh:mm tt");
        }

        public Visibility ShouldDisplayFlaggedBackground(bool isFlagged, bool isPointerOver, bool isSelected)
        {
            if (isPointerOver || isSelected)
            {
                return Visibility.Collapsed;
            }

            return isFlagged ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
