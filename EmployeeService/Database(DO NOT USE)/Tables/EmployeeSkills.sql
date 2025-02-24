CREATE TABLE EmployeeSkills (
    EmployeeId INT NOT NULL,
    SkillId INT NOT NULL,
    PRIMARY KEY (EmployeeId, SkillId),
    FOREIGN KEY (EmployeeId) REFERENCES Employees(EmployeeId) ON DELETE CASCADE,
    FOREIGN KEY (SkillId) REFERENCES Skills(SkillId) ON DELETE CASCADE
);