/****** Object:  Table [dbo].[Emails]    Script Date: 01/20/2012 00:41:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Emails](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[EmailAddress] [nvarchar](50) NOT NULL,
	[Date] [datetime] NOT NULL,
 CONSTRAINT [PK_Emails] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
/****** Object:  Table [dbo].[Colors]    Script Date: 01/20/2012 00:41:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Colors](
	[ColorID] [int] IDENTITY(0,1) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_Colors] PRIMARY KEY CLUSTERED 
(
	[ColorID] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
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
/****** Object:  Table [dbo].[ActionTypes]    Script Date: 01/20/2012 00:41:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ActionTypes](
	[ActionTypeID] [int] IDENTITY(1,1) NOT NULL,
	[ActionName] [nvarchar](50) NOT NULL,
	[DisplayName] [nvarchar](50) NOT NULL,
	[FieldName] [nvarchar](50) NOT NULL,
	[SortOrder] [int] NOT NULL,
 CONSTRAINT [PK__ActionTypes__8131bb7138be687dc8fa] PRIMARY KEY CLUSTERED 
(
	[ActionTypeID] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
SET IDENTITY_INSERT [dbo].[ActionTypes] ON
INSERT [dbo].[ActionTypes] ([ActionTypeID], [ActionName], [DisplayName], [FieldName], [SortOrder]) VALUES (1, N'Postpone', N'postpone', N'DueDate', 1)
INSERT [dbo].[ActionTypes] ([ActionTypeID], [ActionName], [DisplayName], [FieldName], [SortOrder]) VALUES (2, N'AddToCalendar', N'add reminder', N'DueDate', 2)
INSERT [dbo].[ActionTypes] ([ActionTypeID], [ActionName], [DisplayName], [FieldName], [SortOrder]) VALUES (3, N'Map', N'map', N'Location', 3)
INSERT [dbo].[ActionTypes] ([ActionTypeID], [ActionName], [DisplayName], [FieldName], [SortOrder]) VALUES (4, N'Phone', N'call', N'Phone', 4)
INSERT [dbo].[ActionTypes] ([ActionTypeID], [ActionName], [DisplayName], [FieldName], [SortOrder]) VALUES (5, N'TextMessage', N'text', N'Phone', 5)
INSERT [dbo].[ActionTypes] ([ActionTypeID], [ActionName], [DisplayName], [FieldName], [SortOrder]) VALUES (6, N'Browse', N'browse', N'Website', 6)
INSERT [dbo].[ActionTypes] ([ActionTypeID], [ActionName], [DisplayName], [FieldName], [SortOrder]) VALUES (7, N'Email', N'email', N'Email', 7)
SET IDENTITY_INSERT [dbo].[ActionTypes] OFF
/****** Object:  Table [dbo].[FieldTypes]    Script Date: 01/20/2012 00:41:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FieldTypes](
	[FieldTypeID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[DataType] [nvarchar](50) NOT NULL,
	[DisplayName] [nvarchar](50) NULL,
	[DisplayType] [nvarchar](50) NULL,
 CONSTRAINT [PK_FieldTypes] PRIMARY KEY CLUSTERED 
(
	[FieldTypeID] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
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
/****** Object:  Table [dbo].[Priorities]    Script Date: 01/20/2012 00:41:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Priorities](
	[PriorityID] [int] IDENTITY(0,1) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[Color] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK__Priorities__baa34826030f013ebe2f] PRIMARY KEY CLUSTERED 
(
	[PriorityID] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
SET IDENTITY_INSERT [dbo].[Priorities] ON
INSERT [dbo].[Priorities] ([PriorityID], [Name], [Color]) VALUES (0, N'Low', N'Green')
INSERT [dbo].[Priorities] ([PriorityID], [Name], [Color]) VALUES (1, N'Normal', N'White')
INSERT [dbo].[Priorities] ([PriorityID], [Name], [Color]) VALUES (2, N'High', N'Red')
SET IDENTITY_INSERT [dbo].[Priorities] OFF
/****** Object:  Table [dbo].[Permissions]    Script Date: 01/20/2012 00:41:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Permissions](
	[PermissionID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK__Permissions__494d0ba6644b7db16f4c] PRIMARY KEY CLUSTERED 
(
	[PermissionID] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
SET IDENTITY_INSERT [dbo].[Permissions] ON
INSERT [dbo].[Permissions] ([PermissionID], [Name]) VALUES (1, N'See')
INSERT [dbo].[Permissions] ([PermissionID], [Name]) VALUES (2, N'Change')
INSERT [dbo].[Permissions] ([PermissionID], [Name]) VALUES (3, N'Full')
SET IDENTITY_INSERT [dbo].[Permissions] OFF
/****** Object:  Table [dbo].[Users]    Script Date: 01/20/2012 00:41:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Users](
	[ID] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[Password] [nvarchar](50) NOT NULL,
	[Email] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK__Users__df8aeb2f938c26cb4249] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
/****** Object:  Table [dbo].[Tags]    Script Date: 01/20/2012 00:41:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Tags](
	[ID] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[UserID] [uniqueidentifier] NOT NULL,
	[Color] [nvarchar](50) NULL,
 CONSTRAINT [PK__Tags__04e96c592154bcfadf5f] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
/****** Object:  Table [dbo].[Operations]    Script Date: 01/20/2012 00:41:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Operations](
	[ID] [uniqueidentifier] NOT NULL,
	[UserID] [uniqueidentifier] NOT NULL,
	[Username] [nvarchar](50) NOT NULL,
	[EntityID] [uniqueidentifier] NOT NULL,
	[EntityName] [nvarchar](max) NOT NULL,
	[EntityType] [nchar](10) NOT NULL,
	[OperationType] [nchar](6) NOT NULL,
	[Body] [nvarchar](max) NOT NULL,
	[OldBody] [nvarchar](max) NOT NULL,
	[Timestamp] [datetime] NOT NULL,
 CONSTRAINT [PK__Operations__bbe3df4825f0726cda3b] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
/****** Object:  Table [dbo].[ItemTypes]    Script Date: 01/20/2012 00:41:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ItemTypes](
	[ID] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[UserID] [uniqueidentifier] NULL,
	[Icon] [nvarchar](50) NULL,
 CONSTRAINT [PK__ItemTypes__ffb8b1782bf805eb501c] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
INSERT [dbo].[ItemTypes] ([ID], [Name], [UserID], [Icon]) VALUES (N'00000000-0000-0000-0000-000000000001', N'To Do', NULL, NULL)
INSERT [dbo].[ItemTypes] ([ID], [Name], [UserID], [Icon]) VALUES (N'00000000-0000-0000-0000-000000000002', N'Shopping Item', NULL, NULL)
INSERT [dbo].[ItemTypes] ([ID], [Name], [UserID], [Icon]) VALUES (N'00000000-0000-0000-0000-000000000003', N'Freeform Item', NULL, NULL)
INSERT [dbo].[ItemTypes] ([ID], [Name], [UserID], [Icon]) VALUES (N'00000000-0000-0000-0000-000000000004', N'Contact', NULL, N'contact.png')
/****** Object:  Table [dbo].[Folders]    Script Date: 01/20/2012 00:41:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Folders](
	[ID] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](max) NOT NULL,
	[UserID] [uniqueidentifier] NOT NULL,
	[ColorID] [int] NULL,
 CONSTRAINT [PK__Folders__ea28b4d39bbdea2c0744] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
/****** Object:  Table [dbo].[Fields]    Script Date: 01/20/2012 00:41:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Fields](
	[ID] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](50) NULL,
	[FieldTypeID] [int] NOT NULL,
	[ItemTypeID] [uniqueidentifier] NOT NULL,
	[DisplayName] [nvarchar](50) NOT NULL,
	[DisplayType] [nvarchar](50) NOT NULL,
	[IsPrimary] [bit] NOT NULL,
	[SortOrder] [int] NOT NULL,
 CONSTRAINT [PK_Fields] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
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
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000021', N'Name', 1, N'00000000-0000-0000-0000-000000000002', N'Name', N'String', 1, 1)
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000022', N'Complete', 10, N'00000000-0000-0000-0000-000000000002', N'Complete', N'Boolean', 0, 2)
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
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000041', N'Name', 1, N'00000000-0000-0000-0000-000000000004', N'Name', N'String', 1, 1)
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000042', N'Phone', 7, N'00000000-0000-0000-0000-000000000004', N'Phone', N'PhoneNumber', 1, 2)
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000043', N'Email', 9, N'00000000-0000-0000-0000-000000000004', N'Email', N'Email', 1, 3)
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000044', N'Address', 6, N'00000000-0000-0000-0000-000000000004', N'Address', N'Address', 0, 4)
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000045', N'Birthday', 4, N'00000000-0000-0000-0000-000000000004', N'Birthday', N'Date', 0, 5)
INSERT [dbo].[Fields] ([ID], [Name], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'00000000-0000-0000-0000-000000000046', N'ItemTags', 5, N'00000000-0000-0000-0000-000000000004', N'Tags (separated by commas)', N'TagList', 0, 6)
/****** Object:  Table [dbo].[Items]    Script Date: 01/20/2012 00:41:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Items](
	[ID] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](max) NOT NULL,
	[ParentID] [uniqueidentifier] NULL,
	[ItemTypeID] [uniqueidentifier] NOT NULL,
	[FolderID] [uniqueidentifier] NOT NULL,
	[UserID] [uniqueidentifier] NOT NULL,
	[IsList] [bit] NOT NULL,
	[Created] [datetime] NOT NULL,
	[LastModified] [datetime] NOT NULL,
 CONSTRAINT [PK__Items__3cf0ef906c281f20edf5] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
/****** Object:  Table [dbo].[FolderUsers]    Script Date: 01/20/2012 00:41:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FolderUsers](
	[ID] [uniqueidentifier] NOT NULL,
	[FolderID] [uniqueidentifier] NOT NULL,
	[UserID] [uniqueidentifier] NOT NULL,
	[PermissionID] [int] NOT NULL,
 CONSTRAINT [PK__FolderUsers__3aea98c674d4db93641c] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
/****** Object:  Table [dbo].[ItemTags]    Script Date: 01/20/2012 00:41:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ItemTags](
	[ID] [uniqueidentifier] NOT NULL,
	[ItemID] [uniqueidentifier] NOT NULL,
	[TagID] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK__ItemTags__28b92c0eab2b0efd1cd7] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
/****** Object:  Table [dbo].[FieldValues]    Script Date: 01/20/2012 00:41:27 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FieldValues](
	[ID] [uniqueidentifier] NOT NULL,
	[FieldID] [uniqueidentifier] NOT NULL,
	[ItemID] [uniqueidentifier] NOT NULL,
	[Value] [nvarchar](max) NULL,
 CONSTRAINT [PK__FieldValues__feb7cad16bd2d32be10e] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
/****** Object:  Default [DF__Fields__785fadd168438d004ffd]    Script Date: 01/20/2012 00:41:27 ******/
ALTER TABLE [dbo].[Fields] ADD  CONSTRAINT [DF__Fields__785fadd168438d004ffd]  DEFAULT (newid()) FOR [ID]
GO
/****** Object:  Default [DF__FieldValues__41289196aecbdddf92a8]    Script Date: 01/20/2012 00:41:27 ******/
ALTER TABLE [dbo].[FieldValues] ADD  CONSTRAINT [DF__FieldValues__41289196aecbdddf92a8]  DEFAULT (newid()) FOR [ID]
GO
/****** Object:  Default [DF__Folders__eb137fbd38356784ee7c]    Script Date: 01/20/2012 00:41:27 ******/
ALTER TABLE [dbo].[Folders] ADD  CONSTRAINT [DF__Folders__eb137fbd38356784ee7c]  DEFAULT (newid()) FOR [ID]
GO
/****** Object:  Default [DF__FolderUsers__6fdccdcf8fd41595c4a9]    Script Date: 01/20/2012 00:41:27 ******/
ALTER TABLE [dbo].[FolderUsers] ADD  CONSTRAINT [DF__FolderUsers__6fdccdcf8fd41595c4a9]  DEFAULT (newid()) FOR [ID]
GO
/****** Object:  Default [DF__Items__24c70eed1ab03b0faf44]    Script Date: 01/20/2012 00:41:27 ******/
ALTER TABLE [dbo].[Items] ADD  CONSTRAINT [DF__Items__24c70eed1ab03b0faf44]  DEFAULT (newid()) FOR [ID]
GO
/****** Object:  Default [DF__ItemTags__c605092eae8c7c03392d]    Script Date: 01/20/2012 00:41:27 ******/
ALTER TABLE [dbo].[ItemTags] ADD  CONSTRAINT [DF__ItemTags__c605092eae8c7c03392d]  DEFAULT (newid()) FOR [ID]
GO
/****** Object:  Default [DF__ItemTypes__88274294eacae2cf7790]    Script Date: 01/20/2012 00:41:27 ******/
ALTER TABLE [dbo].[ItemTypes] ADD  CONSTRAINT [DF__ItemTypes__88274294eacae2cf7790]  DEFAULT (newid()) FOR [ID]
GO
/****** Object:  Default [DF__Operations__4b5be82bc40a1207bac9]    Script Date: 01/20/2012 00:41:27 ******/
ALTER TABLE [dbo].[Operations] ADD  CONSTRAINT [DF__Operations__4b5be82bc40a1207bac9]  DEFAULT (newid()) FOR [ID]
GO
/****** Object:  Default [DF__Tags__639816aea7a0d7ff95c8]    Script Date: 01/20/2012 00:41:27 ******/
ALTER TABLE [dbo].[Tags] ADD  CONSTRAINT [DF__Tags__639816aea7a0d7ff95c8]  DEFAULT (newid()) FOR [ID]
GO
/****** Object:  Default [DF__Tags__306ed5e8cc0a2035296d]    Script Date: 01/20/2012 00:41:27 ******/
ALTER TABLE [dbo].[Tags] ADD  CONSTRAINT [DF__Tags__306ed5e8cc0a2035296d]  DEFAULT (N'White') FOR [Color]
GO
/****** Object:  Default [DF__Users__3d4dad1a62f1154b3626]    Script Date: 01/20/2012 00:41:27 ******/
ALTER TABLE [dbo].[Users] ADD  CONSTRAINT [DF__Users__3d4dad1a62f1154b3626]  DEFAULT (newid()) FOR [ID]
GO
/****** Object:  ForeignKey [FK__Fields__05ddf1426d59f46c7811]    Script Date: 01/20/2012 00:41:27 ******/
ALTER TABLE [dbo].[Fields]  WITH CHECK ADD  CONSTRAINT [FK__Fields__05ddf1426d59f46c7811] FOREIGN KEY([FieldTypeID])
REFERENCES [dbo].[FieldTypes] ([FieldTypeID])
GO
ALTER TABLE [dbo].[Fields] CHECK CONSTRAINT [FK__Fields__05ddf1426d59f46c7811]
GO
/****** Object:  ForeignKey [FK__Fields__59402725ffb93062d1bc]    Script Date: 01/20/2012 00:41:27 ******/
ALTER TABLE [dbo].[Fields]  WITH CHECK ADD  CONSTRAINT [FK__Fields__59402725ffb93062d1bc] FOREIGN KEY([ItemTypeID])
REFERENCES [dbo].[ItemTypes] ([ID])
GO
ALTER TABLE [dbo].[Fields] CHECK CONSTRAINT [FK__Fields__59402725ffb93062d1bc]
GO
/****** Object:  ForeignKey [FK__FieldValues__22348e7d1e0ac9c5a059]    Script Date: 01/20/2012 00:41:27 ******/
ALTER TABLE [dbo].[FieldValues]  WITH CHECK ADD  CONSTRAINT [FK__FieldValues__22348e7d1e0ac9c5a059] FOREIGN KEY([FieldID])
REFERENCES [dbo].[Fields] ([ID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[FieldValues] CHECK CONSTRAINT [FK__FieldValues__22348e7d1e0ac9c5a059]
GO
/****** Object:  ForeignKey [FK__FieldValues__9c2277775d91f07d99a5]    Script Date: 01/20/2012 00:41:27 ******/
ALTER TABLE [dbo].[FieldValues]  WITH CHECK ADD  CONSTRAINT [FK__FieldValues__9c2277775d91f07d99a5] FOREIGN KEY([ItemID])
REFERENCES [dbo].[Items] ([ID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[FieldValues] CHECK CONSTRAINT [FK__FieldValues__9c2277775d91f07d99a5]
GO
/****** Object:  ForeignKey [FK__Folders__c756ca63a1115d0c9eb0]    Script Date: 01/20/2012 00:41:27 ******/
ALTER TABLE [dbo].[Folders]  WITH CHECK ADD  CONSTRAINT [FK__Folders__c756ca63a1115d0c9eb0] FOREIGN KEY([UserID])
REFERENCES [dbo].[Users] ([ID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Folders] CHECK CONSTRAINT [FK__Folders__c756ca63a1115d0c9eb0]
GO
/****** Object:  ForeignKey [FK__Folders__da629744388667040d5e]    Script Date: 01/20/2012 00:41:27 ******/
ALTER TABLE [dbo].[Folders]  WITH CHECK ADD  CONSTRAINT [FK__Folders__da629744388667040d5e] FOREIGN KEY([ColorID])
REFERENCES [dbo].[Colors] ([ColorID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Folders] CHECK CONSTRAINT [FK__Folders__da629744388667040d5e]
GO
/****** Object:  ForeignKey [FK__FolderUsers__77d873a1c2ee78182513]    Script Date: 01/20/2012 00:41:27 ******/
ALTER TABLE [dbo].[FolderUsers]  WITH CHECK ADD  CONSTRAINT [FK__FolderUsers__77d873a1c2ee78182513] FOREIGN KEY([UserID])
REFERENCES [dbo].[Users] ([ID])
GO
ALTER TABLE [dbo].[FolderUsers] CHECK CONSTRAINT [FK__FolderUsers__77d873a1c2ee78182513]
GO
/****** Object:  ForeignKey [FK__FolderUsers__b7dd1e9550773cfbe2df]    Script Date: 01/20/2012 00:41:27 ******/
ALTER TABLE [dbo].[FolderUsers]  WITH CHECK ADD  CONSTRAINT [FK__FolderUsers__b7dd1e9550773cfbe2df] FOREIGN KEY([FolderID])
REFERENCES [dbo].[Folders] ([ID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[FolderUsers] CHECK CONSTRAINT [FK__FolderUsers__b7dd1e9550773cfbe2df]
GO
/****** Object:  ForeignKey [FK__FolderUsers__d368afaffb7b777a56bf]    Script Date: 01/20/2012 00:41:27 ******/
ALTER TABLE [dbo].[FolderUsers]  WITH CHECK ADD  CONSTRAINT [FK__FolderUsers__d368afaffb7b777a56bf] FOREIGN KEY([PermissionID])
REFERENCES [dbo].[Permissions] ([PermissionID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[FolderUsers] CHECK CONSTRAINT [FK__FolderUsers__d368afaffb7b777a56bf]
GO
/****** Object:  ForeignKey [FK__Items__0daf05a74115c05c36d8]    Script Date: 01/20/2012 00:41:27 ******/
ALTER TABLE [dbo].[Items]  WITH CHECK ADD  CONSTRAINT [FK__Items__0daf05a74115c05c36d8] FOREIGN KEY([UserID])
REFERENCES [dbo].[Users] ([ID])
GO
ALTER TABLE [dbo].[Items] CHECK CONSTRAINT [FK__Items__0daf05a74115c05c36d8]
GO
/****** Object:  ForeignKey [FK__Items__1cb7ed8d8fa7887e017c]    Script Date: 01/20/2012 00:41:27 ******/
ALTER TABLE [dbo].[Items]  WITH CHECK ADD  CONSTRAINT [FK__Items__1cb7ed8d8fa7887e017c] FOREIGN KEY([FolderID])
REFERENCES [dbo].[Folders] ([ID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Items] CHECK CONSTRAINT [FK__Items__1cb7ed8d8fa7887e017c]
GO
/****** Object:  ForeignKey [FK__Items__4304820004d7977b4092]    Script Date: 01/20/2012 00:41:27 ******/
ALTER TABLE [dbo].[Items]  WITH CHECK ADD  CONSTRAINT [FK__Items__4304820004d7977b4092] FOREIGN KEY([ItemTypeID])
REFERENCES [dbo].[ItemTypes] ([ID])
GO
ALTER TABLE [dbo].[Items] CHECK CONSTRAINT [FK__Items__4304820004d7977b4092]
GO
/****** Object:  ForeignKey [FK__Items__99dcb5a51054c656a29f]    Script Date: 01/20/2012 00:41:27 ******/
ALTER TABLE [dbo].[Items]  WITH CHECK ADD  CONSTRAINT [FK__Items__99dcb5a51054c656a29f] FOREIGN KEY([ParentID])
REFERENCES [dbo].[Items] ([ID])
GO
ALTER TABLE [dbo].[Items] CHECK CONSTRAINT [FK__Items__99dcb5a51054c656a29f]
GO
/****** Object:  ForeignKey [FK__ItemTags__4924f9d20ea1fcb53f3a]    Script Date: 01/20/2012 00:41:27 ******/
ALTER TABLE [dbo].[ItemTags]  WITH CHECK ADD  CONSTRAINT [FK__ItemTags__4924f9d20ea1fcb53f3a] FOREIGN KEY([TagID])
REFERENCES [dbo].[Tags] ([ID])
GO
ALTER TABLE [dbo].[ItemTags] CHECK CONSTRAINT [FK__ItemTags__4924f9d20ea1fcb53f3a]
GO
/****** Object:  ForeignKey [FK__ItemTags__e389770dc0efaa00f1bb]    Script Date: 01/20/2012 00:41:27 ******/
ALTER TABLE [dbo].[ItemTags]  WITH CHECK ADD  CONSTRAINT [FK__ItemTags__e389770dc0efaa00f1bb] FOREIGN KEY([ItemID])
REFERENCES [dbo].[Items] ([ID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ItemTags] CHECK CONSTRAINT [FK__ItemTags__e389770dc0efaa00f1bb]
GO
/****** Object:  ForeignKey [FK__ItemTypes__ae3295edd33f99c9171a]    Script Date: 01/20/2012 00:41:27 ******/
ALTER TABLE [dbo].[ItemTypes]  WITH CHECK ADD  CONSTRAINT [FK__ItemTypes__ae3295edd33f99c9171a] FOREIGN KEY([UserID])
REFERENCES [dbo].[Users] ([ID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ItemTypes] CHECK CONSTRAINT [FK__ItemTypes__ae3295edd33f99c9171a]
GO
/****** Object:  ForeignKey [FK__Operations__e07450e2eec0ab9959e2]    Script Date: 01/20/2012 00:41:27 ******/
ALTER TABLE [dbo].[Operations]  WITH CHECK ADD  CONSTRAINT [FK__Operations__e07450e2eec0ab9959e2] FOREIGN KEY([UserID])
REFERENCES [dbo].[Users] ([ID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Operations] CHECK CONSTRAINT [FK__Operations__e07450e2eec0ab9959e2]
GO
/****** Object:  ForeignKey [FK__Tags__049dfffb78dc293a1a57]    Script Date: 01/20/2012 00:41:27 ******/
ALTER TABLE [dbo].[Tags]  WITH CHECK ADD  CONSTRAINT [FK__Tags__049dfffb78dc293a1a57] FOREIGN KEY([UserID])
REFERENCES [dbo].[Users] ([ID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Tags] CHECK CONSTRAINT [FK__Tags__049dfffb78dc293a1a57]
GO
