CREATE TABLE Employees (
    EmployeeId INT PRIMARY KEY,  -- Unique Employee ID (Manually inserted, not auto-incremented)
    Name NVARCHAR(100) NOT NULL, -- Employee name, required
    Department NVARCHAR(100) DEFAULT 'General', -- Default department if not provided
    IsDeleted BIT DEFAULT 0,  -- Soft delete flag (0 = Active, 1 = Deleted)
    DateCreated DATETIME DEFAULT GETDATE() -- Timestamp for record creation
);