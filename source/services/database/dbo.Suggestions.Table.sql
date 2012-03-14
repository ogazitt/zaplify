CREATE TABLE [dbo].[Suggestions](
	[ID] [uniqueidentifier] NOT NULL PRIMARY KEY CLUSTERED DEFAULT (newid()),
	[EntityID] [uniqueidentifier] NOT NULL,
	[EntityType] [nvarchar](32) NOT NULL,
	[WorkflowName] [nvarchar](64) NOT NULL,
    [WorkflowInstanceID] [uniqueidentifier] NOT NULL,
    [State] [nvarchar](256) NOT NULL,
	[FieldName] [nvarchar](64) NOT NULL,
	[DisplayName] [nvarchar](1024) NULL,
	[Value] [nvarchar](max) NULL,
	[TimeSelected] [datetime2] NULL,
	[ReasonSelected] [nvarchar](32) NULL,
)
