using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TaskStoreServerEntities
{
    public class FieldType
    {
        public int FieldTypeID { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string DisplayType { get; set; }
    }
}