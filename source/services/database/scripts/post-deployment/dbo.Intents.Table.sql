/****** Object:  Table [dbo].[Intents]     ******/
SET IDENTITY_INSERT [dbo].[Intents] ON
/*
INSERT [dbo].[Intents] ([IntentID], [Name], [Verb], [Noun], [SearchFormatString]) VALUES (0, N'buy gift', N'buy', N'gift', N'buy gift {for (Relationship)} {(Like)}')
INSERT [dbo].[Intents] ([IntentID], [Name], [Verb], [Noun], [SearchFormatString]) VALUES (1, N'buy gift', N'buy', N'present', N'buy gift {for (Relationship)} {(Like)}')
INSERT [dbo].[Intents] ([IntentID], [Name], [Verb], [Noun], [SearchFormatString]) VALUES (2, N'change oil', N'change', N'oil', N'change oil {in (Location)}')
INSERT [dbo].[Intents] ([IntentID], [Name], [Verb], [Noun], [SearchFormatString]) VALUES (3, N'clean gutters', N'clean', N'gutters', N'gutter cleaners {in (Location)}')
*/
INSERT [dbo].[Intents] ([IntentID], [Name], [Verb], [Noun], [SearchFormatString]) VALUES (0, N'buy gift', N'buy', N'gift', N'buy gift')
INSERT [dbo].[Intents] ([IntentID], [Name], [Verb], [Noun], [SearchFormatString]) VALUES (1, N'buy gift', N'buy', N'present', N'buy gift')
INSERT [dbo].[Intents] ([IntentID], [Name], [Verb], [Noun], [SearchFormatString]) VALUES (2, N'change oil', N'change', N'oil', N'change oil')
INSERT [dbo].[Intents] ([IntentID], [Name], [Verb], [Noun], [SearchFormatString]) VALUES (3, N'clean gutters', N'clean', N'gutters', N'gutter cleaners')
INSERT [dbo].[Intents] ([IntentID], [Name], [Verb], [Noun], [SearchFormatString]) VALUES (4, N'fake buy gift', N'get', N'present', N'buy gift')
INSERT [dbo].[Intents] ([IntentID], [Name], [Verb], [Noun], [SearchFormatString]) VALUES (5, N'fake buy gift', N'get', N'gift', N'buy gift')
SET IDENTITY_INSERT [dbo].[Intents] OFF
