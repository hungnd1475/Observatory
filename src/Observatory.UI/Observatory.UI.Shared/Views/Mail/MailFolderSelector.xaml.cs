using Observatory.Core.Interactivity;
using Observatory.Core.ViewModels.Mail;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Uno.Extensions;
using Uno.Logging;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using MUXC = Microsoft.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Observatory.UI.Views.Mail
{
    public sealed partial class MailFolderSelector : ContentDialog, IViewFor<MailFolderSelectionViewModel>
    {
        public static DependencyProperty ViewModelProperty { get; } =
            DependencyProperty.Register(nameof(ViewModel), typeof(MailFolderSelectionViewModel), 
                typeof(MailFolderSelector), new PropertyMetadata(null));

        public static DependencyProperty DestinationFolderProperty { get; } =
            DependencyProperty.Register(nameof(DestinationFolder), typeof(MailFolderSelectionItem), 
                typeof(MailFolderSelector),  new PropertyMetadata(null));

        public MailFolderSelectionItem DestinationFolder
        {
            get { return (MailFolderSelectionItem)GetValue(DestinationFolderProperty); }
            set { SetValue(DestinationFolderProperty, value); }
        }

        public MailFolderSelectionViewModel ViewModel 
        {
            get => (MailFolderSelectionViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel 
        {
            get => ViewModel;
            set => ViewModel = (MailFolderSelectionViewModel)value;
        }

        public MailFolderSelectionResult Result { get; private set; }

        public MailFolderSelector()
        {
            InitializeComponent();
            this.WhenActivated(disposables => 
            {
                Observable.CombineLatest(
                        this.WhenAnyValue(x => x.ViewModel.CanMoveTo),
                        this.WhenAnyValue(x => x.DestinationFolder),
                        (canMoveTo, destinationFolder) => canMoveTo(destinationFolder))
                   .BindTo(this, x => x.IsPrimaryButtonEnabled)
                   .DisposeWith(disposables);

                Disposable.Create(() =>
                {
                    DestinationFolder = null;
                    ViewModel = null;
                })
                .DisposeWith(disposables);
            });
        }

        public void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            Result = new MailFolderSelectionResult(false, DestinationFolder);
        }

        public void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            Result = new MailFolderSelectionResult(true, null);
        }

        public void SelectFolder(MUXC.TreeView sender, MUXC.TreeViewItemInvokedEventArgs e)
        {
            DestinationFolder = (MailFolderSelectionItem)e.InvokedItem;
        }
    }
}
