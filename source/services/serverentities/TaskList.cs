using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace TaskStoreServerEntities
{
    public class TaskList
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public Guid ListTypeID { get; set; }
        public bool Template { get; set; }
        public Guid UserID { get; set; }
        public List<Task> Tasks { get; set; }
    }
}