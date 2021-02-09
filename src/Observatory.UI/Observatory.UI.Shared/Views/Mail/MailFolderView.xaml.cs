using Observatory.Core.Models;
using Observatory.Core.ViewModels.Mail;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Reactive.Disposables;
using Windows.ApplicationModel.Store;
using Uno.Extensions;
using Uno.Logging;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Controls.Primitives;
using Microsoft.Toolkit.Uwp.UI.Extensions;

namespace Observatory.UI.Views.Mail
{
    public sealed partial class MailFolderView : UserControl, IViewFor<MailFolderViewModel>
    {
        public static DependencyProperty ViewModelProperty { get; } =
            DependencyProperty.Register(nameof(ViewModel), typeof(MailFolderViewModel), typeof(MailFolderView), new PropertyMetadata(null));

        public static DependencyProperty SelectedMessageProperty { get; } =
            DependencyProperty.Register(nameof(SelectedMessage), typeof(MessageSummaryViewModel), typeof(MailFolderView), new PropertyMetadata(null));

        public MailFolderViewModel ViewModel
        {
            get { return (MailFolderViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public MessageSummaryViewModel SelectedMessage
        {
            get { return (MessageSummaryViewModel)GetValue(SelectedMessageProperty); }
            set { SetValue(SelectedMessageProperty, value); }
        }

        object IViewFor.ViewModel 
        {
            get => ViewModel;
            set => ViewModel = (MailFolderViewModel)value;
        }

        public MailFolderView()
        {
            this.InitializeComponent();
            var selectedMessageBinding = new Binding()
            {
                Source = this,
                Path = new PropertyPath(nameof(SelectedMessage)),
                Mode = BindingMode.TwoWay,
            };

#if NETFX_CORE
            FolderNameShadow.Receivers.Add(MessageListGrid);
#endif

            this.WhenActivated(disposables =>
            {
                this.OneWayBind(ViewModel,
                        x => x.Messages.Cache,
                        x => x.MessageList.ItemsSource,
                        value => value?.ToNative())
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                        x => x.Messages.IsSelecting,
                        x => x.MessageList.SelectionMode,
                        value => value ? ListViewSelectionMode.Multiple : ListViewSelectionMode.Single)
                    .DisposeWith(disposables);

                this.WhenAnyValue(x => x.ViewModel.Messages.IsSelecting)
                    .DistinctUntilChanged()
                    .Do(isSelecting =>
                    {
                        if (isSelecting)
                        {
                            MessageList.ClearValue(Selector.SelectedValueProperty);
                            SelectedMessage = null;
                        }
                        else
                        {
                            BindingOperations.SetBinding(MessageList, 
                                Selector.SelectedValueProperty, 
                                selectedMessageBinding);
                        }
                    })
                    .Subscribe()
                    .DisposeWith(disposables);

                this.WhenAnyValue(x => x.ViewModel)
                    .DistinctUntilChanged()
                    .Do(_ => SelectedMessage = null)
                    .Subscribe()
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                        x => x.Messages.SelectionCount,
                        x => x.SelectionCountTextBlock.Text,
                        value => $"({value})")
                    .DisposeWith(disposables);

                this.WhenAnyValue(
                        x => x.ViewModel.Messages.IsSelecting,
                        x => x.ViewModel.Messages.SelectionCount,
                        (isSelecting, selectionCount) => isSelecting && selectionCount > 0)
                    .DistinctUntilChanged()
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindTo(this, x => x.SelectionCountTextBlock.Visibility)
                    .DisposeWith(disposables);

                this.WhenAnyValue(x => x.ViewModel.Messages.Cache)
                    .DistinctUntilChanged()
                    .Do(_ => SelectAllCheckBox.IsChecked = false)
                    .Subscribe()
                    .DisposeWith(disposables);
            });
        }

        public void SelectAllCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SelectAllCheckBox.IsChecked ?? false)
            {
                MessageList.SelectAll();
            }
            else
            {
                MessageList.DeselectAll();
            }
        }

        public void FilterRadioButton_Click(object sender, RoutedEventArgs e)
        {
            var filter = (MessageFilter)(sender as FrameworkElement).Tag;
            ViewModel.Messages.Filter = filter;
        }

        public void SortRadioButton_Click(object sender, RoutedEventArgs e)
        {
            var order = (MessageOrder)(sender as FrameworkElement).Tag;
            ViewModel.Messages.Order = order;
        }

        public string FormatFilterText(MessageFilter filter) => "Show: " + filter switch
        {
            MessageFilter.None => "All",
            MessageFilter.Unread => "Unread",
            MessageFilter.Flagged => "Flagged",
            _ => throw new NotSupportedException(),
        };
    }
}
