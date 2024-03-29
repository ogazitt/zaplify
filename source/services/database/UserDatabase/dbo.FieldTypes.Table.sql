CREATE TABLE [dbo].[FieldTypes](
	[FieldTypeID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[DataType] [nvarchar](50) NOT NULL,
	[DisplayName] [nvarchar](50) NULL,
	[DisplayType] [nvarchar](50) NULL,
 CONSTRAINT [PK_FieldTypes] PRIMARY KEY CLUSTERED ([FieldTypeID] ASC)
)

