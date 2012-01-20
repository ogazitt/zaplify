using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class Field
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public int FieldTypeID { get; set; }
        public Guid ItemTypeID { get; set; }
        public string DisplayName { get; set; }
        public string DisplayType { get; set; }
        public bool IsPrimary { get; set; }
        public int SortOrder { get; set; }
    }
}