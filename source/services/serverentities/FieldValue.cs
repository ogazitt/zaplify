using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TaskStoreServerEntities
{
    public class FieldValue
    {
        public Guid FieldValueID { get; set; }
        public Guid FieldID { get; set; }
        public Guid ItemID { get; set; }
        public string Value { get; set; }
    }
}