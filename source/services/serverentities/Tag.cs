using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TaskStoreServerEntities
{
    public class Tag
    {
        public Guid ID { get; set; }
        public Guid UserID { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
        public List<TaskTag> TaskTags { get; set; }
    }
}