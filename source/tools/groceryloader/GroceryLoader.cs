using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuiltSteady.Zaplify.GroceryService.Models;
using System.IO;

namespace BuiltSteady.Zaplify.Tools.GroceryLoader
{
    public class GroceryLoader
    {
        public static bool ReloadGroceryData(string connectionString, string filename)
        {
            var context = new GroceryContext(connectionString);

            // remove groceries and categories
            Console.WriteLine("Removing groceries");
            var groceries = context.Groceries.ToList();
            foreach (var g in groceries)
                context.Groceries.Remove(g);
            context.SaveChanges();
            Console.WriteLine("Removing categories");
            var categories = context.GroceryCategories.ToList();
            foreach (var c in categories)
                context.GroceryCategories.Remove(c);
            context.SaveChanges();

            // recreate categories from the model
            Console.WriteLine("Adding categories");
            foreach (var c in BuiltSteady.Zaplify.GroceryService.Models.GroceryCategories.Categories)
                context.GroceryCategories.Add(new GroceryCategory() { ID = c.ID, Name = c.Name });
            context.SaveChanges();

            // load the grocery names and their categories from a tab-delimited flat file
            try
            {
                filename = filename ?? @"groceries.txt";
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
                            Console.WriteLine("Added " + grocery.Name);
                        }
                        groceryInfo = reader.ReadLine();
                    }
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GroceryLoader: load failed; ex: ", ex.Message);
                context.SaveChanges();
                return false;
            }
            return true;
        }
    }
}
