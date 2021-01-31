using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Observatory.UI.Extensions
{
    public class FluentSystemIcon : IconSourceElement
    {
        public static DependencyProperty IconStyleProperty { get; } =
            DependencyProperty.Register(nameof(IconStyle), typeof(FluentSystemIconStyle), typeof(FluentSystemIcon),
                new PropertyMetadata(FluentSystemIconStyle.Regular, OnIconStyleChanged));

        private static void OnIconStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((FluentSystemIcon)d)._iconSource.Style = (FluentSystemIconStyle)e.NewValue;
        }

        public static DependencyProperty SymbolProperty { get; } =
            DependencyProperty.Register(nameof(Symbol), typeof(FluentSystemIconSymbol), typeof(FluentSystemIcon),
                new PropertyMetadata(FluentSystemIconSymbol.Add, OnSymbolChanged));

        private static void OnSymbolChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((FluentSystemIcon)d)._iconSource.Symbol = (FluentSystemIconSymbol)e.NewValue;
        }

        public static DependencyProperty FontSizeProperty { get; } =
            DependencyProperty.Register(nameof(FontSize), typeof(double), typeof(FluentSystemIcon), 
                new PropertyMetadata(20, OnFontSizeChanged));

        private static void OnFontSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((FluentSystemIcon)d)._iconSource.FontSize = (double)e.NewValue;
        }

        public FluentSystemIconStyle IconStyle
        {
            get { return (FluentSystemIconStyle)GetValue(IconStyleProperty); }
            set { SetValue(IconStyleProperty, value); }
        }

        public FluentSystemIconSymbol Symbol
        {
            get { return (FluentSystemIconSymbol)GetValue(SymbolProperty); }
            set { SetValue(SymbolProperty, value); }
        }

        public double FontSize
        {
            get { return (double)GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        private readonly FluentSystemIconSource _iconSource = new FluentSystemIconSource();

        public FluentSystemIcon()
        {
            Margin = new Thickness(-2);
            IconSource = _iconSource;
        }
    }
}
