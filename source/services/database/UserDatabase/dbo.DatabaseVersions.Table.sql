CREATE TABLE [dbo].[DatabaseVersions](
	[VersionType] [nvarchar](16) NOT NULL PRIMARY KEY CLUSTERED,
	[VersionString] [nvarchar](16) NOT NULL,
	[Status] [nvarchar](64) NULL,
)
