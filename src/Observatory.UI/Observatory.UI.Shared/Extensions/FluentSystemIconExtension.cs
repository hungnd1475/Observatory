using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace Observatory.UI.Extensions
{
    [MarkupExtensionReturnType(ReturnType = typeof(IconSourceElement))]
    public class FluentSystemIconExtension : MarkupExtension
    {
        public FluentSystemIconStyle Style { get; set; } = FluentSystemIconStyle.Regular;

        public FluentSystemIconSymbol Symbol { get; set; } = FluentSystemIconSymbol.Add;

        public Thickness Margin { get; set; } = new Thickness(-2);

        public double Size { get; set; } = 16;

        protected override object ProvideValue()
        {
            return new IconSourceElement()
            {
                Margin = Margin,
                IconSource = new FluentSystemIconSource()
                {
                    Style = Style,
                    Symbol = Symbol,
                    FontSize = Size,
                },
            };
        }
    }
}
