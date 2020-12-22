using Observatory.Core.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.Interactivity
{
    public static partial class Interactions
    {
        public static Interaction<IEnumerable<IProfileProvider>, IProfileProvider> ProviderSelection { get; } =
            new Interaction<IEnumerable<IProfileProvider>, IProfileProvider>();
    }
}
