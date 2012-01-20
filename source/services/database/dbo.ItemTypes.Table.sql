CREATE TABLE [dbo].[ItemTypes](
	[ID] [uniqueidentifier] NOT NULL PRIMARY KEY CLUSTERED DEFAULT (newid()),
	[Name] [nvarchar](50) NOT NULL,
	[UserID] [uniqueidentifier] NULL REFERENCES [dbo].[Users] ([ID]) ON DELETE CASCADE ON UPDATE CASCADE,
	[Icon] [nvarchar](50) NULL,
)
