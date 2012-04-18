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
INSERT [dbo].[WorkflowTypes] ([Type], [Definition]) VALUES (N'new new buy gift', N'{ "States": [ 
	{ "Name": "GetPossibleSubjects", "Activity": "GetPossibleSubjects", "NextState": "GenerateSubjectLikes" },
	{ "Name": "GenerateSubjectLikes", "Activity": "GenerateSubjectLikes", "NextState": "Foreach" },
	{ "Name": "Foreach", 
	  "Activity": { 
	      "Name" : "Foreach",
	      "List": "$(LikeSuggestionList)",
	      "Activity": {
		      "Name": "GetBingSuggestions",
		      "SearchTemplate": "[\"buy gift\",\"$(Like)\",\"for $(Relationship)\"]"
		  }
	  },
	  "NextState": null } ] }')
INSERT [dbo].[WorkflowTypes] ([Type], [Definition]) VALUES (N'Connect to Facebook', N'{ "States": [ 
	{ "Name": "ConnectToFacebook", "Activity": "ConnectToFacebook", "NextState": "ImportContactsFromFacebook" },
	{ "Name": "ImportContactsFromFacebook", "Activity": "ImportFromFacebook", "NextState": null } ] }')
INSERT [dbo].[WorkflowTypes] ([Type], [Definition]) VALUES (N'Connect to Active Directory', N'{ "States": [ 
	{ "Name": "ConnectToActiveDirectory", "Activity": "ConnectToActiveDirectory", "NextState": null } ] }')
INSERT [dbo].[WorkflowTypes] ([Type], [Definition]) VALUES (N'New Contact', N'{ "States": [ 
	{ "Name": "AddContact", "Activity": "AddContactToPossibleSubjects", "NextState": null } ] }')
INSERT [dbo].[WorkflowTypes] ([Type], [Definition]) VALUES (N'New User', N'{ "States": [ 
	{ "Name": "InvokeFBWorkflow", "Activity": "StartWorkflow(WorkflowType=Connect to Facebook)", "NextState": "InvokeADWorkflow" },
	{ "Name": "InvokeADWorkflow", "Activity": "StartWorkflow(WorkflowType=Connect to Active Directory)", "NextState": null } ] }')
INSERT [dbo].[WorkflowTypes] ([Type], [Definition]) VALUES (N'New Task', N'{ "States": [ 
	{ "Name": "DetermineIntent", "Activity": "GetPossibleIntents", "NextState": "InvokeWorkflow" },
	{ "Name": "InvokeWorkflow", "Activity": "StartWorkflow(WorkflowType={$(Intent)})", "NextState": null } ] }')
