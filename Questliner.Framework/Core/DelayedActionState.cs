using System.Collections.Generic;
using TaleWorlds.SaveSystem;

namespace Questliner.Framework.Core
{
    public class DelayedActionState
    {
        [SaveableProperty(1)]
        public float TriggerTimeInDays { get; set; }
        
        [SaveableProperty(2)]
        public string SerializedActionXml { get; set; } = string.Empty;
    }
}
