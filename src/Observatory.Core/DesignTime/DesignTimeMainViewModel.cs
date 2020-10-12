using Observatory.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace Observatory.Core.DesignTime
{
    public class DesignTimeMainViewModel
    {
        public IEnumerable<FunctionalityMode> Modes { get; } = new FunctionalityMode[]
        {
            FunctionalityMode.Mail,
            FunctionalityMode.Calendar,
        };
        public FunctionalityMode SelectedMode { get; set; }
    }
}
