using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.UI.Extensions
{
    public static class FluentSystemIconSymbolExtensions
    {
        public static string ToGlyphRegular(this FluentSystemIconSymbol symbol)
        {
            return symbol switch
            {
                FluentSystemIconSymbol.Add => "\uF109",
                FluentSystemIconSymbol.AlignCenter => "\uF799",
                FluentSystemIconSymbol.AlignJustify => "\uF79D",
                FluentSystemIconSymbol.AlignLeft => "\uF79F",
                FluentSystemIconSymbol.AlignRight => "\uF7A1",
                FluentSystemIconSymbol.Archive => "\uF139",
                FluentSystemIconSymbol.ArrowDown => "\uF148",
                FluentSystemIconSymbol.ArrowForward => "\uF157",
                FluentSystemIconSymbol.Attachment => "\uF1A9",
                FluentSystemIconSymbol.Bold => "\uF7A4",
                FluentSystemIconSymbol.Bullets => "\uF7A6",
                FluentSystemIconSymbol.Calendar => "\uF20B",
                FluentSystemIconSymbol.ChevronDown => "\uF2A3",
                FluentSystemIconSymbol.ChevronRight => "\uF2B0",
                FluentSystemIconSymbol.Delete => "\uF34C",
                FluentSystemIconSymbol.Emoji => "\uF3E0",
                FluentSystemIconSymbol.Flag => "\uF40B",
                FluentSystemIconSymbol.FlagOff => "\uF410",
                FluentSystemIconSymbol.Folder => "\uF418",
                FluentSystemIconSymbol.FolderArrowRight => "\uFC0A",
                FluentSystemIconSymbol.FolderProhibited => "\uFC0F",
                FluentSystemIconSymbol.FontDecreased => "\uF437",
                FluentSystemIconSymbol.FontIncreased => "\uF439",
                FluentSystemIconSymbol.Highlight => "\uF47C",
                FluentSystemIconSymbol.Importance => "\uF49F",
                FluentSystemIconSymbol.IndentDecreased => "\uFAD1",
                FluentSystemIconSymbol.IndentIncreased => "\uFAD2",
                FluentSystemIconSymbol.Italic => "\uF7F4",
                FluentSystemIconSymbol.Link => "\uF4E4",
                FluentSystemIconSymbol.Mail => "\uF506",
                FluentSystemIconSymbol.MailRead => "\uF521",
                FluentSystemIconSymbol.Numbering => "\uF7F9",
                FluentSystemIconSymbol.Person => "\uF5BD",
                FluentSystemIconSymbol.PersonAdd => "\uF5C2",
                FluentSystemIconSymbol.Picture => "\uF488",
                FluentSystemIconSymbol.Print => "\uF62A",
                FluentSystemIconSymbol.Rename => "\uF669",
                FluentSystemIconSymbol.Reply => "\uF177",
                FluentSystemIconSymbol.ReplyAll => "\uF17B",
                FluentSystemIconSymbol.SaveAs => "\uFC51",
                FluentSystemIconSymbol.Search => "\uF68F",
                FluentSystemIconSymbol.Setting => "\uF6A9",
                FluentSystemIconSymbol.Star => "\uF70F",
                FluentSystemIconSymbol.Strikethrough => "\uF804",
                FluentSystemIconSymbol.Subscript => "\uF806",
                FluentSystemIconSymbol.Superscript => "\uF808",
                FluentSystemIconSymbol.Sync => "\uF190",
                FluentSystemIconSymbol.Table => "\uF75D",
                FluentSystemIconSymbol.TextColor => "\uF7BF",
                FluentSystemIconSymbol.Underline => "\uF80A",
                FluentSystemIconSymbol.ZoomIn => "\uF8C4",
                FluentSystemIconSymbol.ZoomOut => "\uF8C6",
                FluentSystemIconSymbol.Navigation => "\uF560",
                FluentSystemIconSymbol.CheckList => "\uF786",
                FluentSystemIconSymbol.CheckMark => "\uF294",
                FluentSystemIconSymbol.Filter => "\uF406",
                FluentSystemIconSymbol.MultiSelect => "\uF55B",
                FluentSystemIconSymbol.Send => "\uF699",
                _ => throw new NotSupportedException(),
            };
        }

        public static string ToGlyphFilled(this FluentSystemIconSymbol symbol)
        {
            return symbol switch
            {
                FluentSystemIconSymbol.Add => "\uF109",
                FluentSystemIconSymbol.CheckMark => "\uF293",
                _ => throw new NotSupportedException(),
            };
        }
    }
}
