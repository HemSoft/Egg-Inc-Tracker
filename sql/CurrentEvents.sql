SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[CurrentEvents]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        e.SubTitle
      , e.EndTime
    FROM
        Events e
    WHERE
        GETDATE() BETWEEN StartTime AND EndTime
END
GO
