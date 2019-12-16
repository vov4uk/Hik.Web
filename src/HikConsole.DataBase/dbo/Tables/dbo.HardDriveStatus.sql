CREATE TABLE [dbo].[HardDriveStatus] (
    [Id]               INT     IDENTITY (1, 1) NOT NULL,
    [CameraId]         INT     NOT NULL,
    [JobId]            INT     NOT NULL,
    [Capacity]         BIGINT  NOT NULL,
    [FreeSpace]        BIGINT  NOT NULL,
    [HdStatus]         BIGINT  NOT NULL,
    [HDAttr]           TINYINT NOT NULL,
    [HDType]           TINYINT NOT NULL,
    [Recycling]        TINYINT NOT NULL,
    [PictureCapacity]  BIGINT  NOT NULL,
    [FreePictureSpace] BIGINT  NOT NULL,
    CONSTRAINT [PK_HardDriveStatus] PRIMARY KEY CLUSTERED ([Id] ASC)
);

