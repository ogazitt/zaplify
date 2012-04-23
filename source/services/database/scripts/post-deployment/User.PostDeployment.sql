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

/* User database */
:r .\dbo.Users.Table.sql
GO
:r .\dbo.ActionTypes.Table.sql
GO
:r .\dbo.Colors.Table.sql
GO
:r .\dbo.ItemTypes.Table.sql
GO
:r .\dbo.Fields.Table.sql
GO
:r .\dbo.Permissions.Table.sql
GO
:r .\dbo.Priorities.Table.sql
GO
:r .\dbo.DatabaseVersions.Table.sql
GO

