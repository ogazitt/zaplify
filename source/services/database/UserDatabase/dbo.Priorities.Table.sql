CREATE TABLE [dbo].[Priorities](
	[PriorityID] [int] IDENTITY(0,1) NOT NULL PRIMARY KEY CLUSTERED,
	[Name] [nvarchar](64) NOT NULL,
	[Color] [nvarchar](32) NOT NULL,
)
