using System.Collections.Generic;

public static class TestHelpers
{
    public static List<Employee> GetFakeEmployees()
    {
        return new List<Employee>
        {
            new Employee { EmployeeId = 101, Name = "John Doe", Department = "IT" },
            new Employee { EmployeeId = 102, Name = "Jane Doe", Department = "HR" }
        };
    }
}
