CREATE TABLE [dbo].[UserCredentials](
    [ID] [bigint] IDENTITY(1, 1) NOT NULL,
    [UserID] [uniqueidentifier] NOT NULL REFERENCES [dbo].[Users] ([ID]) ON DELETE CASCADE ON UPDATE CASCADE,
    [CredentialType] [nvarchar](32) NOT NULL,
    [AccessToken] [nvarchar](max) NULL,
    [AccessTokenExpiration] [datetime2] NULL,
    [RenewalToken] [nvarchar](max) NULL,
    [LastModified] [datetime2] NOT NULL,
    [LastAccessed] [datetime2] NULL,

    CONSTRAINT [PK_UserCredentialID] PRIMARY KEY ([ID]),
    CONSTRAINT [UNIQUE_UserID_CredentialType] UNIQUE ([UserID],[CredentialType])
)

