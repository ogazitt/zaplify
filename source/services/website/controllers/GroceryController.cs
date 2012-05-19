namespace BuiltSteady.Zaplify.Website.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Web.Mvc;
    using BuiltSteady.Zaplify.GroceryService.Models;
    using System.IO;
    using BuiltSteady.Zaplify.ServiceHost;

    //[Authorize]
    public class GroceryController : Controller
    {
        class JsGroceryResults
        {
            public HttpStatusCode StatusCode = HttpStatusCode.OK;
            public int Count = -1;
            public object Groceries = null;
        }

        class GroceryReturnValue
        {
            public string Name;
            public string Category;
            public string ImageUrl;
        }

        public ActionResult GroceryNames(string startsWith = null, string contains = null, int maxCount = 10)
        {
            JsGroceryResults groceryResults = new JsGroceryResults();
            var context = new GroceryContext();
            List<GroceryReturnValue> groceries = new List<GroceryReturnValue>();

            var possibleGroceryNames = context.Groceries.Include("Category").Where(g => g.Name.StartsWith(startsWith) || g.Name.Contains(contains)).ToList();
            foreach (var grocery in possibleGroceryNames)
            {
                if (startsWith == null ||
                    grocery.Name.StartsWith(startsWith, StringComparison.OrdinalIgnoreCase) ||
                    (contains != null && grocery.Name.Contains(contains)))
                {
                    groceries.Add(new GroceryReturnValue() { Name = grocery.Name, Category = grocery.Category.Name });
                }
                if (groceries.Count == maxCount) { break; }
            }

            groceryResults.Count = groceries.Count;
            groceryResults.Groceries = groceries;

            JsonResult result = new JsonResult();
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            result.Data = groceryResults;
            return result;
        }

        public ActionResult GroceryCategories(string startsWith = null, string contains = null, int maxCount = 10)
        {
            JsGroceryResults groceryResults = new JsGroceryResults();
            var context = new GroceryContext();
            List<string> categories = new List<string>();

            var possibleCategories = context.GroceryCategories.Where(g => g.Name.StartsWith(startsWith) || g.Name.Contains(contains)).ToList();
            foreach (var category in possibleCategories)
            {
                if (startsWith == null ||
                    category.Name.StartsWith(startsWith, StringComparison.OrdinalIgnoreCase) ||
                    (contains != null && category.Name.Contains(contains)))
                {
                    categories.Add(category.Name);
                }
                if (categories.Count == maxCount) { break; }
            }

            groceryResults.Count = categories.Count;
            groceryResults.Groceries = categories;

            JsonResult result = new JsonResult();
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            result.Data = groceryResults;
            return result;
        }

        public ActionResult GroceryInfo(string name)
        {
            ServiceHost.TraceLog.TraceDetail("GroceryInfo called with " + name);
            JsGroceryResults groceryResults = new JsGroceryResults();
            var context = new GroceryContext();
            List<GroceryReturnValue> grocery = new List<GroceryReturnValue>();
            var groceryName = name.ToLower();

            try
            {
                var groc = context.Groceries.Include("Category").OrderBy(g => g.Name).First(g => g.Name.StartsWith(groceryName));
                grocery.Add(new GroceryReturnValue() { Name = groc.Name, Category = groc.Category.Name, ImageUrl = groc.ImageUrl });
                ServiceHost.TraceLog.TraceDetail(String.Format("GroceryInfo: found {0} category for {1}", groc.Category.Name, name));
            }
            catch (Exception)
            {
                ServiceHost.TraceLog.TraceDetail("GroceryInfo: could not find a category for " + name);
            }

            groceryResults.Count = grocery.Count;
            groceryResults.Groceries = grocery;

            JsonResult result = new JsonResult();
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            result.Data = groceryResults;
            return result;
        }

        public ActionResult ReloadGroceryData()
        {
            var context = new GroceryContext();

            // remove groceries and categories
            var groceries = context.Groceries.ToList();
            foreach (var g in groceries)
                context.Groceries.Remove(g);
            context.SaveChanges();
            var categories = context.GroceryCategories.ToList();
            foreach (var c in categories)
                context.GroceryCategories.Remove(c);
            context.SaveChanges();

            // recreate categories from the model
            foreach (var c in GroceryService.Models.GroceryCategories.Categories)
                context.GroceryCategories.Add(new GroceryCategory() { ID = c.ID, Name = c.Name });                
            context.SaveChanges();

            // load the grocery names and their categories from a tab-delimited flat file
            try
            {
                string filename = Server.MapPath("~/models/").Substring(0, 1) + @":\approot\bin\models\groceries.txt";
                if (HostEnvironment.IsAzureDevFabric)
                {
                    filename = HttpContext.Request.PhysicalApplicationPath + @"bin\models\groceries.txt";
                }
                using (var stream = System.IO.File.Open(filename, FileMode.Open, FileAccess.Read))
                using (var reader = new StreamReader(stream))
                {
                    var groceryInfo = reader.ReadLine();
                    while (!String.IsNullOrEmpty(groceryInfo))
                    {
                        var keyval = groceryInfo.Split('\t');
                        if (keyval.Length >= 2)
                        {
                            // store the grocery name in lowercase and look up the category ID by name
                            var groceryName = keyval[0].ToLower();
                            var categoryName = keyval[1].Trim('"');
                            var category = context.GroceryCategories.First(c => c.Name == categoryName);
                            var grocery = new Grocery() { Name = groceryName, GroceryCategoryID = category.ID };
                            if (keyval.Length >= 3)
                                grocery.ImageUrl = keyval[2].Trim();
                            context.Groceries.Add(grocery);
                        }
                        groceryInfo = reader.ReadLine();
                    }
                    context.SaveChanges();
                }
            }
            catch (Exception)
            {
                context.SaveChanges();
            }
            return new EmptyResult();
        }
    }
}
