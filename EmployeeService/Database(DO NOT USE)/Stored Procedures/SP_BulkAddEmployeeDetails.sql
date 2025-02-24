CREATE PROCEDURE SP_BulkAddEmployeeDetails
    @Employees TVP_Employees READONLY,
    @Skills TVP_Skills READONLY
AS
BEGIN
    SET NOCOUNT ON;

    -- Temporary tables for processing
    CREATE TABLE #ValidEmployees (
        EmployeeId INT,
        Name NVARCHAR(100),
        Department NVARCHAR(100)
    );

    CREATE TABLE #ValidSkills (
        EmployeeId INT,
        Name NVARCHAR(255),
        Category NVARCHAR(255)
    );

    CREATE TABLE #InvalidEmployees (
        EmployeeId INT,
        Name NVARCHAR(100)
    );

    -- Variables for counts
    DECLARE @EmployeesAdded INT = 0,
            @EmployeesUpdated INT = 0,
            @TotalEmployees INT = 0;

    -- Identify Employees with ID-Name Mismatch
    INSERT INTO #InvalidEmployees (EmployeeId, Name)
    SELECT e.EmployeeId, e.Name
    FROM @Employees e
    JOIN Employees db ON e.EmployeeId = db.EmployeeId
    WHERE e.Name IS NOT NULL AND e.Name <> db.Name;

    -- Copy Only Valid Employees
    INSERT INTO #ValidEmployees (EmployeeId, Name, Department)
    SELECT e.EmployeeId, e.Name, e.Department
    FROM @Employees e
    WHERE NOT EXISTS (
        SELECT 1 FROM #InvalidEmployees i WHERE i.EmployeeId = e.EmployeeId
    );

    -- Copy Only Valid Skills
    INSERT INTO #ValidSkills (EmployeeId, Name, Category)
    SELECT s.EmployeeId, s.Name, s.Category
    FROM @Skills s
    WHERE NOT EXISTS (
        SELECT 1 FROM #InvalidEmployees i WHERE i.EmployeeId = s.EmployeeId
    );

    -- Get Total Employees Count
    SET @TotalEmployees = (SELECT COUNT(*) FROM #ValidEmployees);

    -- Insert New Employees
    INSERT INTO Employees (EmployeeId, Name, Department)
    SELECT e.EmployeeId, e.Name, e.Department
    FROM #ValidEmployees e
    WHERE NOT EXISTS (
        SELECT 1 FROM Employees WHERE EmployeeId = e.EmployeeId
    );

    SET @EmployeesAdded = @@ROWCOUNT;

    SET @EmployeesUpdated = 
        @TotalEmployees - @EmployeesAdded - (SELECT COUNT(*) FROM #InvalidEmployees);

    --  Insert New Skills
    MERGE INTO Skills AS target
    USING (
        SELECT DISTINCT Name, Category FROM #ValidSkills
    ) AS source
    ON target.Name = source.Name AND target.Category = source.Category
    WHEN NOT MATCHED THEN
        INSERT (Name, Category) VALUES (source.Name, source.Category);

    -- Employee-Skill Mapping
    INSERT INTO EmployeeSkills (EmployeeId, SkillId)
    SELECT 
        s.EmployeeId, 
        sk.SkillId
    FROM #ValidSkills s
    INNER JOIN Skills sk ON s.Name = sk.Name AND s.Category = sk.Category
    INNER JOIN #ValidEmployees e ON e.EmployeeId = s.EmployeeId  -- Ensures correct Employee-Skill mapping
    WHERE NOT EXISTS (
        SELECT 1 FROM EmployeeSkills es WHERE es.EmployeeId = s.EmployeeId AND es.SkillId = sk.SkillId
    );

    SELECT @EmployeesAdded AS EmployeesAdded, @EmployeesUpdated AS EmployeesUpdated;

    SELECT EmployeeId, Name FROM #InvalidEmployees;


    DROP TABLE #ValidEmployees;
    DROP TABLE #ValidSkills;
    DROP TABLE #InvalidEmployees;
END;
