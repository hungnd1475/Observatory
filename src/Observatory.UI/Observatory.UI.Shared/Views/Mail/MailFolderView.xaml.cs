using Observatory.Core.ViewModels.Mail;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Observatory.UI.Views.Mail
{
    public sealed partial class MailFolderView : UserControl
    {
        public static readonly DependencyProperty FolderProperty =
            DependencyProperty.Register(nameof(Folder), typeof(MailFolderViewModel), typeof(MailFolderView), new PropertyMetadata(null));

        public MailFolderViewModel Folder
        {
            get { return (MailFolderViewModel)GetValue(FolderProperty); }
            set { SetValue(FolderProperty, value); }
        }

        public MailFolderView()
        {
            this.InitializeComponent();
        }
    }
}
