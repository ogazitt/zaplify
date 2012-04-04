﻿/****** Object:  Table [dbo].[Fields]    ******/
/* Task */
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000011', N'Name', N'String', N'00000000-0000-0000-0000-000000000001', N'Name', N'Text', 1, 1)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000012', N'Priority', N'Integer', N'00000000-0000-0000-0000-000000000001', N'Priority', N'Priority', 1, 2)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000013', N'DueDate', N'DateTime', N'00000000-0000-0000-0000-000000000001', N'Due', N'DateTimePicker', 1, 3)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000014', N'ReminderDate', N'DateTime', N'00000000-0000-0000-0000-000000000001', N'Reminder', N'DateTimePicker', 1, 4)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000015', N'Description', N'String', N'00000000-0000-0000-0000-000000000001', N'Details', N'TextArea', 0, 5)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000016', N'Contacts', N'ItemID', N'00000000-0000-0000-0000-000000000001', N'Contacts', N'ContactList', 0, 6)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000017', N'Locations', N'ItemID', N'00000000-0000-0000-0000-000000000001', N'Locations', N'LocationList', 0, 7)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000018', N'WebLinks', N'ItemID', N'00000000-0000-0000-0000-000000000001', N'Web Links', N'UrlList', 0, 8)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000019', N'ItemTags', N'TagIDs', N'00000000-0000-0000-0000-000000000001', N'Tags', N'TagList', 0, 9)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-00000000001A', N'Complete', N'Boolean', N'00000000-0000-0000-0000-000000000001', N'Complete', N'Checkbox', 0, 10)
/* Location */
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000021', N'Name', N'String', N'00000000-0000-0000-0000-000000000002', N'Name', N'Text', 1, 1)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000022', N'Address', N'Address', N'00000000-0000-0000-0000-000000000002', N'Address', N'Address', 1, 2)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000023', N'Phone', N'Phone', N'00000000-0000-0000-0000-000000000002', N'Phone', N'Phone', 1, 3)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000024', N'Email', N'Email', N'00000000-0000-0000-0000-000000000002', N'Email', N'Email', 1, 4)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000025', N'WebLinks', N'ItemID', N'00000000-0000-0000-0000-000000000002', N'Web Links', N'UrlList', 0, 5)
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
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000039', N'FacebookID', N'String', N'00000000-0000-0000-0000-000000000003', N'Facebook ID', N'Hidden', 0, 9)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-00000000003A', N'Sources', N'String', N'00000000-0000-0000-0000-000000000003', N'Sources', N'Hidden', 0, 10)
/* ListItem */
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000041', N'Name', N'String', N'00000000-0000-0000-0000-000000000004', N'Name', N'Text', 1, 1)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000042', N'Complete', N'Boolean', N'00000000-0000-0000-0000-000000000004', N'Complete', N'Checkbox', 1, 2)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000043', N'Description', N'String', N'00000000-0000-0000-0000-000000000004', N'Notes', N'TextArea', 0, 3)
/* ShoppingItem */
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000051', N'Name', N'String', N'00000000-0000-0000-0000-000000000005', N'Name', N'Text', 1, 1)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000052', N'Complete', N'Boolean', N'00000000-0000-0000-0000-000000000005', N'Complete', N'Checkbox', 1, 2)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000053', N'Amount', N'String', N'00000000-0000-0000-0000-000000000005', N'Quantity', N'String', 0, 3)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000054', N'Cost', N'Currency', N'00000000-0000-0000-0000-000000000005', N'Price', N'Currency', 0, 4)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000055', N'Description', N'String', N'00000000-0000-0000-0000-000000000005', N'Notes', N'TextArea', 0, 5)
/* Reference */
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000061', N'Name', N'String', N'00000000-0000-0000-0000-000000000006', N'Name', N'Text', 1, 1)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000062', N'ItemRef', N'ItemID', N'00000000-0000-0000-0000-000000000006', N'Reference', N'Reference', 1, 2)
/* NameValue */
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000071', N'Name', N'String', N'00000000-0000-0000-0000-000000000007', N'Name', N'Text', 1, 1)
INSERT [dbo].[Fields] ([ID], [Name], [FieldType], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000072', N'Value', N'String', N'00000000-0000-0000-0000-000000000007', N'Value', N'Text', 1, 2)
