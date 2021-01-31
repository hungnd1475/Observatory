using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Observatory.UI.Extensions
{
    public class FluentSystemIconSource : FontIconSource
    {
        public static DependencyProperty StyleProperty { get; } =
            DependencyProperty.Register(nameof(Style), typeof(FluentSystemIconStyle), typeof(FluentSystemIconSource), 
                new PropertyMetadata(FluentSystemIconStyle.Regular, OnPropertyChanged));

        public static DependencyProperty SymbolProperty { get; } =
            DependencyProperty.Register(nameof(Symbol), typeof(FluentSystemIconSymbol), typeof(FluentSystemIconSource),
                new PropertyMetadata(FluentSystemIconSymbol.Add, OnPropertyChanged));

        private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((FluentSystemIconSource)sender).Update();
        }

        public FluentSystemIconStyle Style
        {
            get { return (FluentSystemIconStyle)GetValue(StyleProperty); }
            set { SetValue(StyleProperty, value); }
        }

        public FluentSystemIconSymbol Symbol
        {
            get { return (FluentSystemIconSymbol)GetValue(SymbolProperty); }
            set { SetValue(SymbolProperty, value); }
        }

        public FluentSystemIconSource()
        {
            Update();
        }

        public void Update()
        {
            if (Style == FluentSystemIconStyle.Regular)
            {
                FontFamily = FluentSystemIconFontFamily.Regular;
                Glyph = Symbol.ToGlyphRegular();
            }
            else
            {
                FontFamily = FluentSystemIconFontFamily.Filled;
                Glyph = Symbol.ToGlyphFilled();
            }
        }
    }
}
