CREATE TABLE [dbo].[Folders](
	[ID] [uniqueidentifier] NOT NULL PRIMARY KEY CLUSTERED DEFAULT (newid()),
	[Name] [nvarchar](256) NOT NULL,
	[UserID] [uniqueidentifier] NOT NULL REFERENCES [dbo].[Users] ([ID]) ON DELETE CASCADE ON UPDATE CASCADE,
	[ItemTypeID] [uniqueidentifier] NOT NULL REFERENCES [dbo].[ItemTypes] ([ID]) ON DELETE NO ACTION ON UPDATE NO ACTION,
	[SortOrder] [real] NOT NULL,
)