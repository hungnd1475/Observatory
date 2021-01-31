using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.UI
{
    public partial class App
    {
        public static IEnumerable<string> GetSystemFonts()
        {
            return Microsoft.Graphics.Canvas.Text.CanvasTextFormat
                .GetSystemFontFamilies()
                .OrderBy(x => x);
        }
    }
}
