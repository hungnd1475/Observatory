using Microsoft.Graph;
using Observatory.Core.Models;
using Observatory.Core.ViewModels.Mail;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public sealed partial class MessageView : UserControl
    {
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(MessageDetailViewModel), typeof(MessageView), new PropertyMetadata(null));

        public MessageDetailViewModel Message
        {
            get { return (MessageDetailViewModel)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        public MessageView()
        {
            this.InitializeComponent();
            this.WhenAnyValue(x => x.Message)
                .Where(m => m != null)
                .DistinctUntilChanged()
                .SelectMany(m => m.WhenAnyValue(x => x.Body))
                .Subscribe(body =>
                {
                    BodyViewer.NavigateToString(body);
                });
        }
    }
}
