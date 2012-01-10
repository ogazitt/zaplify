using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TaskStoreServerEntities
{
    public class ListType
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public Guid? UserID { get; set; }
        public List<Field> Fields { get; set; }
    }
}