/****** Object:  Table [dbo].[WorkflowTypes]     ******/
INSERT [dbo].[WorkflowTypes] ([Type], [Definition]) VALUES (N'buy gift', N'{ "States": [ 
	{ "Name": "GetPossibleSubjects", "Activity": "GetPossibleSubjects", "NextState": "GetSubjectLikes" },
	{ "Name": "GetSubjectLikes", "Activity": "GetSubjectLikes", "NextState": "GetBingSuggestions" },
	{ "Name": "GetBingSuggestions", "Activity": "GetBingSuggestions(SearchTemplate={$(Intent) }{$(Likes) }{for $(Relationship)})", "NextState": null } ] }')
INSERT [dbo].[WorkflowTypes] ([Type], [Definition]) VALUES (N'fake buy gift', N'{ "States": [ 
	{ "Name": "GetPossibleSubjects", "Activity": "FakeGetPossibleSubjects", "NextState": "GetSubjectLikes" },
	{ "Name": "GetSubjectLikes", "Activity": "FakeGetSubjectLikes", "NextState": "GetBingSuggestions" },
	{ "Name": "GetBingSuggestions", "Activity": "GetBingSuggestions(SearchTemplate={buy gift }{$(Likes) }{for $(Relationship)})", "NextState": null } ] }')
INSERT [dbo].[WorkflowTypes] ([Type], [Definition]) VALUES (N'new buy gift', N'{ "States": [ 
	{ "Name": "GetPossibleSubjects", "Activity": "GetPossibleSubjects", "NextState": "GenerateSubjectLikes" },
	{ "Name": "GenerateSubjectLikes", "Activity": "GenerateSubjectLikes", "NextState": "Foreach" },
	{ "Name": "Foreach", "Activity": "Foreach(ForeachOver={$(LikeSuggestionList)},ForeachBody=GetBingSuggestions(SearchTemplate={buy gift }{$(Like) }{for $(Relationship)}))", "NextState": null } ] }')
