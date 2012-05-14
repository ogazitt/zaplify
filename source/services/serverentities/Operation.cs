using System;

namespace BuiltSteady.Zaplify.ServerEntities
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
        public int? StatusCode { get; set; }
        public DateTime Timestamp { get; set; }
        
        public override string ToString()
        {
            return this.EntityName;
        }
    }
}