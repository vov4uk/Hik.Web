CREATE TABLE [dbo].[Job] (
    [Id]          INT           IDENTITY (1, 1) NOT NULL,
    [FailsCount]  INT           NULL,
    [PeriodStart] DATETIME2 (7) NOT NULL,
    [PeriodEnd]   DATETIME2 (7) NOT NULL,
    [Started]     DATETIME2 (7) NOT NULL,
    [Finished]    DATETIME2 (7) NULL,
    CONSTRAINT [PK_Job] PRIMARY KEY CLUSTERED ([Id] ASC)
);

