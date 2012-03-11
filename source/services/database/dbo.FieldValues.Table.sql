CREATE TABLE [dbo].[FieldValues](
/*	[ID] [uniqueidentifier] NOT NULL PRIMARY KEY CLUSTERED DEFAULT (newid()), */
    [ID] [bigint] IDENTITY(1, 1) NOT NULL,
	[FieldID] [uniqueidentifier] NOT NULL REFERENCES [dbo].[Fields] ([ID]) ON DELETE CASCADE ON UPDATE CASCADE,
	[ItemID] [uniqueidentifier] NOT NULL REFERENCES [dbo].[Items] ([ID]) ON DELETE CASCADE ON UPDATE CASCADE,
	[Value] [nvarchar](max) NULL,

    CONSTRAINT [PK_FieldValueID] PRIMARY KEY ([ID]),
    CONSTRAINT [UNIQUE_FieldValue] UNIQUE ([FieldID], [ItemID])
)
