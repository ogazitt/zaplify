CREATE TABLE [dbo].[Intents](
    [IntentID] [bigint] IDENTITY(1, 1) NOT NULL,
    [Name] [nvarchar](256) NOT NULL,
    [Verb] [nvarchar](256) NOT NULL,
    [Noun] [nvarchar](256) NOT NULL,
    [SearchFormatString] [nvarchar](max) NULL,
    CONSTRAINT [PK_IntentID] PRIMARY KEY ([IntentID]),
)

