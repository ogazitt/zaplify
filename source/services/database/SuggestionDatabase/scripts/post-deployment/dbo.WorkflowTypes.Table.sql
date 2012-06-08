/****** Object:  Table [dbo].[WorkflowTypes]     ******/
/* buy gift */
INSERT [dbo].[WorkflowTypes] ([Type], [Definition]) VALUES (N'buy gift', N'{ "States": [ 
	{ "Name": "GetPossibleSubjects", "Activity": "GetPossibleSubjects", "NextState": "GetSubjectLikes" },
	{ "Name": "GetSubjectLikes", "Activity": "GetSubjectLikes", "NextState": "GetBingSuggestions" },
	{ 
		"Name": "GetBingSuggestions", 
		"ActivityDefinition": 
		{
			"Name": "GetBingSuggestions",
			"SearchTemplate": ["buy gift","$(Like)","for $(Relationship)"]
		}, 
		"NextState": null 
	} ] }')
INSERT [dbo].[WorkflowTypes] ([Type], [Definition]) VALUES (N'fake buy gift', N'{ "States": [ 
	{ "Name": "GetPossibleSubjects", "Activity": "FakeGetPossibleSubjects", "NextState": "GetSubjectLikes" },
	{ "Name": "GetSubjectLikes", "Activity": "FakeGetSubjectLikes", "NextState": "GetBingSuggestions" },
	{ 
		"Name": "GetBingSuggestions", 
		"ActivityDefinition": 
		{
			"Name": "GetBingSuggestions",
			"SearchTemplate": ["buy gift","$(Like)","for $(Relationship)"]
		}, 
		"NextState": null 
	} ] }')
INSERT [dbo].[WorkflowTypes] ([Type], [Definition]) VALUES (N'new buy gift', N'{ "States": [ 
	{ "Name": "GetPossibleSubjects", "Activity": "GetPossibleSubjects", "NextState": "GenerateSubjectLikes" },
	{ "Name": "GenerateSubjectLikes", "Activity": "GenerateSubjectLikes", "NextState": "Foreach" },
	{ 
		"Name": "Foreach", 
		"ActivityDefinition": { 
			"Name": "Foreach",
			"List": "$(LikeSuggestionList)",
			"Activity": {
				"Name": "GetBingSuggestions",
				"SearchTemplate": ["buy gift","$(Like)","for $(Relationship)"]
			}
		},
		"NextState": null 
	} ] }')

/* get connected workflows */
INSERT [dbo].[WorkflowTypes] ([Type], [Definition]) VALUES (N'Connect to Facebook', N'{ "States": [ 
	{ "Name": "ConnectToFacebook", "Activity": "ConnectToFacebook", "NextState": "ImportContactsFromFacebook" },
	{ "Name": "ImportContactsFromFacebook", "Activity": "ImportFromFacebook", "NextState": null } ] }')
INSERT [dbo].[WorkflowTypes] ([Type], [Definition]) VALUES (N'Connect to Active Directory', N'{ "States": [ 
	{ "Name": "ConnectToActiveDirectory", "Activity": "ConnectToActiveDirectory", "NextState": null } ] }')

/* new user, task, contact workflows */
INSERT [dbo].[WorkflowTypes] ([Type], [Definition]) VALUES (N'New Contact', N'{ "States": [ 
	{ "Name": "GetFacebookInfo", "Activity": "GetContactInfoFromFacebook", "NextState": "AddContact" },
	{ "Name": "AddContact", "Activity": "AddContactToPossibleContacts", "NextState": null } ] }')
INSERT [dbo].[WorkflowTypes] ([Type], [Definition]) VALUES (N'New Grocery', N'{ "States": [ 
	{ "Name": "GetCategory", "Activity": "GetGroceryCategory", "NextState": null } ] }')
INSERT [dbo].[WorkflowTypes] ([Type], [Definition]) VALUES (N'New Task', N'{ "States": [ 
	{ "Name": "DetermineIntent", "Activity": "GetPossibleIntents", "NextState": "InvokeWorkflow" },
	{ 
		"Name": "InvokeWorkflow", 
		"ActivityDefinition": 
		{
			"Name": "StartWorkflow",
			"WorkflowType": "$(Intent)"
		}, 
		"NextState": null 
	} ] }')
INSERT [dbo].[WorkflowTypes] ([Type], [Definition]) VALUES (N'New User', N'{ "States": [ 
	{ 
		"Name": "InvokeFBWorkflow",
		"ActivityDefinition": 
		{
			"Name": "StartWorkflow",
			"WorkflowType": "Connect to Facebook"
		}, 
		"NextState": "InvokeADWorkflow" 
	},
	{ 
		"Name": "InvokeADWorkflow", 
		"ActivityDefinition": 
		{
			"Name": "StartWorkflow",
			"WorkflowType": "Connect to Active Directory"
		}, 
		"NextState": null 
	} ] }')
