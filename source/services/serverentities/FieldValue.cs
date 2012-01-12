using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class FieldValue
    {
        public Guid ID { get; set; }
        public Guid FieldID { get; set; }
        public Guid ItemID { get; set; }
        public string Value { get; set; }
    }
}