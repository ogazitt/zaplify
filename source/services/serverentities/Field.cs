using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TaskStoreServerEntities
{
    public class Field
    {
        public Guid ID { get; set; }
        public int FieldTypeID { get; set; }
        public Guid ListTypeID { get; set; }
        public bool IsPrimary { get; set; }
        public int SortOrder { get; set; }
    }
}