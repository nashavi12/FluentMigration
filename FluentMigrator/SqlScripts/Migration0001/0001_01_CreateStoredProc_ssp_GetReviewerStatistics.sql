DROP PROC IF EXISTS [dbo].[ssp_GetReviewerStatistics]
GO

CREATE PROCEDURE [dbo].[ssp_GetReviewerStatistics]
	@teamName VARCHAR(50),
	@reviewerId VARCHAR(50),
	@startDate DATETIMEOFFSET,
	@endDate DATETIMEOFFSET
AS
	SET NOCOUNT ON;
	--Proc code
GO
