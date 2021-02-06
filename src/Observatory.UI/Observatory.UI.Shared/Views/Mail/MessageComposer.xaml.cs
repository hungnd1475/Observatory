using Microsoft.Toolkit.Uwp.UI.Controls;
using Observatory.Core.ViewModels.Mail;
using ReactiveUI;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using EmailValidation;
using Observatory.Core.Models;
using System.Threading.Tasks;
using Windows.UI.Core;
using Observatory.UI.Views.Mail.Composing;
using Windows.UI.Xaml.Media;
using System.Collections.Generic;
using Uno.Extensions;
using Uno.Logging;
using Windows.UI.ViewManagement.Core;
using Windows.UI.Xaml.Input;
using Observatory.UI.Extensions;
using Observatory.UI.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Observatory.UI.Views.Mail
{
    public sealed partial class MessageComposer : UserControl, IViewFor<MessageDetailViewModel>
    {
        public static DependencyProperty ViewModelProperty { get; } =
            DependencyProperty.Register(nameof(ViewModel), typeof(MessageDetailViewModel), typeof(MessageComposer), new PropertyMetadata(null));

        public static DependencyProperty IsCcVisibleProperty { get; } =
            DependencyProperty.Register(nameof(IsCcVisible), typeof(bool), typeof(MessageComposer), new PropertyMetadata(false));

        public static DependencyProperty IsBccVisibleProperty { get; } =
            DependencyProperty.Register(nameof(IsBccVisible), typeof(bool), typeof(MessageComposer), new PropertyMetadata(false));

        public static DependencyProperty IsFormattingEnabledProperty { get; } =
            DependencyProperty.Register(nameof(IsFormattingEnabled), typeof(bool), typeof(MessageComposer), new PropertyMetadata(false));

        public static DependencyProperty FormattedFontFamilyProperty { get; } =
            DependencyProperty.Register(nameof(FormattedFontFamily), typeof(string), typeof(MessageComposer), new PropertyMetadata(null));

        public static DependencyProperty DisplayTextFormatProperty { get; } =
            DependencyProperty.Register(nameof(DisplayTextFormat), typeof(HTMLEditorTextFormat), typeof(MessageComposer),
                new PropertyMetadata(HTMLEditorTextFormat.Default));

        public static IEnumerable<string> FontFamilies { get; } = App.GetSystemFonts();

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

        public bool IsCcVisible
        {
            get { return (bool)GetValue(IsCcVisibleProperty); }
            set { SetValue(IsCcVisibleProperty, value); }
        }

        public bool IsBccVisible
        {
            get { return (bool)GetValue(IsBccVisibleProperty); }
            set { SetValue(IsBccVisibleProperty, value); }
        }

        public bool IsFormattingEnabled
        {
            get { return (bool)GetValue(IsFormattingEnabledProperty); }
            set { SetValue(IsFormattingEnabledProperty, value); }
        }

        public string FormattedFontFamily
        {
            get { return (string)GetValue(FormattedFontFamilyProperty); }
            set { SetValue(FormattedFontFamilyProperty, value); }
        }

        public HTMLEditorTextFormat DisplayTextFormat
        {
            get { return (HTMLEditorTextFormat)GetValue(DisplayTextFormatProperty); }
            set { SetValue(DisplayTextFormatProperty, value); }
        }

        private bool _isFontChangedFromEditor = false;
        private bool _isFontChangedFromUser = false;

        public MessageComposer()
        {
            this.InitializeComponent();
            EditorToolBarShadow.Receivers.Add(MailHeaderGrid);            
            TableSizeSelectionGrid.SizeSelected += TableSizeSelectionGrid_SizeSelected;
            EditorToolBarTableTab.Loaded += EditorToolBarTableTab_Loaded;

            this.WhenActivated(disposables =>
            {
                this.WhenAnyValue(
                        x => x.ViewModel.Body,
                        x => x.ViewModel.IsDraft,
                        (body, isDraft) => (Body: body, IsDraft: isDraft))
                    .Where(x => x.IsDraft)
                    .Do(x => Editor.StartEditing(x.Body))
                    .Subscribe()
                    .DisposeWith(disposables);

                this.WhenAnyValue(x => x.ViewModel)
                    .Where(x => x != null)
                    .DistinctUntilChanged()
                    .Do(x =>
                    {
                        IsCcVisible = x.CcRecipients.Count > 0;
                        IsBccVisible = false;
                    })
                    .Subscribe()
                    .DisposeWith(disposables);

                this.WhenAnyValue(x => x.Editor.TextFormat)
                    .Do(x => DisplayTextFormat = x)
                    .Subscribe()
                    .DisposeWith(disposables);

                this.WhenAnyValue(x => x.DisplayTextFormat.FontNames)
                    .Select(x => FindMatchingFontFamily(x))
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Do(x =>
                    {
                        if (_isFontChangedFromUser)
                        {
                            _isFontChangedFromUser = false;
                            Editor.Focus(FocusState.Programmatic);
                            return;
                        }

                        if (FormattedFontFamily != x)
                        {
                            _isFontChangedFromEditor = true;
                            FormattedFontFamily = x;
                        }
                    })
                    .Subscribe()
                    .DisposeWith(disposables);

                this.WhenAnyValue(x => x.DisplayTextFormat.IsTable)
                    .DistinctUntilChanged()
                    .Do(isTable =>
                    {
                        if (isTable)
                        {
                            EditorToolBar.Items.Add(EditorToolBarTableTab);
                        }
                        else
                        {
                            EditorToolBar.Items.Remove(EditorToolBarTableTab);
                        }
                    })
                    .Subscribe()
                    .DisposeWith(disposables);


                Disposable.Create(() =>
                {
                    _isFontChangedFromEditor = false;
                    _isFontChangedFromUser = false;
                    IsFormattingEnabled = false;
                })
                .DisposeWith(disposables);
            });
        }

        private void EditorToolBarTableTab_Loaded(object sender, RoutedEventArgs e)
        {
            EditorToolBar.SelectedItem = EditorToolBarTableTab;
        }

        private void TableSizeSelectionGrid_SizeSelected(object sender, TableSizeSelectionEventArgs e)
        {
            TableSizeSelectionFlyout.Hide();
            Editor.InsertTable(e.RowCount, e.ColumnCount);
        }

        public string FindMatchingFontFamily(IEnumerable<string> fontFamiliesToFind)
        {
            return fontFamiliesToFind.FirstOrDefault(x => FontFamilies.Contains(x));
        }

        public void ShowCcTextBox()
        {
            IsCcVisible = true;
            Task.Run(async () => await Dispatcher.RunAsync(
                CoreDispatcherPriority.Low, 
                () => CcTextBox.Focus(FocusState.Programmatic)));
        }

        public void ShowBccTextBox()
        {
            IsBccVisible = true;
            BccTextBox.Focus(FocusState.Programmatic);
        }

        public void RecipientTokenItemAdding(TokenizingTextBox sender, TokenItemAddingEventArgs e)
        {            
            if (EmailValidator.Validate(e.TokenText, allowInternational: true))
            {
                e.Item = new Recipient()
                {
                    DisplayName = e.TokenText,
                    EmailAddress = e.TokenText,
                };
            }
            else
            {
                e.Cancel = true;
            }
        }

        public FluentSystemIconSymbol ConvertAlignmentToSymbol(HTMLEditorTextAlignment alignment)
        {
            return alignment switch
            {
                HTMLEditorTextAlignment.Left => FluentSystemIconSymbol.AlignLeft,
                HTMLEditorTextAlignment.Center => FluentSystemIconSymbol.AlignCenter,
                HTMLEditorTextAlignment.Right => FluentSystemIconSymbol.AlignRight,
                HTMLEditorTextAlignment.Justified => FluentSystemIconSymbol.AlignJustify,
                _ => throw new NotSupportedException(),
            };
        }

        public FontFamily ConvertStringToFontFamily(string fontName)
        {
            return new FontFamily(fontName);
        }

        public void FontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isFontChangedFromEditor)
            {
                _isFontChangedFromEditor = false;
                return;
            }

            if (e.AddedItems.Count > 0)
            {
                _isFontChangedFromUser = true;
                Editor.SetFont((string)e.AddedItems[0]);
            }
        }

        public void FontComboBox_GettingFocus(UIElement sender, GettingFocusEventArgs e)
        {
            e.Handled = true;
        }

        public void FontComboBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                Editor.Focus(FocusState.Programmatic);
            }
        }

        public void ShowEmojiKeyboard()
        {
            CoreInputView.GetForCurrentView().TryShow(CoreInputViewKind.Emoji);
        }

        public void MailHeaderEditor_GotFocus(object sender, RoutedEventArgs e)
        {
            IsFormattingEnabled = false;
            DisplayTextFormat = HTMLEditorTextFormat.Default;
        }

        public void MailBodyEditor_GotFocus(object sender, RoutedEventArgs e)
        {
            IsFormattingEnabled = true;
            DisplayTextFormat = Editor.TextFormat;
        }

        public void EditorToolBar_GettingFocus(UIElement sender, GettingFocusEventArgs e)
        {
            if (e.OldFocusedElement != null)
            {
                e.TrySetNewFocusedElement(e.OldFocusedElement);
            }
        }
    }
}
