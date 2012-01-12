using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class ItemTag
    {
        public Guid ID { get; set; }
        public Guid ItemID { get; set; }
        public Guid TagID { get; set; }
    }
}