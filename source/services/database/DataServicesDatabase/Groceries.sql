CREATE TABLE [dbo].[Groceries]
(
	[ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY, 
    [Name] NVARCHAR(128) NOT NULL, 
    [GroceryCategoryID] INT NOT NULL, 
    CONSTRAINT [FK_Groceries_GroceryCategories] FOREIGN KEY ([GroceryCategoryID]) REFERENCES [GroceryCategories]([ID])
)
