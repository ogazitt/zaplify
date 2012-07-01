using System;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.ServiceHost
{
    public static class FieldProcessor
    {
        // OBSOLETE! Stamping of CompletedOn field is being done on each client to get proper timezone and immediate response.
        // should only be called if newItem.Complete value is true
        public static bool ProcessUpdateCompletedOn(UserStorageContext userContext, Item oldItem, Item newItem)
        {
            var wasComplete = oldItem.GetFieldValue(FieldNames.Complete);
            if (wasComplete == null || !wasComplete.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
            {   // timestamp the CompletedOn field
                newItem.GetFieldValue(FieldNames.CompletedOn, true).Value = DateTime.UtcNow.ToString();
            }
            return false;
        }
    }
}
