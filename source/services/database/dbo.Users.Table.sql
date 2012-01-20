CREATE TABLE [dbo].[Users](
	[ID] [uniqueidentifier] NOT NULL PRIMARY KEY CLUSTERED DEFAULT (newid()),
	[Name] [nvarchar](50) NOT NULL,
	[Password] [nvarchar](50) NOT NULL,
	[Email] [nvarchar](max) NOT NULL,
)
