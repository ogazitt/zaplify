USE [Zaplify]
GO
/****** Object:  Table [dbo].[Emails]    Script Date: 01/16/2012 17:48:37 ******/
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
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Colors]    Script Date: 01/16/2012 17:48:37 ******/
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
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
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
/****** Object:  Table [dbo].[ActionTypes]    Script Date: 01/16/2012 17:48:37 ******/
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
 CONSTRAINT [PK_Actions] PRIMARY KEY CLUSTERED 
(
	[ActionTypeID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET IDENTITY_INSERT [dbo].[ActionTypes] ON
INSERT [dbo].[ActionTypes] ([ActionTypeID], [ActionName], [DisplayName], [FieldName], [SortOrder]) VALUES (1, N'Navigate', N'navigate', N'LinkedTaskListID', 1)
INSERT [dbo].[ActionTypes] ([ActionTypeID], [ActionName], [DisplayName], [FieldName], [SortOrder]) VALUES (2, N'Postpone', N'postpone', N'Due', 2)
INSERT [dbo].[ActionTypes] ([ActionTypeID], [ActionName], [DisplayName], [FieldName], [SortOrder]) VALUES (3, N'AddToCalendar', N'add reminder', N'Due', 3)
INSERT [dbo].[ActionTypes] ([ActionTypeID], [ActionName], [DisplayName], [FieldName], [SortOrder]) VALUES (4, N'Map', N'map', N'Location', 4)
INSERT [dbo].[ActionTypes] ([ActionTypeID], [ActionName], [DisplayName], [FieldName], [SortOrder]) VALUES (5, N'Phone', N'call', N'Phone', 5)
INSERT [dbo].[ActionTypes] ([ActionTypeID], [ActionName], [DisplayName], [FieldName], [SortOrder]) VALUES (6, N'TextMessage', N'text', N'Phone', 6)
INSERT [dbo].[ActionTypes] ([ActionTypeID], [ActionName], [DisplayName], [FieldName], [SortOrder]) VALUES (7, N'Browse', N'browse', N'Website', 7)
INSERT [dbo].[ActionTypes] ([ActionTypeID], [ActionName], [DisplayName], [FieldName], [SortOrder]) VALUES (8, N'Email', N'email', N'Email', 8)
SET IDENTITY_INSERT [dbo].[ActionTypes] OFF
/****** Object:  Table [dbo].[FieldTypes]    Script Date: 01/16/2012 17:48:37 ******/
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
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET IDENTITY_INSERT [dbo].[FieldTypes] ON
INSERT [dbo].[FieldTypes] ([FieldTypeID], [Name], [DataType], [DisplayName], [DisplayType]) VALUES (1, N'Name', N'String', N'Name', N'String')
INSERT [dbo].[FieldTypes] ([FieldTypeID], [Name], [DataType], [DisplayName], [DisplayType]) VALUES (2, N'Description', N'String', N'Description', N'String')
INSERT [dbo].[FieldTypes] ([FieldTypeID], [Name], [DataType], [DisplayName], [DisplayType]) VALUES (3, N'PriorityID', N'Int32', N'Priority', N'Priority')
INSERT [dbo].[FieldTypes] ([FieldTypeID], [Name], [DataType], [DisplayName], [DisplayType]) VALUES (4, N'Due', N'DateTime', N'Due', N'Date')
INSERT [dbo].[FieldTypes] ([FieldTypeID], [Name], [DataType], [DisplayName], [DisplayType]) VALUES (5, N'TaskTags', N'List''TaskTag', N'Tags (separated by commas)', N'TagList')
INSERT [dbo].[FieldTypes] ([FieldTypeID], [Name], [DataType], [DisplayName], [DisplayType]) VALUES (6, N'Location', N'String', N'Location', N'Address')
INSERT [dbo].[FieldTypes] ([FieldTypeID], [Name], [DataType], [DisplayName], [DisplayType]) VALUES (7, N'Phone', N'String', N'Phone', N'PhoneNumber')
INSERT [dbo].[FieldTypes] ([FieldTypeID], [Name], [DataType], [DisplayName], [DisplayType]) VALUES (8, N'Website', N'String', N'Website', N'Website')
INSERT [dbo].[FieldTypes] ([FieldTypeID], [Name], [DataType], [DisplayName], [DisplayType]) VALUES (9, N'Email', N'String', N'Email', N'Email')
INSERT [dbo].[FieldTypes] ([FieldTypeID], [Name], [DataType], [DisplayName], [DisplayType]) VALUES (10, N'Complete', N'Boolean', N'Complete', N'Boolean')
INSERT [dbo].[FieldTypes] ([FieldTypeID], [Name], [DataType], [DisplayName], [DisplayType]) VALUES (11, N'Description', N'String', N'Details', N'TextBox')
INSERT [dbo].[FieldTypes] ([FieldTypeID], [Name], [DataType], [DisplayName], [DisplayType]) VALUES (12, N'LinkedTaskListID', N'ListPointer', N'Link to another list', N'ListPointer')
SET IDENTITY_INSERT [dbo].[FieldTypes] OFF
/****** Object:  Table [dbo].[Priorities]    Script Date: 01/16/2012 17:48:37 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Priorities](
	[PriorityID] [int] IDENTITY(0,1) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[Color] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_Priorities] PRIMARY KEY CLUSTERED 
(
	[PriorityID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET IDENTITY_INSERT [dbo].[Priorities] ON
INSERT [dbo].[Priorities] ([PriorityID], [Name], [Color]) VALUES (0, N'Low', N'Green')
INSERT [dbo].[Priorities] ([PriorityID], [Name], [Color]) VALUES (1, N'Normal', N'White')
INSERT [dbo].[Priorities] ([PriorityID], [Name], [Color]) VALUES (2, N'High', N'Red')
SET IDENTITY_INSERT [dbo].[Priorities] OFF
/****** Object:  Table [dbo].[Permissions]    Script Date: 01/16/2012 17:48:37 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Permissions](
	[PermissionID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_Permissions] PRIMARY KEY CLUSTERED 
(
	[PermissionID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET IDENTITY_INSERT [dbo].[Permissions] ON
INSERT [dbo].[Permissions] ([PermissionID], [Name]) VALUES (1, N'See')
INSERT [dbo].[Permissions] ([PermissionID], [Name]) VALUES (2, N'Change')
INSERT [dbo].[Permissions] ([PermissionID], [Name]) VALUES (3, N'Full')
SET IDENTITY_INSERT [dbo].[Permissions] OFF
/****** Object:  Table [dbo].[Operations]    Script Date: 01/16/2012 17:48:37 ******/
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
 CONSTRAINT [PK_Operations] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Users]    Script Date: 01/16/2012 17:48:37 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Users](
	[ID] [uniqueidentifier] ROWGUIDCOL  NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[Password] [nvarchar](50) NOT NULL,
	[Email] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Tags]    Script Date: 01/16/2012 17:48:37 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Tags](
	[ID] [uniqueidentifier] ROWGUIDCOL  NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[UserID] [uniqueidentifier] NOT NULL,
	[Color] [nvarchar](50) NULL,
 CONSTRAINT [PK_Tags] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ItemTypes]    Script Date: 01/16/2012 17:48:37 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ItemTypes](
	[ID] [uniqueidentifier] ROWGUIDCOL  NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[UserID] [uniqueidentifier] NULL,
 CONSTRAINT [PK_ListTypes] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
INSERT [dbo].[ItemTypes] ([ID], [Name], [UserID]) VALUES (N'14cda248-4116-4e51-ac13-00096b43418c', N'To Do', NULL)
INSERT [dbo].[ItemTypes] ([ID], [Name], [UserID]) VALUES (N'dc1c6243-e510-4297-9df8-75babd237fbe', N'Freeform Item', NULL)
INSERT [dbo].[ItemTypes] ([ID], [Name], [UserID]) VALUES (N'1788a0c4-96e8-4b95-911a-75e1519d7259', N'Shopping Item', NULL)
/****** Object:  Table [dbo].[Folders]    Script Date: 01/16/2012 17:48:37 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Folders](
	[ID] [uniqueidentifier] ROWGUIDCOL  NOT NULL,
	[Name] [nvarchar](max) NOT NULL,
	[UserID] [uniqueidentifier] NOT NULL,
	[ColorID] [int] NULL,
 CONSTRAINT [PK_Groups] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Fields]    Script Date: 01/16/2012 17:48:37 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Fields](
	[ID] [uniqueidentifier] ROWGUIDCOL  NOT NULL,
	[FieldTypeID] [int] NOT NULL,
	[ItemTypeID] [uniqueidentifier] NOT NULL,
	[DisplayName] [nvarchar](50) NOT NULL,
	[DisplayType] [nvarchar](50) NOT NULL,
	[IsPrimary] [bit] NOT NULL,
	[SortOrder] [int] NOT NULL,
 CONSTRAINT [PK_Fields] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
INSERT [dbo].[Fields] ([ID], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'f5391480-1675-4d5c-9f4b-0887227afda5', 6, N'14cda248-4116-4e51-ac13-00096b43418c', N'Location', N'Address', 0, 6)
INSERT [dbo].[Fields] ([ID], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'82957b93-67d9-4e4a-a522-08d18b4b5a1f', 8, N'14cda248-4116-4e51-ac13-00096b43418c', N'Website', N'Website', 0, 8)
INSERT [dbo].[Fields] ([ID], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'1448b7e7-f876-46ec-8e5b-0b9a1de7ea74', 12, N'14cda248-4116-4e51-ac13-00096b43418c', N'Link to another list', N'ListPointer', 0, 10)
INSERT [dbo].[Fields] ([ID], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'2848af68-26f7-4abb-8b9e-1da74ee4ec73', 4, N'dc1c6243-e510-4297-9df8-75babd237fbe', N'Due', N'Date', 0, 4)
INSERT [dbo].[Fields] ([ID], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'4e304cca-561f-4cb3-889b-1f5d022c4364', 9, N'dc1c6243-e510-4297-9df8-75babd237fbe', N'Email', N'Email', 0, 9)
INSERT [dbo].[Fields] ([ID], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'fe0cfc57-0a1c-4e3e-add3-225e2c062de0', 10, N'dc1c6243-e510-4297-9df8-75babd237fbe', N'Complete', N'Boolean', 0, 11)
INSERT [dbo].[Fields] ([ID], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'7ffd95db-fe46-49b4-b5ee-2863938cd687', 11, N'dc1c6243-e510-4297-9df8-75babd237fbe', N'Details', N'TextBox', 1, 2)
INSERT [dbo].[Fields] ([ID], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'4054f093-3f7f-4894-a2c2-5924098dbb29', 6, N'dc1c6243-e510-4297-9df8-75babd237fbe', N'Location', N'Address', 0, 6)
INSERT [dbo].[Fields] ([ID], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'5f33c018-f0ed-4c8d-af96-5b5c4b78c843', 4, N'14cda248-4116-4e51-ac13-00096b43418c', N'Due', N'Date', 1, 4)
INSERT [dbo].[Fields] ([ID], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'da356e6e-a484-47a3-9c95-7618bcbb39ef', 7, N'14cda248-4116-4e51-ac13-00096b43418c', N'Phone', N'PhoneNumber', 0, 7)
INSERT [dbo].[Fields] ([ID], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'1c01e1b0-c14a-4ce9-81b9-868a13aae045', 1, N'dc1c6243-e510-4297-9df8-75babd237fbe', N'Name', N'String', 1, 1)
INSERT [dbo].[Fields] ([ID], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'9ebb9cba-277a-4462-b205-959520eb88c5', 5, N'dc1c6243-e510-4297-9df8-75babd237fbe', N'Tags (separated by commas)', N'TagList', 0, 5)
INSERT [dbo].[Fields] ([ID], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'ea7a11ad-e842-40ea-8a50-987427e69845', 5, N'14cda248-4116-4e51-ac13-00096b43418c', N'Tags (separated by commas)', N'TagList', 1, 5)
INSERT [dbo].[Fields] ([ID], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'32ee3561-226a-4dad-922a-9ed93099c457', 10, N'14cda248-4116-4e51-ac13-00096b43418c', N'Complete', N'Boolean', 0, 11)
INSERT [dbo].[Fields] ([ID], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'9f9b9fdb-3403-4dcd-a139-a28487c1832c', 8, N'dc1c6243-e510-4297-9df8-75babd237fbe', N'Website', N'Website', 0, 8)
INSERT [dbo].[Fields] ([ID], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'8f0915de-e77f-4b63-8b22-a4ff4afc99ff', 7, N'dc1c6243-e510-4297-9df8-75babd237fbe', N'Phone', N'PhoneNumber', 0, 7)
INSERT [dbo].[Fields] ([ID], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'7e7eaeb4-562b-481c-9a38-aee216b8b4a0', 10, N'1788a0c4-96e8-4b95-911a-75e1519d7259', N'Complete', N'Boolean', 0, 2)
INSERT [dbo].[Fields] ([ID], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'8f96e751-417f-489e-8be2-b9a2babf05d1', 3, N'14cda248-4116-4e51-ac13-00096b43418c', N'Priority', N'Priority', 1, 3)
INSERT [dbo].[Fields] ([ID], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'5b934dc3-983c-4f05-aa48-c26b43464bbf', 2, N'14cda248-4116-4e51-ac13-00096b43418c', N'Description', N'String', 1, 2)
INSERT [dbo].[Fields] ([ID], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'dea2ecad-1e53-4616-8ee9-c399d4223ffb', 1, N'1788a0c4-96e8-4b95-911a-75e1519d7259', N'Name', N'String', 1, 1)
INSERT [dbo].[Fields] ([ID], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'261950f7-7fda-4432-a280-d0373cc8cadf', 9, N'14cda248-4116-4e51-ac13-00096b43418c', N'Email', N'Email', 0, 9)
INSERT [dbo].[Fields] ([ID], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'6b3e6603-3bab-4994-a69c-df0f4310fa95', 3, N'dc1c6243-e510-4297-9df8-75babd237fbe', N'Priority', N'Priority', 0, 3)
INSERT [dbo].[Fields] ([ID], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'7715234d-a60e-4336-9af1-f05c36add1c8', 12, N'dc1c6243-e510-4297-9df8-75babd237fbe', N'Link to another list', N'ListPointer', 0, 10)
INSERT [dbo].[Fields] ([ID], [FieldTypeID], [ItemTypeID], [DisplayName], [DisplayType], [IsPrimary], [SortOrder]) VALUES (N'3f6f8964-fccd-47c6-8595-fbb0d5cab5c2', 1, N'14cda248-4116-4e51-ac13-00096b43418c', N'Name', N'String', 1, 1)
/****** Object:  Table [dbo].[Items]    Script Date: 01/16/2012 17:48:37 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Items](
	[ID] [uniqueidentifier] ROWGUIDCOL  NOT NULL,
	[ParentID] [uniqueidentifier] NULL,
	[ItemTypeID] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](max) NOT NULL,
	[FolderID] [uniqueidentifier] NOT NULL,
	[UserID] [uniqueidentifier] NOT NULL,
	[IsList] [bit] NOT NULL,
	[PriorityID] [int] NULL,
	[Description] [nvarchar](max) NULL,
	[DueDate] [nchar](10) NULL,
	[Complete] [bit] NOT NULL,
	[Location] [nvarchar](max) NULL,
	[Phone] [nvarchar](50) NULL,
	[Website] [nvarchar](max) NULL,
	[Email] [nvarchar](max) NULL,
	[Created] [datetime] NOT NULL,
	[LastModified] [datetime] NOT NULL,
	[LinkedFolderID] [uniqueidentifier] NULL,
 CONSTRAINT [PK_Tasks] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[FolderUsers]    Script Date: 01/16/2012 17:48:37 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FolderUsers](
	[ID] [uniqueidentifier] ROWGUIDCOL  NOT NULL,
	[FolderID] [uniqueidentifier] NOT NULL,
	[UserID] [uniqueidentifier] NOT NULL,
	[PermissionID] [int] NOT NULL,
 CONSTRAINT [PK_GroupUsers] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ItemTags]    Script Date: 01/16/2012 17:48:37 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ItemTags](
	[ID] [uniqueidentifier] ROWGUIDCOL  NOT NULL,
	[ItemID] [uniqueidentifier] NOT NULL,
	[TagID] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_TaskTags] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[FieldValues]    Script Date: 01/16/2012 17:48:37 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FieldValues](
	[ID] [uniqueidentifier] ROWGUIDCOL  NOT NULL,
	[FieldID] [uniqueidentifier] NOT NULL,
	[ItemID] [uniqueidentifier] NOT NULL,
	[Value] [nvarchar](max) NULL,
 CONSTRAINT [PK_FieldValues] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Default [DF_Fields_ID]    Script Date: 01/16/2012 17:48:37 ******/
ALTER TABLE [dbo].[Fields] ADD  CONSTRAINT [DF_Fields_ID]  DEFAULT (newid()) FOR [ID]
GO
/****** Object:  Default [DF_FieldValues_ID]    Script Date: 01/16/2012 17:48:37 ******/
ALTER TABLE [dbo].[FieldValues] ADD  CONSTRAINT [DF_FieldValues_ID]  DEFAULT (newid()) FOR [ID]
GO
/****** Object:  Default [DF_Groups_ID]    Script Date: 01/16/2012 17:48:37 ******/
ALTER TABLE [dbo].[Folders] ADD  CONSTRAINT [DF_Groups_ID]  DEFAULT (newid()) FOR [ID]
GO
/****** Object:  Default [DF_GroupUsers_ID]    Script Date: 01/16/2012 17:48:37 ******/
ALTER TABLE [dbo].[FolderUsers] ADD  CONSTRAINT [DF_GroupUsers_ID]  DEFAULT (newid()) FOR [ID]
GO
/****** Object:  Default [DF_Tasks_ID]    Script Date: 01/16/2012 17:48:37 ******/
ALTER TABLE [dbo].[Items] ADD  CONSTRAINT [DF_Tasks_ID]  DEFAULT (newid()) FOR [ID]
GO
/****** Object:  Default [DF_Tasks_PriorityID]    Script Date: 01/16/2012 17:48:37 ******/
ALTER TABLE [dbo].[Items] ADD  CONSTRAINT [DF_Tasks_PriorityID]  DEFAULT ((1)) FOR [PriorityID]
GO
/****** Object:  Default [DF_TaskTags_ID]    Script Date: 01/16/2012 17:48:37 ******/
ALTER TABLE [dbo].[ItemTags] ADD  CONSTRAINT [DF_TaskTags_ID]  DEFAULT (newid()) FOR [ID]
GO
/****** Object:  Default [DF_ListTypes_ID]    Script Date: 01/16/2012 17:48:37 ******/
ALTER TABLE [dbo].[ItemTypes] ADD  CONSTRAINT [DF_ListTypes_ID]  DEFAULT (newid()) FOR [ID]
GO
/****** Object:  Default [DF_Operations_ID]    Script Date: 01/16/2012 17:48:37 ******/
ALTER TABLE [dbo].[Operations] ADD  CONSTRAINT [DF_Operations_ID]  DEFAULT (newid()) FOR [ID]
GO
/****** Object:  Default [DF_Tags_ID]    Script Date: 01/16/2012 17:48:37 ******/
ALTER TABLE [dbo].[Tags] ADD  CONSTRAINT [DF_Tags_ID]  DEFAULT (newid()) FOR [ID]
GO
/****** Object:  Default [DF_Tags_Color]    Script Date: 01/16/2012 17:48:37 ******/
ALTER TABLE [dbo].[Tags] ADD  CONSTRAINT [DF_Tags_Color]  DEFAULT (N'White') FOR [Color]
GO
/****** Object:  Default [DF_Users_ID]    Script Date: 01/16/2012 17:48:37 ******/
ALTER TABLE [dbo].[Users] ADD  CONSTRAINT [DF_Users_ID]  DEFAULT (newid()) FOR [ID]
GO
/****** Object:  ForeignKey [FK_Fields_FieldTypes]    Script Date: 01/16/2012 17:48:37 ******/
ALTER TABLE [dbo].[Fields]  WITH CHECK ADD  CONSTRAINT [FK_Fields_FieldTypes] FOREIGN KEY([FieldTypeID])
REFERENCES [dbo].[FieldTypes] ([FieldTypeID])
GO
ALTER TABLE [dbo].[Fields] CHECK CONSTRAINT [FK_Fields_FieldTypes]
GO
/****** Object:  ForeignKey [FK_Fields_ItemTypes]    Script Date: 01/16/2012 17:48:37 ******/
ALTER TABLE [dbo].[Fields]  WITH CHECK ADD  CONSTRAINT [FK_Fields_ItemTypes] FOREIGN KEY([ItemTypeID])
REFERENCES [dbo].[ItemTypes] ([ID])
GO
ALTER TABLE [dbo].[Fields] CHECK CONSTRAINT [FK_Fields_ItemTypes]
GO
/****** Object:  ForeignKey [FK_FieldValues_Fields]    Script Date: 01/16/2012 17:48:37 ******/
ALTER TABLE [dbo].[FieldValues]  WITH CHECK ADD  CONSTRAINT [FK_FieldValues_Fields] FOREIGN KEY([FieldID])
REFERENCES [dbo].[Fields] ([ID])
GO
ALTER TABLE [dbo].[FieldValues] CHECK CONSTRAINT [FK_FieldValues_Fields]
GO
/****** Object:  ForeignKey [FK_FieldValues_Items]    Script Date: 01/16/2012 17:48:37 ******/
ALTER TABLE [dbo].[FieldValues]  WITH CHECK ADD  CONSTRAINT [FK_FieldValues_Items] FOREIGN KEY([ItemID])
REFERENCES [dbo].[Items] ([ID])
GO
ALTER TABLE [dbo].[FieldValues] CHECK CONSTRAINT [FK_FieldValues_Items]
GO
/****** Object:  ForeignKey [FK_Folders_Colors]    Script Date: 01/16/2012 17:48:37 ******/
ALTER TABLE [dbo].[Folders]  WITH CHECK ADD  CONSTRAINT [FK_Folders_Colors] FOREIGN KEY([ColorID])
REFERENCES [dbo].[Colors] ([ColorID])
GO
ALTER TABLE [dbo].[Folders] CHECK CONSTRAINT [FK_Folders_Colors]
GO
/****** Object:  ForeignKey [FK_Folders_Users]    Script Date: 01/16/2012 17:48:37 ******/
ALTER TABLE [dbo].[Folders]  WITH CHECK ADD  CONSTRAINT [FK_Folders_Users] FOREIGN KEY([UserID])
REFERENCES [dbo].[Users] ([ID])
GO
ALTER TABLE [dbo].[Folders] CHECK CONSTRAINT [FK_Folders_Users]
GO
/****** Object:  ForeignKey [FK_FolderUsers_Folders]    Script Date: 01/16/2012 17:48:37 ******/
ALTER TABLE [dbo].[FolderUsers]  WITH CHECK ADD  CONSTRAINT [FK_FolderUsers_Folders] FOREIGN KEY([FolderID])
REFERENCES [dbo].[Folders] ([ID])
GO
ALTER TABLE [dbo].[FolderUsers] CHECK CONSTRAINT [FK_FolderUsers_Folders]
GO
/****** Object:  ForeignKey [FK_FolderUsers_Permissions]    Script Date: 01/16/2012 17:48:37 ******/
ALTER TABLE [dbo].[FolderUsers]  WITH CHECK ADD  CONSTRAINT [FK_FolderUsers_Permissions] FOREIGN KEY([PermissionID])
REFERENCES [dbo].[Permissions] ([PermissionID])
GO
ALTER TABLE [dbo].[FolderUsers] CHECK CONSTRAINT [FK_FolderUsers_Permissions]
GO
/****** Object:  ForeignKey [FK_FolderUsers_Users]    Script Date: 01/16/2012 17:48:37 ******/
ALTER TABLE [dbo].[FolderUsers]  WITH CHECK ADD  CONSTRAINT [FK_FolderUsers_Users] FOREIGN KEY([UserID])
REFERENCES [dbo].[Users] ([ID])
GO
ALTER TABLE [dbo].[FolderUsers] CHECK CONSTRAINT [FK_FolderUsers_Users]
GO
/****** Object:  ForeignKey [FK_Items_Folders]    Script Date: 01/16/2012 17:48:37 ******/
ALTER TABLE [dbo].[Items]  WITH CHECK ADD  CONSTRAINT [FK_Items_Folders] FOREIGN KEY([FolderID])
REFERENCES [dbo].[Folders] ([ID])
GO
ALTER TABLE [dbo].[Items] CHECK CONSTRAINT [FK_Items_Folders]
GO
/****** Object:  ForeignKey [FK_Items_Items]    Script Date: 01/16/2012 17:48:37 ******/
ALTER TABLE [dbo].[Items]  WITH CHECK ADD  CONSTRAINT [FK_Items_Items] FOREIGN KEY([ParentID])
REFERENCES [dbo].[Items] ([ID])
GO
ALTER TABLE [dbo].[Items] CHECK CONSTRAINT [FK_Items_Items]
GO
/****** Object:  ForeignKey [FK_Items_Priorities]    Script Date: 01/16/2012 17:48:37 ******/
ALTER TABLE [dbo].[Items]  WITH CHECK ADD  CONSTRAINT [FK_Items_Priorities] FOREIGN KEY([PriorityID])
REFERENCES [dbo].[Priorities] ([PriorityID])
GO
ALTER TABLE [dbo].[Items] CHECK CONSTRAINT [FK_Items_Priorities]
GO
/****** Object:  ForeignKey [FK_Items_Users]    Script Date: 01/16/2012 17:48:37 ******/
ALTER TABLE [dbo].[Items]  WITH CHECK ADD  CONSTRAINT [FK_Items_Users] FOREIGN KEY([UserID])
REFERENCES [dbo].[Users] ([ID])
GO
ALTER TABLE [dbo].[Items] CHECK CONSTRAINT [FK_Items_Users]
GO
/****** Object:  ForeignKey [FK_ItemTags_Items]    Script Date: 01/16/2012 17:48:37 ******/
ALTER TABLE [dbo].[ItemTags]  WITH CHECK ADD  CONSTRAINT [FK_ItemTags_Items] FOREIGN KEY([ItemID])
REFERENCES [dbo].[Items] ([ID])
GO
ALTER TABLE [dbo].[ItemTags] CHECK CONSTRAINT [FK_ItemTags_Items]
GO
/****** Object:  ForeignKey [FK_ItemTags_Tags]    Script Date: 01/16/2012 17:48:37 ******/
ALTER TABLE [dbo].[ItemTags]  WITH CHECK ADD  CONSTRAINT [FK_ItemTags_Tags] FOREIGN KEY([TagID])
REFERENCES [dbo].[Tags] ([ID])
GO
ALTER TABLE [dbo].[ItemTags] CHECK CONSTRAINT [FK_ItemTags_Tags]
GO
/****** Object:  ForeignKey [FK_ListTypes_Users]    Script Date: 01/16/2012 17:48:37 ******/
ALTER TABLE [dbo].[ItemTypes]  WITH CHECK ADD  CONSTRAINT [FK_ListTypes_Users] FOREIGN KEY([UserID])
REFERENCES [dbo].[Users] ([ID])
GO
ALTER TABLE [dbo].[ItemTypes] CHECK CONSTRAINT [FK_ListTypes_Users]
GO
/****** Object:  ForeignKey [FK_Tags_Users]    Script Date: 01/16/2012 17:48:37 ******/
ALTER TABLE [dbo].[Tags]  WITH CHECK ADD  CONSTRAINT [FK_Tags_Users] FOREIGN KEY([UserID])
REFERENCES [dbo].[Users] ([ID])
GO
ALTER TABLE [dbo].[Tags] CHECK CONSTRAINT [FK_Tags_Users]
GO
