﻿CREATE TABLE [dbo].[Workers]
(
	[Id] UNIQUEIDENTIFIER NOT NULL DEFAULT newid(),
	[FirstName] NVARCHAR(256) NOT NULL,
	[LastName] NVARCHAR(256) NOT NULL, 
    [DateOfBirth] DATE NOT NULL,
	[WorkerType] NVARCHAR(20) NOT NULL,
	[CreationDate] DATETIME NOT NULL, 
    [UpdatedDate] DATETIME NULL,

    PRIMARY KEY(Id)
)