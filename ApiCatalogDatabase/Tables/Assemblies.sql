﻿CREATE TABLE [dbo].[Assemblies]
(
	[AssemblyId] INT IDENTITY(1, 1) NOT NULL PRIMARY KEY,
	[AssemblyGuid] UNIQUEIDENTIFIER NOT NULL,
	[Name] NVARCHAR(255) NOT NULL,
	[Version] NVARCHAR(15) NOT NULL,
	[PublicKeyToken] NVARCHAR(16) NOT NULL,
)

GO

CREATE UNIQUE INDEX [IX_Assemblies_AssemblyGuid] ON [dbo].[Assemblies] ([AssemblyGuid])