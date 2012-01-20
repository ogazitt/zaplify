CREATE TABLE [dbo].[Items](
	[ID] [uniqueidentifier] NOT NULL PRIMARY KEY CLUSTERED DEFAULT (newid()),
	[Name] [nvarchar](max) NOT NULL,
	[ParentID] [uniqueidentifier] NULL REFERENCES [dbo].[Items] ([ID]) ON DELETE NO ACTION ON UPDATE NO ACTION /* self-join makes cascading hard (DAC barfs on it) */,
	[ItemTypeID] [uniqueidentifier] NOT NULL REFERENCES [dbo].[ItemTypes] ([ID]) ON DELETE NO ACTION ON UPDATE NO ACTION /* the change in the user will propagate through the itemtype */,
	[FolderID] [uniqueidentifier] NOT NULL REFERENCES [dbo].[Folders] ([ID]) ON DELETE CASCADE ON UPDATE CASCADE,
	[UserID] [uniqueidentifier] NOT NULL REFERENCES [dbo].[Users] ([ID]) ON DELETE NO ACTION ON UPDATE NO ACTION /* the change in the user will propagate through the folder */,
	[IsList] [bit] NOT NULL,
	[Created] [datetime] NOT NULL,
	[LastModified] [datetime] NOT NULL,
)
