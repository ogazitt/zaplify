/****** Object:  Table [dbo].[WorkflowTypes]     ******/
INSERT [dbo].[WorkflowTypes] ([Type], [Definition]) VALUES (N'buy gift', N'{ "Name": "buy gift", "States": [ 
	{ "Name": "Who is this for?", "Activity": "Get Possible Subjects", "NextState": "Which kind of gift?" },
	{ "Name": "Which kind of gift?", "Activity": "Get Subject Likes", "NextState": "Helpful links" },
	{ "Name": "Helpful links", "Activity": "Get Bing Suggestions", "NextState": null } ] }')
GO
