CREATE TABLE [dbo].[Camera] (
    [Id]                INT            IDENTITY (1, 1) NOT NULL,
    [Alias]             NVARCHAR (30)  NULL,
    [DestinationFolder] NVARCHAR (255) NULL,
    [IpAddress]         NVARCHAR (255) NULL,
    [PortNumber]        INT            NOT NULL,
    [UserName]          NVARCHAR (30)  NULL,
    CONSTRAINT [PK_Camera] PRIMARY KEY CLUSTERED ([Id] ASC)
);

