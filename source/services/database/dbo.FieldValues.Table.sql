CREATE TABLE [dbo].[FieldValues](
	[ID] [uniqueidentifier] NOT NULL PRIMARY KEY CLUSTERED DEFAULT (newid()),
	[FieldID] [uniqueidentifier] NOT NULL REFERENCES [dbo].[Fields] ([ID]) ON DELETE CASCADE ON UPDATE CASCADE,
	[ItemID] [uniqueidentifier] NOT NULL REFERENCES [dbo].[Items] ([ID]) ON DELETE CASCADE ON UPDATE CASCADE,
	[Value] [nvarchar](max) NULL,
/* CONSTRAINT [PK_FieldValues] PRIMARY KEY CLUSTERED ([ID] ASC)*/
)
