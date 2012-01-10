using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace TaskStoreServerEntities
{
    public class Task
    {
        public Guid ID { get; set; }
        public Guid TaskListID { get; set; }
        public string Name { get; set; }
        public bool Complete { get; set; }
        public string Description { get; set; }
        public int? PriorityID { get; set; }
        public string DueDate { get; set; }
        public string Location { get; set; }
        public string Phone { get; set; }
        public string Website { get; set; }
        public string Email { get; set; }
        public Guid? LinkedTaskListID { get; set; }
        public List<TaskTag> TaskTags { get; set; }
        public List<FieldValue> FieldValues { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastModified { get; set; } // this has to be the last field
    }
}