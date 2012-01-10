using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Reflection;

namespace TaskStoreClientEntities
{
    [DataContract]
    public class TaskStoreEntity 
    {
        [DataMember]
        public virtual Guid ID { get; set; }

        [DataMember]
        public virtual string Name { get; set; }

        public TaskStoreEntity()
        {
            ID = Guid.NewGuid();
        }
    }
}