/****** Object:  Table [dbo].[FieldTypes]    Script Date: 01/18/2012 11:19:55 ******/
SET IDENTITY_INSERT [dbo].[FieldTypes] ON
INSERT [dbo].[FieldTypes] ([FieldTypeID], [Name], [DataType], [DisplayName], [DisplayType]) VALUES (1, N'Name', N'String', N'Name', N'String')
INSERT [dbo].[FieldTypes] ([FieldTypeID], [Name], [DataType], [DisplayName], [DisplayType]) VALUES (2, N'Description', N'String', N'Description', N'String')
INSERT [dbo].[FieldTypes] ([FieldTypeID], [Name], [DataType], [DisplayName], [DisplayType]) VALUES (3, N'PriorityID', N'Int32', N'Priority', N'Priority')
INSERT [dbo].[FieldTypes] ([FieldTypeID], [Name], [DataType], [DisplayName], [DisplayType]) VALUES (4, N'DueDate', N'DateTime', N'Due', N'Date')
INSERT [dbo].[FieldTypes] ([FieldTypeID], [Name], [DataType], [DisplayName], [DisplayType]) VALUES (5, N'TaskTags', N'List''TaskTag', N'Tags (separated by commas)', N'TagList')
INSERT [dbo].[FieldTypes] ([FieldTypeID], [Name], [DataType], [DisplayName], [DisplayType]) VALUES (6, N'Location', N'String', N'Location', N'Address')
INSERT [dbo].[FieldTypes] ([FieldTypeID], [Name], [DataType], [DisplayName], [DisplayType]) VALUES (7, N'Phone', N'String', N'Phone', N'PhoneNumber')
INSERT [dbo].[FieldTypes] ([FieldTypeID], [Name], [DataType], [DisplayName], [DisplayType]) VALUES (8, N'Website', N'String', N'Website', N'Website')
INSERT [dbo].[FieldTypes] ([FieldTypeID], [Name], [DataType], [DisplayName], [DisplayType]) VALUES (9, N'Email', N'String', N'Email', N'Email')
INSERT [dbo].[FieldTypes] ([FieldTypeID], [Name], [DataType], [DisplayName], [DisplayType]) VALUES (10, N'Complete', N'Boolean', N'Complete', N'Boolean')
INSERT [dbo].[FieldTypes] ([FieldTypeID], [Name], [DataType], [DisplayName], [DisplayType]) VALUES (11, N'Description', N'String', N'Details', N'TextBox')
INSERT [dbo].[FieldTypes] ([FieldTypeID], [Name], [DataType], [DisplayName], [DisplayType]) VALUES (12, N'ParentID', N'List''FieldValue', N'list', N'List')
SET IDENTITY_INSERT [dbo].[FieldTypes] OFF
GO
