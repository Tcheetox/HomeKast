using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Cast.Provider.Converter
{
    public class QueueState
    {
        // TODO: serialize media don't give a shit thanks
        public bool IsConverting { get; init; }
        public IMedia Media { get; init; }
        public string MediaName => Media?.Name ?? string.Empty;
        public Guid MediaId => Media?.Id ?? Guid.Empty;
        public int MediaProgress { get; init; }
        public int QueueLength { get; init; }
    }
}
