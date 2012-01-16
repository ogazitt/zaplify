using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Reflection;

namespace BuiltSteady.Zaplify.Devices.ClientEntities
{
    [DataContract]
    public class ZaplifyEntity 
    {
        [DataMember]
        public virtual Guid ID { get; set; }

        [DataMember]
        public virtual string Name { get; set; }

        public ZaplifyEntity()
        {
            ID = Guid.NewGuid();
        }
    }
}