using Observatory.Core.ViewModels.Mail;
using ReactiveUI;
using Splat;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using Uno.Extensions.ValueType;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace Observatory.UI.Controls
{
    public partial class MessageListViewItem : ListViewItem, IEnableLogger
    {
        public static readonly DependencyProperty IsPointerOverProperty =
            DependencyProperty.Register(nameof(IsPointerOver), typeof(bool), typeof(MessageListViewItem), new PropertyMetadata(false));

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(MessageSummaryViewModel), typeof(MessageListViewItem), new PropertyMetadata(null));

        public static readonly DependencyProperty StateNameProperty =
            DependencyProperty.Register(nameof(StateName), typeof(string), typeof(MessageListViewItem), new PropertyMetadata("Normal"));

        private IDisposable _stateSubscription;

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

        public string StateName
        {
            get { return (string)GetValue(StateNameProperty); }
            set { SetValue(StateNameProperty, value); }
        }

        public MessageListViewItem()
        {
        }

        public void Prepare(MessageSummaryViewModel message)
        {
            Message = message;
            _stateSubscription = Observable.CombineLatest(message.WhenAnyValue(x => x.IsFlagged),
                    this.WhenAnyValue(x => x.IsPointerOver),
                    this.WhenAnyValue(x => x.IsSelected),
                    (isFlagged, isPointerOver, isSelected) => (IsFlagged: isFlagged, IsPointerOver: isPointerOver, IsSelected: isSelected))
                .Select(state =>
                {
                    if (state.IsSelected)
                    {
                        return "Selected";
                    }
                    else if (state.IsPointerOver)
                    {
                        return "PointerOver";
                    }
                    else if (state.IsFlagged)
                    {
                        return "Flagged";
                    }
                    else
                    {
                        return "Normal";
                    }
                })
                .BindTo(this, x => x.StateName);
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
            _stateSubscription?.Dispose();
            this.ClearValue(StateNameProperty);
            Message = null;
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
    }
}
