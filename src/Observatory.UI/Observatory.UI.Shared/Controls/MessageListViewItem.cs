using Observatory.Core.ViewModels.Mail;
using Observatory.UI.Virtualizing;
using ReactiveUI;
using Splat;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        public static DependencyProperty IsPointerOverProperty { get; } =
            DependencyProperty.Register(nameof(IsPointerOver), typeof(bool), typeof(MessageListViewItem), new PropertyMetadata(false));

        public static DependencyProperty MessageProperty { get; } =
            DependencyProperty.Register(nameof(Message), typeof(MessageSummaryViewModel), typeof(MessageListViewItem), new PropertyMetadata(null));

        public static DependencyProperty StateNameProperty { get; } =
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

        public void Prepare(MessageSummaryViewModel message)
        {
            if (message != null)
            {
                Message = message;
                _stateSubscription = Observable.CombineLatest(
                        message.WhenAnyValue(x => x.IsFlagged),
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
            ClearValue(StateNameProperty);
            Message = null;
        }
    }
}
