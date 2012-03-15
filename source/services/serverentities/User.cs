using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace BuiltSteady.Zaplify.ServerEntities
{

    public class User : ServerEntity
    {
        // ServerEntity properties
        public override Guid ID { get; set; }
        public override string Name { get; set; }

        public string Email { get; set; }
        public DateTime CreateDate { get; set; }

        public List<UserCredential>UserCredentials { get; set; }  
        public List<ItemType> ItemTypes { get; set; }  
        public List<Tag> Tags { get; set; }
        public List<Item> Items { get; set; }
        public List<Folder> Folders { get; set; }
    }

    public class UserCredential
    {
        public long ID { get; set; }
        public Guid UserID { get; set; }

        // do not serialize credential information
        [IgnoreDataMember]
        public string Password { get; set; }
        [IgnoreDataMember]
        public string PasswordSalt { get; set; }
        [IgnoreDataMember]
        public string FBConsentToken { get; set; }
        [IgnoreDataMember]
        public string ADConsentToken { get; set; }

        public DateTime LastModified { get; set; }
    }
}