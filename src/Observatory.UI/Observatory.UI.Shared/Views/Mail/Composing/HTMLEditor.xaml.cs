using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Observatory.UI.Views.Mail.Composing
{
    public sealed partial class HTMLEditor : UserControl
    {
        private static string EditorScript { get; set; }

        public static DependencyProperty TextFormatProperty { get; } =
            DependencyProperty.Register(nameof(TextFormat), typeof(HTMLEditorTextFormat),
                typeof(HTMLEditor), new PropertyMetadata(HTMLEditorTextFormat.Default));

        public static DependencyProperty CanIncreaseFontSizeProperty { get; } =
            DependencyProperty.Register(nameof(CanIncreaseFontSize), typeof(bool), typeof(HTMLEditor), new PropertyMetadata(true));

        public static DependencyProperty CanDecreaseFontSizeProperty { get; } =
            DependencyProperty.Register(nameof(CanDecreaseFontSize), typeof(bool), typeof(HTMLEditor), new PropertyMetadata(true));

        /// <summary>
        /// Gets the format of the current text selection.
        /// </summary>
        public HTMLEditorTextFormat TextFormat
        {
            get { return (HTMLEditorTextFormat)GetValue(TextFormatProperty); }
            private set { SetValue(TextFormatProperty, value); }
        }

        /// <summary>
        /// Gets the value indicating whether font size can be increased.
        /// </summary>
        public bool CanIncreaseFontSize
        {
            get { return (bool)GetValue(CanIncreaseFontSizeProperty); }
            private set { SetValue(CanIncreaseFontSizeProperty, value); }
        }

        /// <summary>
        /// Gets the value indicating whether the font size can be decrease.
        /// </summary>
        public bool CanDecreaseFontSize
        {
            get { return (bool)GetValue(CanDecreaseFontSizeProperty); }
            private set { SetValue(CanDecreaseFontSizeProperty, value); }
        }

        public HTMLEditor()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Populates the editor with the given HTML text.
        /// </summary>
        /// <param name="htmlText">The HTML text to be edited.</param>
        public void StartEditing(string htmlText)
        {
            EditorWebView.NavigateToString(htmlText);
        }

        /// <summary>
        /// Toggles bold format for the current text selection.
        /// </summary>
        public async void ToggleBold() => await SetCurrentFormat(ScriptConstants.FORMAT_BOLD);

        /// <summary>
        /// Toggles italic format for the current text selection.
        /// </summary>
        public async void ToggleItalic() => await SetCurrentFormat(ScriptConstants.FORMAT_ITALIC);

        /// <summary>
        /// Toggles underlined format for the current text selection.
        /// </summary>
        public async void ToggleUnderlined() => await SetCurrentFormat(ScriptConstants.FORMAT_UNDERLINED);

        /// <summary>
        /// Toggles strikethrough format for the current text selection.
        /// </summary>
        public async void ToggleStrikethrough() => await SetCurrentFormat(ScriptConstants.FORMAT_STRIKETHROUGH);

        /// <summary>
        /// Toggles superscript format for the current text selection.
        /// </summary>
        public async void ToggleSuperscript() => await SetCurrentFormat(ScriptConstants.FORMAT_SUPERSCRIPT);

        /// <summary>
        /// Toggles subscript format for the current text selection.
        /// </summary>
        public async void ToggleSubscript() => await SetCurrentFormat(ScriptConstants.FORMAT_SUBSCRIPT);

        /// <summary>
        /// Aligns the current paragraph based on a given <paramref name="alignment"/>.
        /// </summary>
        /// <param name="alignment">The alignment.</param>
        public async void Align(HTMLEditorTextAlignment alignment)
        {
            var formatName = alignment switch
            {
                HTMLEditorTextAlignment.Left => ScriptConstants.FORMAT_ALIGN_LEFT,
                HTMLEditorTextAlignment.Center => ScriptConstants.FORMAT_ALIGN_CENTER,
                HTMLEditorTextAlignment.Right => ScriptConstants.FORMAT_ALIGN_RIGHT,
                HTMLEditorTextAlignment.Justified => ScriptConstants.FORMAT_ALIGN_JUSTIFIED,
                _ => throw new NotSupportedException(),
            };
            await SetCurrentFormat(formatName);
        }

        /// <summary>
        /// Aligns the current paragraph to the left.
        /// </summary>
        public void AlignLeft() => Align(HTMLEditorTextAlignment.Left);

        /// <summary>
        /// Centers the current paragraph.
        /// </summary>
        public void AlignCenter() => Align(HTMLEditorTextAlignment.Center);

        /// <summary>
        /// Aligns the current paragraph to the right.
        /// </summary>
        public void AlignRight() => Align(HTMLEditorTextAlignment.Right);

        /// <summary>
        /// Justifies the current paragraph.
        /// </summary>
        public void AlignJustified() => Align(HTMLEditorTextAlignment.Justified);

        /// <summary>
        /// Increases the font size of the current text selection.
        /// </summary>
        public async void IncreaseFontSize()
        {
            if (CanIncreaseFontSize)
            {
                await SetCurrentFormat(ScriptConstants.FORMAT_FONT_SIZE, $"{TextFormat.FontSize + 1}");
                CanIncreaseFontSize = TextFormat.FontSize < HTMLEditorTextFormat.MAX_FONT_SIZE;
                CanDecreaseFontSize = TextFormat.FontSize > HTMLEditorTextFormat.MIN_FONT_SIZE;
            }
        }

        /// <summary>
        /// Decreases the font size of the current text selection.
        /// </summary>
        public async void DecreaseFontSize()
        {
            if (CanDecreaseFontSize)
            {
                await SetCurrentFormat(ScriptConstants.FORMAT_FONT_SIZE, $"{TextFormat.FontSize - 1}");
                CanIncreaseFontSize = TextFormat.FontSize < HTMLEditorTextFormat.MAX_FONT_SIZE;
                CanDecreaseFontSize = TextFormat.FontSize > HTMLEditorTextFormat.MIN_FONT_SIZE;
            }
        }

        /// <summary>
        /// Sets the font of the current selection.
        /// </summary>
        /// <param name="fontName"></param>
        public async void SetFont(string fontName) => await SetCurrentFormat(
            ScriptConstants.FORMAT_FONT_NAME, fontName);

        private async void NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            if (args.Uri != null)
            {
                args.Cancel = true;
                await Launcher.LaunchUriAsync(args.Uri);
            }
        }

        private async void NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            if (args.IsSuccess)
            {
                if (EditorScript == null)
                {
                    EditorScript = await LoadScriptAsync();
                }
                await sender.InvokeScriptAsync(ScriptConstants.FUNCTION_EVAL, new string[] { EditorScript });
            }
        }

        private void ScriptNotify(object sender, NotifyEventArgs e)
        {
            TextFormat = HTMLEditorTextFormat.Deserialize(e.Value);
        }

        private static async Task<string> LoadScriptAsync()
        {
            var scriptFile = await StorageFile.GetFileFromApplicationUriAsync(ScriptConstants.SCRIPT_URI);
            using var stream = await scriptFile.OpenReadAsync();
            var reader = new StreamReader(stream.AsStreamForRead());
            return await reader.ReadToEndAsync();
        }

        private async Task SetCurrentFormat(string formatName, string value = null)
        {
            var arguments = value == null
                ? new string[] { formatName }
                : new string[] { formatName, value };
            var result = await EditorWebView.InvokeScriptAsync(
                ScriptConstants.FUNCTION_SET_CURRENT_FORMAT,
                arguments);
            TextFormat = HTMLEditorTextFormat.Deserialize(result);
        }

        private static class ScriptConstants
        {
            public static readonly Uri SCRIPT_URI = new Uri("ms-appx:////HTMLEditor.js");

            public const string FUNCTION_EVAL = "eval";
            public const string FUNCTION_SET_CURRENT_FORMAT = "setCurrentFormat";

            public const string FORMAT_BOLD = "bold";
            public const string FORMAT_ITALIC = "italic";
            public const string FORMAT_UNDERLINED = "underline";
            public const string FORMAT_SUBSCRIPT = "subscript";
            public const string FORMAT_SUPERSCRIPT = "superscript";
            public const string FORMAT_STRIKETHROUGH = "strikethrough";
            public const string FORMAT_FOREGROUND = "forecolor";
            public const string FORMAT_BACKGROUND = "backcolor";
            public const string FORMAT_FONT_NAME = "fontname";
            public const string FORMAT_FONT_SIZE = "fontsize";
            public const string FORMAT_ALIGN_LEFT = "justifyLeft";
            public const string FORMAT_ALIGN_CENTER = "justifyCenter";
            public const string FORMAT_ALIGN_RIGHT = "justifyRight";
            public const string FORMAT_ALIGN_JUSTIFIED = "justifyFull";
        }
    }
}
