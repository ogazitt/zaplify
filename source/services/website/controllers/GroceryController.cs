namespace BuiltSteady.Zaplify.Website.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Web.Mvc;
    using BuiltSteady.Zaplify.GroceryService.Models;

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

            List<Grocery> possibleGroceryNames;
            if (startsWith != null)
            {   // filter by startsWith
                possibleGroceryNames = context.Groceries.Include("Category").Where(g => g.Name.StartsWith(startsWith)).ToList();
            }
            else
            {   // get all and post-filter
                possibleGroceryNames = context.Groceries.Include("Category").ToList();
            }

            contains = (contains != null) ? contains.ToLowerInvariant() : null;
            foreach (var grocery in possibleGroceryNames)
            {
                if (contains == null || grocery.Name.Contains(contains))
                {   // upper-case each word in name and add to results
                    var groceryName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(grocery.Name);
                    var groceryValue = new GroceryReturnValue() { Name = groceryName, Category = grocery.Category.Name };
                    groceryValue.ImageUrl = (!string.IsNullOrEmpty(grocery.ImageUrl)) ? grocery.ImageUrl : null;
                    groceries.Add(groceryValue);
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
    }
}
