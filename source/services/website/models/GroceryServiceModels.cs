namespace BuiltSteady.Zaplify.GroceryService.Models
{
    using System.Collections.Generic;
    using System.Data.Entity;

    using BuiltSteady.Zaplify.ServiceHost;

    public class Grocery
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int GroceryCategoryID { get; set; }
        public GroceryCategory Category { get; set; }
        public string ImageUrl { get; set; }
    }

    public class GroceryCategory
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }

    public class GroceryCategories
    {
        public static List<GroceryCategory> Categories = new List<GroceryCategory>()
        {
            new GroceryCategory { ID = 0, Name = "Baby" },
            new GroceryCategory { ID = 1, Name = "Beer, Wine & Spirits" },
            new GroceryCategory { ID = 2, Name = "Beverages" },
            new GroceryCategory { ID = 3, Name = "Bread & Bakery" },
            new GroceryCategory { ID = 4, Name = "Breakfast & Cereal" },
            new GroceryCategory { ID = 5, Name = "Canned Goods & Soups" },
            new GroceryCategory { ID = 6, Name = "Condiments/Spices & Bake" },
            new GroceryCategory { ID = 7, Name = "Cookies, Snacks & Candy" },
            new GroceryCategory { ID = 8, Name = "Dairy, Eggs & Cheese" },
            new GroceryCategory { ID = 9, Name = "Deli & Signature Cafe" },
            new GroceryCategory { ID = 10, Name = "Frozen Foods" },
            new GroceryCategory { ID = 11, Name = "Fruits & Vegetables" },
            new GroceryCategory { ID = 12, Name = "Grains, Pasta & Sides" },
            new GroceryCategory { ID = 13, Name = "International Cuisine" },
            new GroceryCategory { ID = 14, Name = "Meat & Seafood" },
            new GroceryCategory { ID = 15, Name = "Paper, Cleaning & Home" },
            new GroceryCategory { ID = 16, Name = "Personal Care & Pharmacy" },
            new GroceryCategory { ID = 17, Name = "Pet Care" },
        };
    }

    // DbContext for the Grocery DB
    public class GroceryContext : DbContext
    {
        public GroceryContext() : base(HostEnvironment.DataServicesConnection) { }
        public GroceryContext(string connection) : base(connection) { }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }

        public DbSet<Grocery> Groceries { get; set; }
        public DbSet<GroceryCategory> GroceryCategories { get; set; }
    }

}
