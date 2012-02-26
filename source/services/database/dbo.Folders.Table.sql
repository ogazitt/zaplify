CREATE TABLE [dbo].[Folders](
	[ID] [uniqueidentifier] NOT NULL PRIMARY KEY CLUSTERED DEFAULT (newid()),
	[Name] [nvarchar](max) NOT NULL,
	[UserID] [uniqueidentifier] NOT NULL REFERENCES [dbo].[Users] ([ID]) ON DELETE CASCADE ON UPDATE CASCADE,
	[ColorID] [int] NULL REFERENCES [dbo].[Colors] ([ColorID]) ON DELETE CASCADE ON UPDATE CASCADE,
	[DefaultItemTypeID] [uniqueidentifier] NOT NULL REFERENCES [dbo].[ItemTypes] ([ID]) ON DELETE NO ACTION ON UPDATE NO ACTION,
)