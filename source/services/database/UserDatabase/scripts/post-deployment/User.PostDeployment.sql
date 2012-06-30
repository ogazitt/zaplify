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

/* User database - set the version string */
DECLARE @VersionString NVARCHAR(16);
SET @VersionString = N'1.0.2012.0622';
UPDATE [dbo].[DatabaseVersions]  SET [VersionType] = N'Schema', [VersionString] = @VersionString, [Status] = 'OK' WHERE VersionType = N'Schema'
IF @@ROWCOUNT=0
    INSERT INTO [dbo].[DatabaseVersions] ([VersionType], [VersionString], [Status]) VALUES (N'Schema', @VersionString, N'OK')

