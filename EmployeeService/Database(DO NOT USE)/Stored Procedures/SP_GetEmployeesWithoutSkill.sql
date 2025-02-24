CREATE PROCEDURE SP_GetEmployeesWithoutSkill
    @SkillName NVARCHAR(255),
    @SkillCategory NVARCHAR(255) = NULL,
    @Department NVARCHAR(255) = NULL,
    @PageSize INT = 10,
    @PageNumber INT = 1,
    @Sorting NVARCHAR(50) = 'Name',
    @SortOrder NVARCHAR(4) = 'ASC'
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SQLQuery NVARCHAR(MAX);

    SET @SQLQuery = '
    WITH EmployeesWithoutSkill AS (
        SELECT DISTINCT 
            e.EmployeeId, 
            e.Name, 
            e.Department,
            STRING_AGG(s.Name, '' | '') AS Skills,
            STRING_AGG(CAST(s.SkillId AS NVARCHAR), '' | '') AS SkillIds,
            s.Category AS Category
        FROM Employees e
        LEFT JOIN EmployeeSkills es ON e.EmployeeId = es.EmployeeId
        LEFT JOIN Skills s ON es.SkillId = s.SkillId
        WHERE e.IsDeleted = 0 
        AND e.EmployeeId NOT IN (
            SELECT es2.EmployeeId 
            FROM EmployeeSkills es2
            INNER JOIN Skills s2 ON es2.SkillId = s2.SkillId
            WHERE s2.Name = @SkillName
        )';


    IF @SkillCategory IS NOT NULL AND @SkillCategory <> ''
        SET @SQLQuery = @SQLQuery + ' AND s.Category LIKE ''%'' + @SkillCategory + ''%'' ';

    IF @Department IS NOT NULL AND @Department <> ''
        SET @SQLQuery = @SQLQuery + ' AND e.Department LIKE ''%'' + @Department + ''%'' ';


    SET @SQLQuery = @SQLQuery + '
        GROUP BY e.EmployeeId, e.Name, e.Department, s.SkillId, s.Category
    )';


    SET @SQLQuery = @SQLQuery + '
    SELECT * FROM EmployeesWithoutSkill
    ORDER BY ' + QUOTENAME(@Sorting) + ' ' + @SortOrder + '
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;';


    EXEC sp_executesql @SQLQuery, 
        N'@SkillName NVARCHAR(255), @SkillCategory NVARCHAR(255), @Department NVARCHAR(255), 
          @PageSize INT, @PageNumber INT',
        @SkillName, @SkillCategory, @Department, @PageSize, @PageNumber;
END;
