CREATE TABLE [dbo].[Suggestions](
	[ID] [uniqueidentifier] NOT NULL PRIMARY KEY CLUSTERED DEFAULT (newid()),
	[ItemID] [uniqueidentifier] NOT NULL,
	[WorkflowName] [nvarchar](64) NOT NULL,
    [WorkflowInstanceID] [uniqueidentifier] NOT NULL,
    [State] [nvarchar](256) NOT NULL,
	[FieldName] [nvarchar](64) NOT NULL,
	[DisplayName] [nvarchar](1024) NULL,
	[Value] [nvarchar](max) NULL,
	[TimeChosen] [datetime] NULL,
)
