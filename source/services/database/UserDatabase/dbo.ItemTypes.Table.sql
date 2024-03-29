CREATE TABLE [dbo].[ItemTypes](
	[ID] [uniqueidentifier] NOT NULL PRIMARY KEY CLUSTERED DEFAULT (newid()),
	[Name] [nvarchar](64) NOT NULL,
	[UserID] [uniqueidentifier] NOT NULL REFERENCES [dbo].[Users] ([ID]) ON DELETE CASCADE ON UPDATE CASCADE,
	[Icon] [nvarchar](256) NULL,
)
