using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class Item
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public Guid FolderID { get; set; }
        public bool IsList { get; set; }
        public Guid ItemTypeID { get; set; }
        public Guid? ParentID { get; set; }
        public Guid UserID { get; set; }
        public List<ItemTag> ItemTags { get; set; }
        public List<FieldValue> FieldValues { get; set; }

        /*
        // these will go away
        public bool Complete { get; set; }
        public string Description { get; set; }
        public int? PriorityID { get; set; }
        public string DueDate { get; set; }
        public string Location { get; set; }
        public string Phone { get; set; }
        public string Website { get; set; }
        public string Email { get; set; }
        public Guid? LinkedFolderID { get; set; }
         */

        // these are first-class attributes of each item
        public DateTime Created { get; set; }
        public DateTime LastModified { get; set; } // this has to be the last field
    }
}