CREATE TABLE [dbo].[Suggestions](
	[ID] [uniqueidentifier] NOT NULL PRIMARY KEY CLUSTERED DEFAULT (newid()),
	[ParentID] [uniqueidentifier] NULL, /* REFERENCES [dbo].[Suggestions] ([ID]) ON DELETE NO ACTION ON UPDATE NO ACTION, */ /* if we make this a self-join, DAC barfs on it - therefore ref integrity buys us nothing */
	[SuggestionType] [nvarchar](64) NOT NULL,
	[EntityID] [uniqueidentifier] NOT NULL,
	[EntityType] [nvarchar](32) NOT NULL,
	[WorkflowType] [nvarchar](256) NOT NULL,
    [WorkflowInstanceID] [uniqueidentifier] NOT NULL,
    [State] [nvarchar](256) NOT NULL,
	[DisplayName] [nvarchar](1024) NULL,
	[GroupDisplayName] [nvarchar](1024) NULL,
	[SortOrder] [int] NULL,
	[Value] [nvarchar](max) NULL,
	[TimeSelected] [datetime2] NULL,
	[ReasonSelected] [nvarchar](32) NULL,
)
