using Observatory.Core.Models;
using Observatory.Core.ViewModels.Mail;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Uno;
using Uno.Extensions;
using Uno.Logging;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Observatory.UI.Views.Mail
{
    public sealed partial class MessageDetailView : UserControl, IViewFor<MessageDetailViewModel>
    {
        public static DependencyProperty ViewModelProperty { get; } =
            DependencyProperty.Register(nameof(ViewModel), typeof(MessageDetailViewModel), typeof(MessageDetailView), new PropertyMetadata(null));

        public MessageDetailViewModel ViewModel
        {
            get { return (MessageDetailViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        object IViewFor.ViewModel 
        {
            get => ViewModel;
            set => ViewModel = (MessageDetailViewModel)value;
        }

        public MessageDetailView()
        {
            this.InitializeComponent();
            this.WhenActivated(disposables =>
            {
                this.WhenAnyValue(
                        x => x.ViewModel.Body,
                        x => x.ViewModel.IsDraft,
                        (body, isDraft) => (Body: body, IsDraft: isDraft))
                    .Where(x => !x.IsDraft)
                    .Do(x => BodyViewer.NavigateToString(x.Body))
                    .Subscribe()
                    .DisposeWith(disposables);
            });
        }

        public async void BodyViewer_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs e)
        {            
            if (e.Uri != null)
            {
                e.Cancel = true;
                await Launcher.LaunchUriAsync(e.Uri);
            }
        }

        public string FormatRecipient(Recipient recipient, bool isFull)
        {
            if (string.IsNullOrEmpty(recipient.DisplayName))
            {
                return recipient.EmailAddress;
            }
            else if (isFull)
            {
                return $"{recipient.DisplayName} <{recipient.EmailAddress}>";
            }
            else
            {
                return recipient.DisplayName;
            }
        }

        public IReadOnlyList<string> FormatRecipientList(IReadOnlyList<Recipient> recipients)
        {
            return recipients.Select((r, i) =>
            {
                return i == recipients.Count - 1
                    ? FormatRecipient(r, false)
                    : FormatRecipient(r, false) + ";";
            })
            .ToList().AsReadOnly();
        }

        public string FormatReceivedDateTime(DateTimeOffset receivedDateTime)
        {
            var now = DateTimeOffset.Now;
            if (now.Date == receivedDateTime.Date)
            {
                return receivedDateTime.ToString("hh:mm tt");
            }

            return receivedDateTime.ToString("g");
        }
    }
}
