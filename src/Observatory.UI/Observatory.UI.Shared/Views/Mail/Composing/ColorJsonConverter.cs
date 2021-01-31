using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Windows.UI;

namespace Observatory.UI.Views.Mail.Composing
{
    public class ColorJsonConverter : JsonConverter<Color?>
    {
        private static Regex RgbaPattern { get; } = new Regex("rgba?\\((?<r>\\d+), (?<g>\\d+), (?<b>\\d+)(, (?<a>\\d+))?\\)", RegexOptions.Compiled);

        public override Color? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var rawValue = reader.GetString();
            if (rawValue == null) return null;

            var rgbMatch = RgbaPattern.Match(rawValue);
            if (rgbMatch.Success)
            {
                return new Color()
                {
                    R = byte.Parse(rgbMatch.Groups["r"].Value),
                    G = byte.Parse(rgbMatch.Groups["g"].Value),
                    B = byte.Parse(rgbMatch.Groups["b"].Value),
                    A = rgbMatch.Groups.Count == 4 ? byte.Parse(rgbMatch.Groups['a'].Value) : (byte)255,
                };
            }
            else
            {
                return rawValue switch
                {
                    _ => Colors.Transparent,
                };
            }
        }

        public override void Write(Utf8JsonWriter writer, Color? nullableValue, JsonSerializerOptions options)
        {
            if (!nullableValue.HasValue)
            {
                writer.WriteNullValue();
                return;
            }

            var value = nullableValue.Value;
            if (value == Colors.Transparent)
            {
                writer.WriteStringValue("transparent");
            }
            else if (value.A == 1)
            {
                var rawValue = $"rgb({value.R}, {value.G}, {value.B})";
                writer.WriteStringValue(rawValue);
            }
            else
            {
                var rawValue = $"rgb({value.R}, {value.G}, {value.B}, {value.A})";
                writer.WriteStringValue(rawValue);
            }
        }
    }
}
