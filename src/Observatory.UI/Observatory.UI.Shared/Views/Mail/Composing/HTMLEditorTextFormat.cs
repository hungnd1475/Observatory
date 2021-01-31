using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.UI;

namespace Observatory.UI.Views.Mail.Composing
{
    /// <summary>
    /// Represents a class that encapsulates all formatting information of the <see cref="HTMLEditor"/>.
    /// </summary>
    public class HTMLEditorTextFormat
    {
        private static JsonSerializerOptions JsonSerializerOptions { get; } = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            IgnoreNullValues = true,
        };

        /// <summary>
        /// Gets the default value of <see cref="HTMLEditorTextFormat"/>.
        /// </summary>
        public static HTMLEditorTextFormat Default { get; } = new HTMLEditorTextFormat()
        {
            FontNames = new string[] { "serif" },
            FontSize = 11,
            IsBold = false,
            IsItalic = false,
            IsUnderlined = false,
            IsStrikethrough = false,
            IsSubscript = false,
            IsSuperscript = false,
            Background = Colors.Transparent,
            Foreground = Colors.Black,
            Alignment = HTMLEditorTextAlignment.Left,
        };

        /// <summary>
        /// Gets the font names of the text.
        /// </summary>
        public string[] FontNames { get; set; }
        /// <summary>
        /// Gets the font size of the text.
        /// </summary>
        public int FontSize { get; set; }

        /// <summary>
        /// Gets the value indicating whether the text is bold.
        /// </summary>
        public bool IsBold { get; set; }
        /// <summary>
        /// Gets the value indicating whether the text is italic.
        /// </summary>
        public bool IsItalic { get; set; }
        /// <summary>
        /// Gets the value indicating whether the text is underlined.
        /// </summary>
        public bool IsUnderlined { get; set; }
        /// <summary>
        /// Gets the value indicating whether the text is strikethrough.
        /// </summary>
        public bool IsStrikethrough { get; set; }
        /// <summary>
        /// Gets the value indicating whether is text is superscript.
        /// </summary>
        public bool IsSuperscript { get; set; }
        /// <summary>
        /// Gets the value indicating whether the text is subscript.
        /// </summary>
        public bool IsSubscript { get; set; }

        /// <summary>
        /// Gets the background color of the text.
        /// </summary>
        [JsonConverter(typeof(ColorJsonConverter))]
        public Color? Background { get; set; }
        /// <summary>
        /// Gets the foreground color of the text.
        /// </summary>
        [JsonConverter(typeof(ColorJsonConverter))]
        public Color? Foreground { get; set; }

        /// <summary>
        /// Gets the alignment of the text.
        /// </summary>
        public HTMLEditorTextAlignment Alignment { get; set; }

        /// <summary>
        /// Constructs an instance of <see cref="HTMLEditorTextFormat"/> by deserializing the given text. 
        /// </summary>
        /// <param name="text">The text to be deserialized.</param>
        /// <returns>An instance of <see cref="HTMLEditorTextFormat"/>.</returns>
        public static HTMLEditorTextFormat Deserialize(string text)
        {
            return JsonSerializer.Deserialize<HTMLEditorTextFormat>(text, JsonSerializerOptions);
        }
    }
}
