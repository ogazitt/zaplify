CREATE TABLE [dbo].[Operations](
	[ID] [uniqueidentifier] NOT NULL PRIMARY KEY CLUSTERED DEFAULT (newid()),
	[UserID] [uniqueidentifier] NOT NULL REFERENCES [dbo].[Users] ([ID]) ON DELETE CASCADE ON UPDATE CASCADE,
	[Username] [nvarchar](256) NOT NULL,
	[EntityID] [uniqueidentifier] NOT NULL,
	[EntityName] [nvarchar](max) NOT NULL,
	[EntityType] [nchar](32) NOT NULL,
	[OperationType] [nchar](6) NOT NULL,
	[Body] [nvarchar](max) NOT NULL,
	[OldBody] [nvarchar](max) NOT NULL,
	[StatusCode] [int] NULL,
	[Timestamp] [datetime2] NOT NULL,
)
