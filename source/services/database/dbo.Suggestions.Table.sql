CREATE TABLE [dbo].[Suggestions](
	[ID] [uniqueidentifier] NOT NULL PRIMARY KEY CLUSTERED DEFAULT (newid()),
	[ItemID] [uniqueidentifier] NOT NULL,
	[Type] [char](10) NOT NULL, /* 'URL', 'ItemRef' */
	[Name] [nvarchar](256) NOT NULL,
	[Value] [nvarchar](max) NOT NULL,
	[Retrieved] [bit] NOT NULL DEFAULT(0),
	[Created] [datetime] NOT NULL,
)
