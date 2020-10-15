using Observatory.Core.DesignTime;
using Observatory.Core.ViewModels.Mail;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
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

namespace Observatory.UI.Shared.Controls
{
    public sealed partial class MessageSummaryListView : UserControl
    {
        public static readonly DependencyProperty FolderProperty =
            DependencyProperty.Register(nameof(Folder), typeof(MailFolderViewModel), typeof(MessageSummaryListView), new PropertyMetadata(null));

        public MailFolderViewModel Folder
        {
            get { return (MailFolderViewModel)GetValue(FolderProperty); }
            set { SetValue(FolderProperty, value); }
        }

        public MessageSummaryListView()
        {
            this.InitializeComponent();
            if (DesignMode.DesignModeEnabled)
            {
                Folder = new DesignTimeMailFolderViewModel();
            }
        }
    }
}
