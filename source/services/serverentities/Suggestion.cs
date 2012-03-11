using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class Suggestion
    {
        public Guid ID { get; set; }
        public Guid ItemID { get; set; }
        public bool Retrieved { get; set; }
        public string Username { get; set; }
        public string Type { get; set; }  // "URL", "ItemRef"
        public string Name { get; set; }  // Type == "URL": anchor text; Type == "ItemRef": FieldName
        public string Value { get; set; } // Type == "URL": URL;         Type == "ItemRef": GUID
        public DateTime Created { get; set; }
    }
}