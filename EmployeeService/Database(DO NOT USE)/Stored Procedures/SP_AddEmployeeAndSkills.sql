CREATE PROCEDURE SP_AddEmployeeAndSkills
    @EmployeeId INT,
    @Name NVARCHAR(100),
    @Department NVARCHAR(100),
    @Skills TVP_Skills READONLY
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ExistingName NVARCHAR(100);

    -- Check if EmployeeId already exists
    SELECT @ExistingName = Name 
    FROM Employees 
    WHERE EmployeeId = @EmployeeId;

    -- Validation: If EmployeeId exists but Name does not match, return an error
    IF @ExistingName IS NOT NULL 
        AND @Name IS NOT NULL 
        AND @Name <> '' 
        AND @ExistingName <> @Name
    BEGIN
        SELECT 'Validation Error: EmployeeId exists but Name does not match.' AS ErrorMessage;
        RETURN;
    END

    DECLARE @ActionMessage NVARCHAR(255) = '';
    DECLARE @SkillsAdded INT = 0;
    DECLARE @InsertedSkills TABLE (SkillId INT);

    -- If Employee does not exist, insert new employee
    IF @ExistingName IS NULL
    BEGIN
        INSERT INTO Employees (EmployeeId, Name, Department)
        VALUES (@EmployeeId, @Name, @Department);

        SET @ActionMessage = 'New Employee Added';
    END
    ELSE
    BEGIN
        SET @ActionMessage = 'Employee Exists';
    END

    -- Insert new skills and fetch generated SkillId
    IF EXISTS (SELECT 1 FROM @Skills)
    BEGIN
        -- Insert new skills that do not already exist
        MERGE INTO Skills AS target
        USING (SELECT DISTINCT Name, Category FROM @Skills) AS source
        ON target.Name = source.Name 
        AND target.Category = source.Category
        WHEN NOT MATCHED THEN 
            INSERT (Name, Category) 
            VALUES (source.Name, source.Category)
        OUTPUT INSERTED.SkillId INTO @InsertedSkills;

        -- Insert new Employee-Skill mappings
        INSERT INTO EmployeeSkills (EmployeeId, SkillId)
        SELECT s.EmployeeId, sk.SkillId 
        FROM @Skills s
        INNER JOIN Skills sk 
            ON s.Name = sk.Name 
            AND s.Category = sk.Category
        WHERE NOT EXISTS (
            SELECT 1 
            FROM EmployeeSkills es 
            WHERE es.EmployeeId = s.EmployeeId 
            AND es.SkillId = sk.SkillId
        );

        SET @SkillsAdded = @@ROWCOUNT;
    END

    IF @SkillsAdded > 0
    BEGIN
        SET @ActionMessage = @ActionMessage + ' and ' + CAST(@SkillsAdded AS NVARCHAR(10)) + ' New Skills Added';
    END

    SELECT @ActionMessage AS SuccessMessage;
END;
