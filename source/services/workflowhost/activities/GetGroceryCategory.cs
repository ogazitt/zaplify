using System;
using System.Collections.Generic;
using System.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.ServiceUtilities.Supermarket;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.WorkflowHost.Activities
{
    public class GetGroceryCategory : WorkflowActivity
    {
        public override Func<WorkflowInstance, ServerEntity, object, Status> Function
        {
            get
            {
                return ((workflowInstance, entity, data) =>
                {
                    Item item = entity as Item;
                    if (item == null)
                    {
                        TraceLog.TraceError("Entity is not an Item");
                        return Status.Error;
                    }

                    if (VerifyItemType(item, SystemItemTypes.Grocery) == false)
                        return Status.Error;

                    // set up the Supermarket API context
                    SupermarketAPI smApi = new SupermarketAPI();

                    try
                    {
                        var results = smApi.Query(SupermarketQueries.SearchByProductName, item.Name);
                        FieldValue categoryFV = item.GetFieldValue(FieldNames.Category, true);
                        foreach (var entry in results)
                        {
                            categoryFV.Value = entry[SupermarketQueryResult.Category];
                            break;
                        }
                        UserContext.SaveChanges();
                        TraceLog.TraceInfo(String.Format("Assigned {0} category to Item {1}", categoryFV.Value, item.Name));
                    }
                    catch (Exception ex)
                    {
                        TraceLog.TraceException("Supermarket API call failed", ex);
                        return Status.Error;
                    }

                    // signal the client to reload the entity
                    SignalEntityRefresh(workflowInstance, entity);

                    return Status.Complete;
                });
            }
        }
    }
}
