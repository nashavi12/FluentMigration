DROP TYPE IF EXISTS [dbo].[tbltype_TagValues]
GO

CREATE TYPE [dbo].[tbltype_TagValues] AS TABLE(
	[TagShortCode] [varchar](2) NOT NULL,
	[ReviewId] [varchar](50) NOT NULL,
	[Value] [bit] NOT NULL
)
GO
