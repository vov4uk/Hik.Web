CREATE TABLE [dbo].[Photo] (
    [Id]                INT           IDENTITY (1, 1) NOT NULL,
    [CameraId]          INT           NOT NULL,
    [JobId]             INT           NOT NULL,
    [Name]              NVARCHAR (30) NULL,
    [DateTaken]         DATETIME2 (7) NOT NULL,
    [DownloadStartTime] DATETIME2 (7) NOT NULL,
    [DownloadStopTime]  DATETIME2 (7) NOT NULL,
    [Size]              BIGINT        NOT NULL,
    [LocalSize]         BIGINT        NULL,
    CONSTRAINT [PK_Photo] PRIMARY KEY CLUSTERED ([Id] ASC)
);

