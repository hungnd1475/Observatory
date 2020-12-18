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
            FolderNameShadow.Receivers.Add(MessageListGrid);

            this.WhenActivated(disposables =>
            {
                this.OneWayBind(ViewModel,
                        x => x.Messages,
                        x => x.MessageList.ItemsSource,
                        value => value?.ToNative())
                    .DisposeWith(disposables);
            });
        }
    }
}
