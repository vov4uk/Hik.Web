ALTER TABLE JobTrigger ADD COLUMN ClassName text default '';
ALTER TABLE JobTrigger ADD COLUMN Config text default '';
ALTER TABLE JobTrigger ADD COLUMN Description text default '';
ALTER TABLE JobTrigger ADD COLUMN CronExpression text default '';
ALTER TABLE JobTrigger ADD COLUMN RunAsTask BOOLEAN       DEFAULT (true);
ALTER TABLE JobTrigger ADD COLUMN IsEnabled BOOLEAN       DEFAULT (true);
