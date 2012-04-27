/*
Post-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script.		
 Use SQLCMD syntax to include a file in the post-deployment script.			
 Example:      :r .\myfile.sql								
 Use SQLCMD syntax to reference a variable in the post-deployment script.		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/

/* Suggestions database - set the schema version */
DECLARE @VersionString NVARCHAR(16);
SET @VersionString = N'1.0.2012.0426';
UPDATE [dbo].[DatabaseVersions]  SET [VersionType] = N'Schema', [VersionString] = @VersionString, [Status] = 'OK' WHERE VersionType = N'Schema'
IF @@ROWCOUNT=0
    INSERT INTO [dbo].[DatabaseVersions] ([VersionType], [VersionString], [Status]) VALUES (N'Schema', @VersionString, N'OK')

/*
:r .\dbo.Intents.Table.sql
GO
:Setvar Intent "$(Intent)"
:Setvar Like "$(Like)"
:Setvar Likes "$(Likes)"
:Setvar LikeSuggestionList "$(LikeSuggestionList)"
:Setvar Relationship "$(Relationship)"
:r .\dbo.WorkflowTypes.Table.sql
GO
:r .\dbo.DatabaseVersions.Table.sql
GO
*/