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
#if NETFX_CORE
            FolderNameShadow.Receivers.Add(MessageListGrid);
#endif

            this.WhenActivated(disposables =>
            {
                var viewModel = this.WhenAnyValue(x => x.ViewModel)
                    .Publish().RefCount();

                this.OneWayBind(ViewModel,
                        x => x.Messages.Cache,
                        x => x.MessageList.ItemsSource,
                        value => value?.ToNative())
                    .DisposeWith(disposables);

#if NETFX_CORE
                this.OneWayBind(ViewModel,
                        x => x.Messages.Filter,
                        x => x.ShowButton.Label,
                        value => value switch
                        {
                            MessageFilter.None => "Show: All",
                            MessageFilter.Unread => "Show: Unread",
                            MessageFilter.Flagged => "Show: Flagged",
                            _ => throw new NotSupportedException(),
                        })
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                        x => x.Messages.Filter,
                        x => x.FilterAllRadio.IsChecked,
                        value => value == MessageFilter.None)
                    .DisposeWith(disposables);
                this.OneWayBind(ViewModel,
                        x => x.Messages.Filter,
                        x => x.FilterFlaggedRadio.IsChecked,
                        value => value == MessageFilter.Flagged)
                    .DisposeWith(disposables);
                this.OneWayBind(ViewModel,
                        x => x.Messages.Filter,
                        x => x.FilterUnreadRadio.IsChecked,
                        value => value == MessageFilter.Unread)
                    .DisposeWith(disposables);

                FilterAllRadio.Events().Click
                    .WithLatestFrom(viewModel, (_, vm) => vm)
                    .Subscribe(vm => vm.Messages.Filter = MessageFilter.None)
                    .DisposeWith(disposables);
                FilterFlaggedRadio.Events().Click
                    .WithLatestFrom(viewModel, (_, vm) => vm)
                    .Subscribe(vm => vm.Messages.Filter = MessageFilter.Flagged)
                    .DisposeWith(disposables);
                FilterUnreadRadio.Events().Click
                    .WithLatestFrom(viewModel, (_, vm) => vm)
                    .Subscribe(vm => vm.Messages.Filter = MessageFilter.Unread)
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel,
                        x => x.Messages.Order,
                        x => x.SortDateRadio.IsChecked,
                        value => value == MessageOrder.ReceivedDateTime)
                    .DisposeWith(disposables);
                this.OneWayBind(ViewModel,
                        x => x.Messages.Order,
                        x => x.SortNameRadio.IsChecked,
                        value => value == MessageOrder.Sender)
                    .DisposeWith(disposables);

                SortDateRadio.Events().Click
                    .WithLatestFrom(viewModel, (_, vm) => vm)
                    .Subscribe(vm => vm.Messages.Order = MessageOrder.ReceivedDateTime)
                    .DisposeWith(disposables);
                SortNameRadio.Events().Click
                    .WithLatestFrom(viewModel, (_, vm) => vm)
                    .Subscribe(vm => vm.Messages.Order = MessageOrder.Sender)
                    .DisposeWith(disposables);
#endif
            });
        }
    }
}
