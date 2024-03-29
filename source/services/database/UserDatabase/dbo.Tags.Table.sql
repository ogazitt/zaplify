CREATE TABLE [dbo].[Tags](
	[ID] [uniqueidentifier] NOT NULL PRIMARY KEY CLUSTERED DEFAULT (newid()),
	[Name] [nvarchar](64) NOT NULL,
	[UserID] [uniqueidentifier] NOT NULL REFERENCES [dbo].[Users] ([ID]) ON DELETE CASCADE ON UPDATE CASCADE,
	[ColorID] [int] NOT NULL REFERENCES [dbo].[Colors] ([ColorID]) ON DELETE CASCADE ON UPDATE CASCADE,
)
