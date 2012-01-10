using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TaskStoreServerEntities
{
    public class User
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public List<ListType> ListTypes { get; set; }  
        public List<Tag> Tags { get; set; }
        public List<TaskList> TaskLists { get; set; }
    }
}