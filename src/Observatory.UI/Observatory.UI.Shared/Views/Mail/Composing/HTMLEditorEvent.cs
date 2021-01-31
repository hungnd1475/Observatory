using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Observatory.UI.Views.Mail.Composing
{
    public class HTMLEditorEvent
    {
        public string Type { get; set; }
        public JsonElement Payload { get; set; }
    }
}
