﻿BEGIN TRANSACTION;

ALTER TABLE JobTrigger DROP COLUMN LastSuccessJobId;
ALTER TABLE JobTrigger DROP COLUMN RunAsTask;

COMMIT