using System;

namespace BuiltSteady.Zaplify.ServiceUtilities.Bing
{
    [Flags]
    public enum SearchOption
    {
        None = 0,
        DisableLocationDetection = 0x1,
        EnableHighlighting = 0x2
    }
}
