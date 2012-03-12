﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.WorkflowWorker.Workflows;

namespace BuiltSteady.Zaplify.WorkflowWorker.Activities
{
    public class GetSuggestions : WorkflowActivity
    {
        public override string Name { get { return ActivityNames.GetSuggestions; } }
        public override string TargetFieldName { get { return "Suggestions"; } }
        public override Func<WorkflowInstance, Item, object, List<Guid>, bool> Function
        {
            get
            {
                return ((workflowInstance, item, state, list) =>
                {
                    try
                    {
                        foreach (var s in "golf club;sounders jersey;outliers".Split(';'))
                        {
                            var url = "http://www.bing.com/search?q=" + s.Replace(' ', '+');
                            var sugg = new Suggestion()
                            {
                                ID = Guid.NewGuid(),
                                ItemID = item.ID,
                                Type = "URL",
                                Name = s,
                                Value = url,
                                Retrieved = false,
                                Created = DateTime.Now
                            };
                            WorkflowWorker.StorageContext.Suggestions.Add(sugg);
                            list.Add(sugg.ID);
                        }
                        WorkflowWorker.StorageContext.SaveChanges();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        LoggingHelper.TraceError("GetSuggestions Activity failed; ex: " + ex.Message);
                    }
                    return true;
                });
            }
        }
    }
}
