CREATE TABLE [dbo].[FolderUsers](
	[ID] [uniqueidentifier] NOT NULL PRIMARY KEY CLUSTERED DEFAULT (newid()),
	[FolderID] [uniqueidentifier] NOT NULL REFERENCES [dbo].[Folders] ([ID]) ON DELETE CASCADE ON UPDATE CASCADE,
	[UserID] [uniqueidentifier] NOT NULL REFERENCES [dbo].[Users] ([ID]) ON DELETE NO ACTION ON UPDATE NO ACTION /* the change in the user will propagate through the folder */,
	[PermissionID] [int] NOT NULL REFERENCES [dbo].[Permissions] ([PermissionID]) ON DELETE CASCADE ON UPDATE CASCADE,
)