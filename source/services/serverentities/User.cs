using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class User
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public List<ItemType> ItemTypes { get; set; }  
        public List<Tag> Tags { get; set; }
        public List<ItemList> ItemLists { get; set; }
    }
}