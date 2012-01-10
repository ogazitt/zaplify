using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace TaskStoreServerEntities
{
    public class Operation
    {
        public Guid ID { get; set; }
        public Guid UserID { get; set; }
        public string Username { get; set; }
        public Guid EntityID { get; set; }
        public string EntityName { get; set; }
        public string EntityType { get; set; }
        public string OperationType { get; set; }
        public string Body { get; set; }
        public string OldBody { get; set; }
        public DateTime Timestamp { get; set; }
    }
}