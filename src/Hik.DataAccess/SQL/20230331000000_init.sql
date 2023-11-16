
PRAGMA foreign_keys = off;
BEGIN TRANSACTION;

CREATE TABLE IF NOT EXISTS DailyStatistics (
    Id            INTEGER       NOT NULL CONSTRAINT PK_DailyStatistics PRIMARY KEY AUTOINCREMENT,
    JobTriggerId  INTEGER       NOT NULL,
    Period        DATETIME2 (0) NOT NULL,
    FilesCount    INTEGER       NOT NULL,
    FilesSize     INTEGER       NOT NULL,
    TotalDuration INTEGER,
    CONSTRAINT FK_DailyStatistics_JobTrigger_JobTriggerId FOREIGN KEY ( JobTriggerId ) REFERENCES JobTrigger (Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS ExceptionLog (
    Id           INTEGER       NOT NULL CONSTRAINT PK_ExceptionLog PRIMARY KEY AUTOINCREMENT,
    JobId        INTEGER       NOT NULL,
    Created      DATETIME2 (0) NOT NULL DEFAULT (datetime('now', 'localtime') ),
    HikErrorCode INTEGER,
    Message      TEXT,
    CallStack    TEXT,
    CONSTRAINT FK_ExceptionLog_Job_JobId FOREIGN KEY ( JobId ) REFERENCES Job (Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS Job (
    Id           INTEGER       NOT NULL CONSTRAINT PK_Job PRIMARY KEY AUTOINCREMENT,
    JobTriggerId INTEGER       NOT NULL,
    Success      INTEGER       NOT NULL  DEFAULT 1,
    PeriodStart  DATETIME2 (0),
    PeriodEnd    DATETIME2 (0),
    Started      DATETIME2 (0) NOT NULL,
    Finished     DATETIME2 (0),
    FilesCount   INTEGER       NOT NULL,
    CONSTRAINT FK_Job_JobTrigger_JobTriggerId FOREIGN KEY ( JobTriggerId )
    REFERENCES JobTrigger (Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS JobTrigger (
    Id                INTEGER       NOT NULL CONSTRAINT PK_JobTrigger PRIMARY KEY AUTOINCREMENT,
    TriggerKey        TEXT,
    [Group]           TEXT,
    LastSync          DATETIME2 (0),
    ShowInSearch      BOOLEAN       DEFAULT (true),
    LastExecutedJobId INTEGER,
    ClassName         text          default '',
    [Config]          text          default '',
    [Description]     text          default '',
    CronExpression    text          default '',
    IsEnabled         BOOLEAN       DEFAULT (true),
    SentEmailOnError  BOOLEAN       DEFAULT (true)
);

CREATE TABLE IF NOT EXISTS MediaFile (
    Id                      INTEGER       NOT NULL CONSTRAINT PK_MediaFile PRIMARY KEY AUTOINCREMENT,
    JobTriggerId            INTEGER       NOT NULL,
    JobId                   INTEGER       NOT NULL,
    Name                    TEXT,
    Path                    TEXT,
    Date                    DATETIME2 (0) NOT NULL,
    DownloadStarted         DATETIME2 (0) NOT NULL,
    DownloadDuration        INTEGER,
    Duration                INTEGER,
    Size                    INTEGER       NOT NULL,
    Objects                 TEXT,
    CONSTRAINT FK_MediaFile_JobTrigger_JobTriggerId FOREIGN KEY ( JobTriggerId ) REFERENCES JobTrigger (Id) ON DELETE CASCADE
    CONSTRAINT FK_MediaFile_Job_JobId FOREIGN KEY ( JobId ) REFERENCES Job (Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS IX_DailyStatistics_JobTriggerId ON DailyStatistics ( "JobTriggerId" );

CREATE UNIQUE INDEX IF NOT EXISTS IX_ExceptionLog_JobId ON ExceptionLog ( "JobId" );

CREATE INDEX IF NOT EXISTS IX_Job_JobTriggerId ON Job ( "JobTriggerId" );

CREATE INDEX IF NOT EXISTS IX_MediaFile_JobTriggerId ON MediaFile ( "JobTriggerId" );

CREATE INDEX IF NOT EXISTS IX_MediaFile_JobId ON MediaFile ( "JobId" );

CREATE INDEX IF NOT EXISTS IX_MediaFile_Date ON MediaFile ( "Date" );

COMMIT TRANSACTION;
PRAGMA foreign_keys = on;
