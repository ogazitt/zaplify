CREATE TABLE [dbo].[UserCredentials](
    [ID] [bigint] IDENTITY(1, 1) NOT NULL,
    [UserID] [uniqueidentifier] NOT NULL REFERENCES [dbo].[Users] ([ID]) ON DELETE CASCADE ON UPDATE CASCADE,
    [Password] [nvarchar](256) NOT NULL,
    [PasswordSalt] [nvarchar](256) NOT NULL,
    [FBConsentToken] [nvarchar](max) NULL,
    [ADConsentToken] [nvarchar](max) NULL,
    [LastModified] [datetime2] NOT NULL,

    CONSTRAINT [PK_UserCredentialID] PRIMARY KEY ([ID]),
    CONSTRAINT [UNIQUE_UserID] UNIQUE ([UserID])
)

