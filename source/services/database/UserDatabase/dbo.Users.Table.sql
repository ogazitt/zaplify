CREATE TABLE [dbo].[Users](
	[ID] [uniqueidentifier] NOT NULL PRIMARY KEY CLUSTERED DEFAULT (newid()),
	[Name] [nvarchar](256) NOT NULL,
	[Email] [nvarchar](256) NOT NULL,
	[CreateDate] [datetime2] NOT NULL,
)
