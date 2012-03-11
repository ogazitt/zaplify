CREATE TABLE [dbo].[WorkflowInstances](
	[ID] [uniqueidentifier] NOT NULL PRIMARY KEY CLUSTERED DEFAULT (newid()),
	[WorkflowType] [nvarchar](256) NOT NULL,
	[State] [nvarchar](256) NULL,
	[ItemID] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](256) NOT NULL,
	[Body] [nvarchar](max) NOT NULL,
	[Created] [datetime] NOT NULL,
	[LastModified] [datetime] NOT NULL,
)
