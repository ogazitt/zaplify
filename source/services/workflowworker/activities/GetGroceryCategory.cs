﻿using System;
using System.Collections.Generic;
using System.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.ServiceUtilities.Supermarket;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.WorkflowWorker.Activities
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
                        TraceLog.TraceError("GetGroceryCategory: non-Item passed in to Function");
                        return Status.Error;
                    }

                    if (VerifyItemType(item, SystemItemTypes.ShoppingItem) == false)
                        return Status.Error;

                    // set up the Supermarket API context
                    SupermarketAPI smApi = new SupermarketAPI();

                    try
                    {
                        FieldValue categoryFV = GetFieldValue(item, FieldNames.Category, true);
                        var results = smApi.Query(SupermarketQueries.SearchByProductName, item.Name);
                        foreach (var entry in results)
                        {
                            categoryFV.Value = entry[SupermarketQueryResult.Category];
                            break;
                        }
                        UserContext.SaveChanges();
                        TraceLog.TraceInfo(String.Format("GetGroceryCategory: assigned {0} category to item {1}", categoryFV.Value, item.Name));
                    }
                    catch (Exception ex)
                    {
                        TraceLog.TraceException("GetGroceryCategory: Supermarket API call failed", ex);
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
