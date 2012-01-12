using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class ItemList
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public Guid DefaultItemTypeID { get; set; }
        public bool Template { get; set; }
        public Guid UserID { get; set; }
        public List<Item> Items { get; set; }
    }
}