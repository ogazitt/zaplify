/*
Post-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script.		
 Use SQLCMD syntax to include a file in the post-deployment script.			
 Example:      :r .\myfile.sql								
 Use SQLCMD syntax to reference a variable in the post-deployment script.		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/
/****** Object:  Table [dbo].[ActionTypes]    Script Date: 01/18/2012 11:19:55 ******/
SET IDENTITY_INSERT [dbo].[ActionTypes] ON
INSERT [dbo].[ActionTypes] ([ActionTypeID], [ActionName], [DisplayName], [FieldName], [SortOrder]) VALUES (1, N'Postpone', N'postpone', N'DueDate', 1)
INSERT [dbo].[ActionTypes] ([ActionTypeID], [ActionName], [DisplayName], [FieldName], [SortOrder]) VALUES (2, N'AddToCalendar', N'add reminder', N'DueDate', 2)
INSERT [dbo].[ActionTypes] ([ActionTypeID], [ActionName], [DisplayName], [FieldName], [SortOrder]) VALUES (3, N'Map', N'map', N'Location', 3)
INSERT [dbo].[ActionTypes] ([ActionTypeID], [ActionName], [DisplayName], [FieldName], [SortOrder]) VALUES (4, N'Phone', N'call', N'Phone', 4)
INSERT [dbo].[ActionTypes] ([ActionTypeID], [ActionName], [DisplayName], [FieldName], [SortOrder]) VALUES (5, N'TextMessage', N'text', N'Phone', 5)
INSERT [dbo].[ActionTypes] ([ActionTypeID], [ActionName], [DisplayName], [FieldName], [SortOrder]) VALUES (6, N'Browse', N'browse', N'Website', 6)
INSERT [dbo].[ActionTypes] ([ActionTypeID], [ActionName], [DisplayName], [FieldName], [SortOrder]) VALUES (7, N'Email', N'email', N'Email', 7)
SET IDENTITY_INSERT [dbo].[ActionTypes] OFF
GO

/****** Object:  Table [dbo].[Colors]    Script Date: 01/18/2012 11:19:55 ******/
SET IDENTITY_INSERT [dbo].[Colors] ON
INSERT [dbo].[Colors] ([ColorID], [Name]) VALUES (0, N'White')
INSERT [dbo].[Colors] ([ColorID], [Name]) VALUES (1, N'Blue')
INSERT [dbo].[Colors] ([ColorID], [Name]) VALUES (2, N'Brown')
INSERT [dbo].[Colors] ([ColorID], [Name]) VALUES (3, N'Green')
INSERT [dbo].[Colors] ([ColorID], [Name]) VALUES (4, N'Orange')
INSERT [dbo].[Colors] ([ColorID], [Name]) VALUES (5, N'Purple')
INSERT [dbo].[Colors] ([ColorID], [Name]) VALUES (6, N'Red')
INSERT [dbo].[Colors] ([ColorID], [Name]) VALUES (7, N'Yellow')
SET IDENTITY_INSERT [dbo].[Colors] OFF
GO

/****** Object:  Table [dbo].[ItemTypes]    Script Date: 01/18/2012 11:19:55 ******/
INSERT [dbo].[ItemTypes] ([ID], [Name], [UserID], [Icon]) VALUES (N'00000000-0000-0000-0000-000000000001', N'To Do', NULL, NULL)
INSERT [dbo].[ItemTypes] ([ID], [Name], [UserID], [Icon]) VALUES (N'00000000-0000-0000-0000-000000000002', N'Shopping Item', NULL, NULL)
INSERT [dbo].[ItemTypes] ([ID], [Name], [UserID], [Icon]) VALUES (N'00000000-0000-0000-0000-000000000003', N'Freeform Item', NULL, NULL)
INSERT [dbo].[ItemTypes] ([ID], [Name], [UserID], [Icon]) VALUES (N'00000000-0000-0000-0000-000000000004', N'Contact', NULL, N'contact.png')
GO

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

