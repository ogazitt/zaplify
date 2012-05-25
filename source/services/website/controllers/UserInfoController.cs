namespace BuiltSteady.Zaplify.Website.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Mvc;

    using BuiltSteady.Zaplify.ServerEntities;
    using BuiltSteady.Zaplify.Shared.Entities;
    using System.Net;

    public class UserInfoController : BaseController
    {
        class JsSubjectsResult
        {
            public HttpStatusCode StatusCode = HttpStatusCode.OK;
            public int Count = -1;
            public Dictionary<string, string> Subjects = null;
        }

        public ActionResult PossibleSubjects(string startsWith = null, string contains = null, int maxCount = 10)
        {
            JsSubjectsResult subjectResults = new JsSubjectsResult();
            Folder userFolder = StorageContext.GetOrCreateUserFolder(CurrentUser);
            if (StorageContext.Items.Any(item => item.UserID == CurrentUser.ID && item.FolderID == userFolder.ID && item.Name == SystemEntities.PossibleSubjects))
            {
                Dictionary<string, string> subjects = new Dictionary<string, string>();
                Item possibleSubjectList = StorageContext.Items.Single(item => item.UserID == CurrentUser.ID && item.FolderID == userFolder.ID && item.Name == SystemEntities.PossibleSubjects);
                List<Item> possibleSubjects = StorageContext.Items.
                    Include("FieldValues").
                    Where(item => item.UserID == CurrentUser.ID
                        && item.FolderID == userFolder.ID
                        && item.ParentID == possibleSubjectList.ID
                        //&& item.Name.StartsWith(startsWith)           // entity framework does not support ignore case
                        && System.Data.Objects.SqlClient.SqlFunctions.PatIndex(startsWith + "%", item.Name) == 1
                    ).ToList<Item>();

                foreach (var item in possibleSubjects)
                {
                    if (startsWith == null || 
                        item.Name.StartsWith(startsWith, StringComparison.OrdinalIgnoreCase) ||
                        (contains != null && item.Name.Contains(contains)))
                    {
                        FieldValue fv = item.GetFieldValue(FieldNames.Value);
                        if (fv != null)
                        {
                            if (subjects.ContainsKey(item.Name))
                            {   // disambiguate duplicate names
                                item.Name = string.Format("{0} ({1})", item.Name, subjects.Count.ToString());
                            }
                            subjects.Add(item.Name, fv.Value);
                        }
                        if (subjects.Count == maxCount) { break; }
                    }
                }
                subjectResults.Count = subjects.Count;
                subjectResults.Subjects = subjects;
            }

            JsonResult result = new JsonResult();
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            result.Data = subjectResults;
            return result;
        }

    }
}
