using Microsoft.Graph;
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
                this.WhenAnyValue(x => x.ViewModel.Body)
                    .Subscribe(body => BodyViewer.NavigateToString(body))
                    .DisposeWith(disposables);
            });
        }
    }
}
