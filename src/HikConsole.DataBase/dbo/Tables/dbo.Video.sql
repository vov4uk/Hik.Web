CREATE TABLE [dbo].[Video] (
    [Id]                INT            IDENTITY (1, 1) NOT NULL,
    [CameraId]          INT            NOT NULL,
    [JobId]             INT            NOT NULL,
    [Name]              NVARCHAR (255) NULL,
    [StartTime]         DATETIME2 (7)  NOT NULL,
    [StopTime]          DATETIME2 (7)  NOT NULL,
    [DownloadStartTime] DATETIME2 (7)  NOT NULL,
    [DownloadStopTime]  DATETIME2 (7)  NOT NULL,
    [Size]              BIGINT         NOT NULL,
    [LocalSize]         BIGINT         NOT NULL,
    CONSTRAINT [PK_Video] PRIMARY KEY CLUSTERED ([Id] ASC)
);

