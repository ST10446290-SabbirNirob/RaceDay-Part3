-- Run ONCE against the existing RaceDay SQL Server database before running Part 3.
-- Adds fields only; all existing Part 1 and Part 2 records stay intact.
IF COL_LENGTH('dbo.Event', 'DistanceKm') IS NULL
    ALTER TABLE dbo.[Event] ADD DistanceKm decimal(6,2) NULL;
GO
IF COL_LENGTH('dbo.Event', 'BannerUrl') IS NULL
    ALTER TABLE dbo.[Event] ADD BannerUrl nvarchar(2048) NULL;
GO
IF COL_LENGTH('dbo.User', 'ProfilePictureUrl') IS NULL
    ALTER TABLE dbo.[User] ADD ProfilePictureUrl nvarchar(2048) NULL;
GO
