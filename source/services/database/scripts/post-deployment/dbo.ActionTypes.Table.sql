/****** Object:  Table [dbo].[ActionTypes]    ******/
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

