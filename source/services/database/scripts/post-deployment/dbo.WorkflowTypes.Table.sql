/****** Object:  Table [dbo].[WorkflowTypes]     ******/
INSERT [dbo].[WorkflowTypes] ([Type], [Definition]) VALUES (N'buy gift', N'{ "States": [ 
	{ "Name": "Who is this for?", "Activity": "GetPossibleSubjects", "NextState": "Which kind of gift?" },
	{ "Name": "Which kind of gift?", "Activity": "GetSubjectLikes", "NextState": "Helpful links" },
	{ "Name": "Helpful links", "Activity": "GetBingSuggestions", "NextState": null } ] }')
INSERT [dbo].[WorkflowTypes] ([Type], [Definition]) VALUES (N'fake buy gift', N'{ "States": [ 
	{ "Name": "Who is this for?", "Activity": "FakeGetPossibleSubjects", "NextState": "Which kind of gift?" },
	{ "Name": "Which kind of gift?", "Activity": "FakeGetSubjectLikes", "NextState": "Helpful links" },
	{ "Name": "Helpful links", "Activity": "GetBingSuggestions", "NextState": null } ] }')
