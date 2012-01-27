using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class User
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime CreateDate { get; set; }

        // do not serialize password information (hash & salt)
        [IgnoreDataMember]
        public string Password { get; set; }
        [IgnoreDataMember]
        public string PasswordSalt { get; set; }

        public List<ItemType> ItemTypes { get; set; }  
        public List<Tag> Tags { get; set; }
        public List<Item> Items { get; set; }
        public List<Folder> Folders { get; set; }
    }
}