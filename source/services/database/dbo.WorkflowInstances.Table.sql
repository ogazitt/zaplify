CREATE TABLE [dbo].[WorkflowInstances](
	[ID] [uniqueidentifier] NOT NULL PRIMARY KEY CLUSTERED DEFAULT (newid()),
	[WorkflowType] [nvarchar](256) NOT NULL,
	[State] [nvarchar](256) NULL,
	[EntityID] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](256) NOT NULL,
	[InstanceData] [nvarchar](max) NOT NULL,
	[Created] [datetime2] NOT NULL,
	[LastModified] [datetime2] NOT NULL,
)
