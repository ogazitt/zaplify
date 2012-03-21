using System;

namespace BuiltSteady.Zaplify.ServiceUtilities.Bing
{
    [Flags]
    public enum SourceType
    {
        // TODO: If any of these are uncomment, a class needs to be
        //       created that derives from SearchResult, and logic
        //       needs to be implemented in the query processing to
        //       support de-serializing the objects from the result.

        //Image = 0x1,
        //News = 0x2,
        //Phonebook = 0x4,
        //RelatedSearch = 0x8,
        //Spell = 0x10,
        //Translation = 0x20,
        //Video = 0x40,
        Web = 0x80,
    }
}
