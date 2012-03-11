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
INSERT [dbo].[ActionTypes] ([ActionTypeID], [ActionName], [DisplayName], [FieldName], [SortOrder]) VALUES (1,  N'Postpone',      N'postpone',       N'DueDate', 1)
INSERT [dbo].[ActionTypes] ([ActionTypeID], [ActionName], [DisplayName], [FieldName], [SortOrder]) VALUES (2,  N'AddToCalendar', N'add reminder',   N'ReminderDate', 2)
INSERT [dbo].[ActionTypes] ([ActionTypeID], [ActionName], [DisplayName], [FieldName], [SortOrder]) VALUES (3,  N'Map',           N'map',            N'Address', 3)
INSERT [dbo].[ActionTypes] ([ActionTypeID], [ActionName], [DisplayName], [FieldName], [SortOrder]) VALUES (4,  N'Call',          N'call cell',      N'Phone', 4)
INSERT [dbo].[ActionTypes] ([ActionTypeID], [ActionName], [DisplayName], [FieldName], [SortOrder]) VALUES (5,  N'Call',          N'call home',      N'HomePhone', 5)
INSERT [dbo].[ActionTypes] ([ActionTypeID], [ActionName], [DisplayName], [FieldName], [SortOrder]) VALUES (6,  N'Call',          N'call work',      N'WorkPhone', 6)
INSERT [dbo].[ActionTypes] ([ActionTypeID], [ActionName], [DisplayName], [FieldName], [SortOrder]) VALUES (7,  N'TextMessage',   N'text',           N'Phone', 7)
INSERT [dbo].[ActionTypes] ([ActionTypeID], [ActionName], [DisplayName], [FieldName], [SortOrder]) VALUES (8,  N'Browse',        N'browse',         N'WebLink', 8)
INSERT [dbo].[ActionTypes] ([ActionTypeID], [ActionName], [DisplayName], [FieldName], [SortOrder]) VALUES (9,  N'SendEmail',     N'email',          N'Email', 9)
INSERT [dbo].[ActionTypes] ([ActionTypeID], [ActionName], [DisplayName], [FieldName], [SortOrder]) VALUES (10, N'Navigate',      N'show contacts',  N'Contacts', 10)
INSERT [dbo].[ActionTypes] ([ActionTypeID], [ActionName], [DisplayName], [FieldName], [SortOrder]) VALUES (11, N'Navigate',      N'show locations', N'Locations', 11)
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

/****** Object:  Table [dbo].[Users]    Script Date: 03/19/2012 11:19:55 ******/
INSERT [dbo].[Users] ([ID], [Name], [Password], [PasswordSalt], [Email], [CreateDate]) VALUES (N'00000000-0000-0000-0000-000000000000', N'System', N'zrc022..', N'salt', N'foo@example.com', N'3/7/2012 3:38:44 PM')
GO

/****** Object:  Table [dbo].[ItemTypes]    Script Date: 01/18/2012 11:19:55 ******/
INSERT [dbo].[ItemTypes] ([ID], [Name], [UserID], [Icon]) VALUES (N'00000000-0000-0000-0000-000000000001', N'Task', NULL, NULL)
INSERT [dbo].[ItemTypes] ([ID], [Name], [UserID], [Icon]) VALUES (N'00000000-0000-0000-0000-000000000002', N'Location', NULL, NULL)
INSERT [dbo].[ItemTypes] ([ID], [Name], [UserID], [Icon]) VALUES (N'00000000-0000-0000-0000-000000000003', N'Contact', NULL, N'contact.png')
INSERT [dbo].[ItemTypes] ([ID], [Name], [UserID], [Icon]) VALUES (N'00000000-0000-0000-0000-000000000004', N'ListItem', NULL, NULL)
INSERT [dbo].[ItemTypes] ([ID], [Name], [UserID], [Icon]) VALUES (N'00000000-0000-0000-0000-000000000005', N'ShoppingItem', NULL, NULL)
INSERT [dbo].[ItemTypes] ([ID], [Name], [UserID], [Icon]) VALUES (N'00000000-0000-0000-0000-000000000006', N'Reference', N'00000000-0000-0000-0000-000000000000', NULL)
INSERT [dbo].[ItemTypes] ([ID], [Name], [UserID], [Icon]) VALUES (N'00000000-0000-0000-0000-000000000007', N'KeyValuePair', N'00000000-0000-0000-0000-000000000000', NULL)
GO

