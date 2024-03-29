CREATE TABLE [dbo].[Fields](
	[ID] [uniqueidentifier] NOT NULL DEFAULT (newid()),
	[ItemTypeID] [uniqueidentifier] NOT NULL REFERENCES [dbo].[ItemTypes] ([ID]),
	[Name] [nvarchar](64) NOT NULL,
	[FieldType] [nvarchar](64),
	[DisplayName] [nvarchar](64) NOT NULL,
	[DisplayType] [nvarchar](64) NOT NULL,
	[IsPrimary] [bit] NOT NULL,
	[SortOrder] [int] NOT NULL,
 CONSTRAINT [PK_Fields] PRIMARY KEY CLUSTERED ([ID] ASC)
)
