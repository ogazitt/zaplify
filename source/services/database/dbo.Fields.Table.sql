CREATE TABLE [dbo].[Fields](
	[ID] [uniqueidentifier] NOT NULL DEFAULT (newid()),
	[Name] [nvarchar](50) NULL,
	[FieldTypeID] [int] NOT NULL REFERENCES [dbo].[FieldTypes] ([FieldTypeID]),
	[ItemTypeID] [uniqueidentifier] NOT NULL REFERENCES [dbo].[ItemTypes] ([ID]),
	[DisplayName] [nvarchar](50) NOT NULL,
	[DisplayType] [nvarchar](50) NOT NULL,
	[IsPrimary] [bit] NOT NULL,
	[SortOrder] [int] NOT NULL,
 CONSTRAINT [PK_Fields] PRIMARY KEY CLUSTERED ([ID] ASC)
)
