using Tizen.Applications;
using Uno.UI.Runtime.Skia;

namespace Observatory.UI.Skia.Tizen
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = new TizenHost(() => new Observatory.UI.App(), args);
            host.Run();
        }
    }
}