/****** Object:  Table [dbo].[Fields]    Script Date: 01/18/2012 11:19:55 ******/
/* todo */
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000011', N'Name', 1, N'00000000-0000-0000-0000-000000000001', N'Name', N'String', 1, 1)
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000012', N'Description', 2, N'00000000-0000-0000-0000-000000000001', N'Description', N'String', 1, 2)
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000013', N'PriorityID', 3, N'00000000-0000-0000-0000-000000000001', N'Priority', N'Priority', 1, 3)
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000014', N'DueDate', 4, N'00000000-0000-0000-0000-000000000001', N'Due', N'Date', 1, 4)
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000015', N'ItemTags', 5, N'00000000-0000-0000-0000-000000000001', N'Tags (separated by commas)', N'TagList', 1, 5)
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000016', N'Location', 6, N'00000000-0000-0000-0000-000000000001', N'Location', N'Address', 0, 6)
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000017', N'Phone', 7, N'00000000-0000-0000-0000-000000000001', N'Phone', N'PhoneNumber', 0, 7)
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000018', N'Website', 8, N'00000000-0000-0000-0000-000000000001', N'Website', N'Website', 0, 8)
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000019', N'Email', 9, N'00000000-0000-0000-0000-000000000001', N'Email', N'Email', 0, 9)
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-00000000001a', N'Complete', 10, N'00000000-0000-0000-0000-000000000001', N'Complete', N'Boolean', 0, 11)
/* shopping item */
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000021', N'Name', 1, N'00000000-0000-0000-0000-000000000002', N'Name', N'String', 1, 1)
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000022', N'Complete', 10, N'00000000-0000-0000-0000-000000000002', N'Complete', N'Boolean', 0, 2)
/* freeform item */
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000031', N'Name', 1, N'00000000-0000-0000-0000-000000000003', N'Name', N'String', 1, 1)
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000032', N'Description', 11, N'00000000-0000-0000-0000-000000000003', N'Details', N'TextBox', 1, 2)
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000033', N'PriorityID', 3, N'00000000-0000-0000-0000-000000000003', N'Priority', N'Priority', 0, 3)
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000034', N'DueDate', 4, N'00000000-0000-0000-0000-000000000003', N'Due', N'Date', 0, 4)
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000035', N'ItemTags', 5, N'00000000-0000-0000-0000-000000000003', N'Tags (separated by commas)', N'TagList', 0, 5)
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000036', N'Location', 6, N'00000000-0000-0000-0000-000000000003', N'Location', N'Address', 0, 6)
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000037', N'Phone', 7, N'00000000-0000-0000-0000-000000000003', N'Phone', N'PhoneNumber', 0, 7)
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000038', N'Website', 8, N'00000000-0000-0000-0000-000000000003', N'Website', N'Website', 0, 8)
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000039', N'Email', 9, N'00000000-0000-0000-0000-000000000003', N'Email', N'Email', 0, 9)
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-00000000003a', N'Complete', 10, N'00000000-0000-0000-0000-000000000003', N'Complete', N'Boolean', 0, 11)
/* contact */
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000041', N'Name', 1, N'00000000-0000-0000-0000-000000000004', N'Name', N'String', 1, 1)
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000042', N'Phone', 7, N'00000000-0000-0000-0000-000000000004', N'Phone', N'PhoneNumber', 1, 2)
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000043', N'Email', 9, N'00000000-0000-0000-0000-000000000004', N'Email', N'Email', 1, 3)
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000044', N'Address', 6, N'00000000-0000-0000-0000-000000000004', N'Address', N'Address', 0, 4)
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000045', N'Birthday', 4, N'00000000-0000-0000-0000-000000000004', N'Birthday', N'Date', 0, 5)
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000046', N'ItemTags', 5, N'00000000-0000-0000-0000-000000000004', N'Tags (separated by commas)', N'TagList', 0, 6)
GO

/****** Object:  Table [dbo].[Permissions]    Script Date: 01/18/2012 11:19:55 ******/
SET IDENTITY_INSERT [dbo].[Permissions] ON
GO
INSERT [dbo].[Permissions] ([PermissionID], [Name]) VALUES (1, N'See')
INSERT [dbo].[Permissions] ([PermissionID], [Name]) VALUES (2, N'Change')
INSERT [dbo].[Permissions] ([PermissionID], [Name]) VALUES (3, N'Full')
SET IDENTITY_INSERT [dbo].[Permissions] OFF
GO

/****** Object:  Table [dbo].[Priorities]    Script Date: 01/18/2012 11:19:55 ******/
SET IDENTITY_INSERT [dbo].[Priorities] ON
INSERT [dbo].[Priorities] ([PriorityID], [Name], [Color]) VALUES (0, N'Low', N'Green')
INSERT [dbo].[Priorities] ([PriorityID], [Name], [Color]) VALUES (1, N'Normal', N'White')
INSERT [dbo].[Priorities] ([PriorityID], [Name], [Color]) VALUES (2, N'High', N'Red')
SET IDENTITY_INSERT [dbo].[Priorities] OFF
GO
