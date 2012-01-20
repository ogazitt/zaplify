CREATE TABLE [dbo].[ItemTags](
	[ID] [uniqueidentifier] NOT NULL PRIMARY KEY CLUSTERED DEFAULT (newid()),
	[ItemID] [uniqueidentifier] NOT NULL REFERENCES [dbo].[Items] ([ID]) ON DELETE CASCADE ON UPDATE CASCADE,
	[TagID] [uniqueidentifier] NOT NULL REFERENCES [dbo].[Tags] ([ID]) ON DELETE NO ACTION ON UPDATE NO ACTION /* an item is more likely to be deleted than a tag */,
)
