CREATE TABLE [dbo].[Priorities](
	[PriorityID] [int] IDENTITY(0,1) NOT NULL PRIMARY KEY CLUSTERED,
	[Name] [nvarchar](50) NOT NULL,
	[Color] [nvarchar](50) NOT NULL,
)
