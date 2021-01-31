using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Media;

namespace Observatory.UI.Extensions
{
    public static class FluentSystemIconFontFamily
    {
        public static FontFamily Regular { get; } = new FontFamily("/Assets/FluentSystemIcons-Regular.ttf#FluentSystemIcons-Regular");
        public static FontFamily Filled { get; } = new FontFamily("/Assets/FluentSystemIcons-Filled.ttf#FluentSystemIcons-Filled");
    }
}