/****** Object:  Table [dbo].[Fields]    Script Date: 01/18/2012 11:19:55 ******/
/* Task */
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000011', N'Name', N'String', N'00000000-0000-0000-0000-000000000001', N'Name', N'Text', 1, 1)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000012', N'Priority', N'Integer', N'00000000-0000-0000-0000-000000000001', N'Priority', N'Priority', 1, 2)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000013', N'DueDate', N'DateTime', N'00000000-0000-0000-0000-000000000001', N'Due', N'DatePicker', 1, 3)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000014', N'ReminderDate', N'DateTime', N'00000000-0000-0000-0000-000000000001', N'Reminder', N'DatePicker', 1, 4)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000015', N'Description', N'String', N'00000000-0000-0000-0000-000000000001', N'Details', N'TextArea', 0, 5)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000016', N'WebLink', N'Url', N'00000000-0000-0000-0000-000000000001', N'Website', N'Link', 0, 5)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000017', N'Locations', N'ItemID', N'00000000-0000-0000-0000-000000000001', N'Location', N'LocationList', 0, 6)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000018', N'Contacts', N'ItemID', N'00000000-0000-0000-0000-000000000001', N'For', N'ContactList', 0, 7)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000019', N'ItemTags', N'TagIDs', N'00000000-0000-0000-0000-000000000001', N'Tags', N'TagList', 0, 8)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-00000000001A', N'Complete', N'Boolean', N'00000000-0000-0000-0000-000000000001', N'Complete', N'Checkbox', 0, 9)
/* Location */
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000021', N'Name', N'String', N'00000000-0000-0000-0000-000000000002', N'Name', N'Text', 1, 1)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000022', N'Address', N'Address', N'00000000-0000-0000-0000-000000000002', N'Address', N'Address', 1, 2)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000023', N'Phone', N'Phone', N'00000000-0000-0000-0000-000000000002', N'Phone', N'Phone', 0, 3)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000024', N'WebLink', N'Url', N'00000000-0000-0000-0000-000000000002', N'Website', N'Link', 0, 4)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000025', N'Email', N'Email', N'00000000-0000-0000-0000-000000000002', N'Email', N'Email', 0, 5)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000026', N'Description', N'String', N'00000000-0000-0000-0000-000000000002', N'Description', N'TextArea', 0, 6)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000027', N'ItemTags', N'TagIDs', N'00000000-0000-0000-0000-000000000002', N'Tags', N'TagList', 0, 7)
/* Contact */
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000031', N'Name', N'String', N'00000000-0000-0000-0000-000000000003', N'Name', N'Text', 1, 1)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000032', N'Email', N'Email', N'00000000-0000-0000-0000-000000000003', N'Email', N'Email', 1, 2)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000033', N'Phone', N'Phone', N'00000000-0000-0000-0000-000000000003', N'Mobile Phone', N'Phone', 1, 3)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000034', N'HomePhone', N'Phone', N'00000000-0000-0000-0000-000000000003', N'Home Phone', N'Phone', 0, 4)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000035', N'WorkPhone', N'Phone', N'00000000-0000-0000-0000-000000000003', N'Work Phone', N'Phone', 0, 5)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000036', N'Locations', N'ItemID', N'00000000-0000-0000-0000-000000000003', N'Address', N'LocationList', 0, 6)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000037', N'Birthday', N'DateTime', N'00000000-0000-0000-0000-000000000003', N'Birthday', N'DatePicker', 0, 7)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000038', N'ItemTags', N'TagIDs', N'00000000-0000-0000-0000-000000000003', N'Tags', N'TagList', 0, 8)
/* ListItem */
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000041', N'Name', N'String', N'00000000-0000-0000-0000-000000000004', N'Name', N'Text', 1, 1)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000042', N'Complete', N'Boolean', N'00000000-0000-0000-0000-000000000004', N'Complete', N'Checkbox', 1, 2)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000043', N'Description', N'String', N'00000000-0000-0000-0000-000000000004', N'Notes', N'TextArea', 0, 3)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000044', N'ItemRef', N'ItemID', N'00000000-0000-0000-0000-000000000004', N'Reference', N'Reference', 0, 4)
/* ShoppingItem */
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000051', N'Name', N'String', N'00000000-0000-0000-0000-000000000005', N'Name', N'Text', 1, 1)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000052', N'Complete', N'Boolean', N'00000000-0000-0000-0000-000000000005', N'Complete', N'Boolean', 1, 2)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000053', N'Amount', N'String', N'00000000-0000-0000-0000-000000000005', N'Quantity', N'String', 0, 3)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000054', N'Cost', N'Currency', N'00000000-0000-0000-0000-000000000005', N'Price', N'Currency', 0, 4)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000055', N'Description', N'String', N'00000000-0000-0000-0000-000000000005', N'Notes', N'TextBox', 0, 5)
/* Reference */
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000061', N'Name', N'String', N'00000000-0000-0000-0000-000000000006', N'Name', N'Text', 1, 1)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000062', N'ItemRef', N'ItemID', N'00000000-0000-0000-0000-000000000006', N'Reference', N'Reference', 1, 2)
/* KeyValuePair */
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000071', N'Name', N'String', N'00000000-0000-0000-0000-000000000007', N'Name', N'Text', 1, 1)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000072', N'Value', N'String', N'00000000-0000-0000-0000-000000000007', N'Value', N'Text', 1, 2)
GO

/****** Object:  Table [dbo].[Permissions]    Script Date: 01/18/2012 11:19:55 ******/
SET IDENTITY_INSERT [dbo].[Permissions] ON
GO
INSERT [dbo].[Permissions] ([PermissionID], [Name]) VALUES (1, N'View')
INSERT [dbo].[Permissions] ([PermissionID], [Name]) VALUES (2, N'Modify')
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
