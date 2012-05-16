/*
Post-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script.		
 Use SQLCMD syntax to include a file in the post-deployment script.			
 Example:      :r .\myfile.sql								
 Use SQLCMD syntax to reference a variable in the post-deployment script.		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/
SET IDENTITY_INSERT [dbo].[GroceryCategories] ON
INSERT [dbo].[GroceryCategories] ([ID], [Name]) VALUES (0, N'Baby')
INSERT [dbo].[GroceryCategories] ([ID], [Name]) VALUES (1, N'Beer, Wine & Spirits')
INSERT [dbo].[GroceryCategories] ([ID], [Name]) VALUES (2, N'Beverages')
INSERT [dbo].[GroceryCategories] ([ID], [Name]) VALUES (3, N'Bread & Bakery')
INSERT [dbo].[GroceryCategories] ([ID], [Name]) VALUES (4, N'Breakfast & Cereal')
INSERT [dbo].[GroceryCategories] ([ID], [Name]) VALUES (5, N'Canned Goods & Soups')
INSERT [dbo].[GroceryCategories] ([ID], [Name]) VALUES (6, N'Condiments/Spices & Bake')
INSERT [dbo].[GroceryCategories] ([ID], [Name]) VALUES (7, N'Cookies, Snacks & Candy')
INSERT [dbo].[GroceryCategories] ([ID], [Name]) VALUES (8, N'Dairy, Eggs & Cheese')
INSERT [dbo].[GroceryCategories] ([ID], [Name]) VALUES (9, N'Deli & Signature Cafe')
INSERT [dbo].[GroceryCategories] ([ID], [Name]) VALUES (10, N'Frozen Foods')
INSERT [dbo].[GroceryCategories] ([ID], [Name]) VALUES (11, N'Fruits & Vegetables')
INSERT [dbo].[GroceryCategories] ([ID], [Name]) VALUES (12, N'Grains, Pasta & Sides')
INSERT [dbo].[GroceryCategories] ([ID], [Name]) VALUES (13, N'International Cuisine')
INSERT [dbo].[GroceryCategories] ([ID], [Name]) VALUES (14, N'Meat & Seafood')
INSERT [dbo].[GroceryCategories] ([ID], [Name]) VALUES (15, N'Paper, Cleaning & Home')
INSERT [dbo].[GroceryCategories] ([ID], [Name]) VALUES (16, N'Personal Care & Pharmacy')
INSERT [dbo].[GroceryCategories] ([ID], [Name]) VALUES (17, N'Pet Care')
SET IDENTITY_INSERT [dbo].[GroceryCategories] OFF
