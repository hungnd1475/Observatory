using Observatory.Core.ViewModels.Mail;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Observatory.UI.Views.Mail
{
    public sealed partial class MessageSummaryView : UserControl, IViewFor<MessageSummaryViewModel>
    {
        public const string STATE_NORMAL = "Normal";
        public const string STATE_FLAGGED = "Flagged";
        public const string STATE_POINTER_OVER = "PointerOver";
        public const string STATE_SELECTED = "Selected";

        public static DependencyProperty ViewModelProperty { get; } =
            DependencyProperty.Register(nameof(ViewModel), typeof(MessageSummaryViewModel), typeof(MessageSummaryView), new PropertyMetadata(null));

        public static DependencyProperty IsPointerOverProperty { get; } =
            DependencyProperty.Register(nameof(IsPointerOver), typeof(bool), typeof(MessageSummaryView), new PropertyMetadata(false));

        public static DependencyProperty IsSelectedProperty { get; } =
            DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(MessageSummaryView), new PropertyMetadata(false));

        public static DependencyProperty StateNameProperty { get; } =
            DependencyProperty.Register(nameof(StateName), typeof(string), typeof(MessageSummaryView), new PropertyMetadata(STATE_NORMAL));

        public MessageSummaryViewModel ViewModel 
        {
            get => (MessageSummaryViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value); 
        }

        object IViewFor.ViewModel 
        {
            get => ViewModel;
            set => ViewModel = (MessageSummaryViewModel)value;
        }

        public bool IsPointerOver
        {
            get => (bool)GetValue(IsPointerOverProperty);
            set => SetValue(IsPointerOverProperty, value);
        }

        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        public string StateName
        {
            get => (string)GetValue(StateNameProperty);
            set => SetValue(StateNameProperty, value);
        }

        public MessageSummaryView()
        {
            this.InitializeComponent();
            this.WhenActivated(disposables =>
            {
                Observable.CombineLatest(
                    this.WhenAnyValue(x => x.ViewModel.IsFlagged),
                    this.WhenAnyValue(x => x.IsPointerOver),
                    this.WhenAnyValue(x => x.IsSelected),
                    (isFlagged, isPointerOver, isSelected) => (IsFlagged: isFlagged, IsPointerOver: isPointerOver, IsSelected: isSelected))
                .Select(state =>
                {
                    if (state.IsSelected)
                    {
                        return STATE_SELECTED;
                    }
                    else if (state.IsPointerOver)
                    {
                        return STATE_POINTER_OVER;
                    }
                    else if (state.IsFlagged)
                    {
                        return STATE_FLAGGED;
                    }
                    else
                    {
                        return STATE_NORMAL;
                    }
                })
                .BindTo(this, x => x.StateName)
                .DisposeWith(disposables);
            });
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
    }
}
