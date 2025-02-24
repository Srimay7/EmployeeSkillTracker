using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly string _connectionString;

    public EmployeeRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new ArgumentNullException(nameof(_connectionString), "Database connection string is missing.");
    }
    public async Task BulkUpdateEmployeesAsync(List<Employee> employees)
    {
        using SqlConnection connection = new(_connectionString);
        await connection.OpenAsync();
        using SqlTransaction transaction = connection.BeginTransaction();
        try
        {
            foreach (var employee in employees)
            {
                string query = "UPDATE Employees SET Name = @Name, Department = @Department WHERE Id = @Id";
                using SqlCommand command = new(query, connection, transaction);
                command.Parameters.AddWithValue("@Id", employee.EmployeeId);
                command.Parameters.AddWithValue("@Name", employee.Name);
                command.Parameters.AddWithValue("@Department", employee.Department);
                await command.ExecuteNonQueryAsync();
            }
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<(bool IsValidationError, string Message)> AddEmployeeDetailsAsync(Employee employee)
{
    using SqlConnection connection = new(_connectionString);
    await connection.OpenAsync();
    using SqlTransaction transaction = connection.BeginTransaction();
    try
    {
        using SqlCommand command = new("SP_AddEmployeeAndSkills", connection, transaction)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@EmployeeId", employee.EmployeeId);
        command.Parameters.AddWithValue("@Name", employee.Name);
        command.Parameters.AddWithValue("@Department", employee.Department);

        DataTable skillsTable = new();
        skillsTable.Columns.Add("EmployeeId", typeof(int));
        skillsTable.Columns.Add("Name", typeof(string));
        skillsTable.Columns.Add("Category", typeof(string));

        foreach (var skill in employee.Skills)
        {
            skillsTable.Rows.Add(employee.EmployeeId, skill.Name, skill.Category);
        }

        //if (employee.Skills.Count > 0)
        //{
            SqlParameter skillsParam = command.Parameters.AddWithValue("@Skills", skillsTable);
            skillsParam.SqlDbType = SqlDbType.Structured;
        //}
        // else
        // {
        //     command.Parameters.AddWithValue("@Skills", DBNull.Value);
        // }

        using SqlDataReader reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            string resultMessage = reader.GetString(0);
            reader.Close();
            if (resultMessage.StartsWith("Validation Error"))
            {
                await transaction.RollbackAsync();
                return (true, resultMessage);
            }
        }
        
        await transaction.CommitAsync();
        return (false, "Operation completed successfully.");
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        return (true, $"Database error: {ex.Message}");
    }
}


    public async Task<(int EmployeesAdded, int EmployeesUpdated, List<Employee> InvalidEmployees)> BulkAddEmployeeDetailsAsync(List<Employee> employees)
    {
        using SqlConnection connection = new(_connectionString);
        await connection.OpenAsync();
        using SqlTransaction transaction = connection.BeginTransaction();
        try
        {
            using SqlCommand command = new("SP_BulkAddEmployeeDetails", connection, transaction)
            {
                CommandType = CommandType.StoredProcedure
            };

            DataTable employeeTable = new();
            employeeTable.Columns.Add("EmployeeId", typeof(int));
            employeeTable.Columns.Add("Name", typeof(string));
            employeeTable.Columns.Add("Department", typeof(string));

            DataTable skillsTable = new();
            skillsTable.Columns.Add("EmployeeId", typeof(int));
            skillsTable.Columns.Add("Name", typeof(string));
            skillsTable.Columns.Add("Category", typeof(string));

            foreach (var emp in employees)
            {
                employeeTable.Rows.Add(emp.EmployeeId, emp.Name, emp.Department ?? "General");

                foreach (var skill in emp.Skills ?? new List<Skill>())
                {
                    skillsTable.Rows.Add(emp.EmployeeId, skill.Name, skill.Category);
                }
            }

            command.Parameters.AddWithValue("@Employees", employeeTable).SqlDbType = SqlDbType.Structured;
            command.Parameters.AddWithValue("@Skills", skillsTable).SqlDbType = SqlDbType.Structured;

            int employeesAdded = 0, employeesUpdated = 0;
            var invalidEmployees = new List<Employee>();

            using (SqlDataReader reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    employeesAdded = reader.GetInt32(0);
                    employeesUpdated = reader.GetInt32(1);
                }

                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        invalidEmployees.Add(new Employee
                        {
                            EmployeeId = reader.GetInt32(0),
                            Name = reader.GetString(1)
                        });
                    }
                }
            } 

            await transaction.CommitAsync();
            return (employeesAdded, employeesUpdated, invalidEmployees);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (0, 0, employees); // Return all as invalid if there's an error
        }
    }


public async Task<List<Employee>> GetEmployeesBySkill(EmployeeSearchRequest request)
{
    using SqlConnection connection = new(_connectionString);
    await connection.OpenAsync();

    using SqlCommand command = new("SP_GetEmployeesBySkill", connection)
    {
        CommandType = CommandType.StoredProcedure
    };

    command.Parameters.AddWithValue("@SkillName", (object?)request.SkillName ?? DBNull.Value);
    command.Parameters.AddWithValue("@SkillCategory", (object?)request.SkillCategory ?? DBNull.Value);
    command.Parameters.AddWithValue("@Department", (object?)request.Department ?? DBNull.Value);
    command.Parameters.AddWithValue("@PageSize", request.PageSize);
    command.Parameters.AddWithValue("@PageNumber", request.PageNumber);
    command.Parameters.AddWithValue("@Sorting", request.Sorting);
    command.Parameters.AddWithValue("@SortOrder", request.SortOrder);


    var employees = new List<Employee>();
    var employeeDictionary = new Dictionary<int, Employee>();

    using SqlDataReader reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        int employeeId = reader.GetInt32(0);
        if (!employeeDictionary.TryGetValue(employeeId, out var employee))
        {
            employee = new Employee
            {
                EmployeeId = employeeId,
                Name = reader.GetString(1),
                Department = reader.GetString(2),
                Skills = new List<Skill>()
            };
            employeeDictionary[employeeId] = employee;
        }

        if (!reader.IsDBNull(3))
        {
            employee.Skills.Add(new Skill
            {
                Name = reader.GetString(3),
                SkillId = Convert.ToInt32(reader.GetString(4)),
                Category = reader.GetString(5)
            });
        }
    }

    return employeeDictionary.Values.ToList();
}

    public async Task<List<Employee>> GetEmployeesWithoutSkill(EmployeeSearchRequest request)
    {

        using SqlConnection connection = new(_connectionString);
        await connection.OpenAsync();
        using SqlCommand command = new("SP_GetEmployeesWithoutSkill", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@SkillName", (object?)request.SkillName ?? DBNull.Value);
        command.Parameters.AddWithValue("@Department", (object?)request.Department ?? DBNull.Value);
        command.Parameters.AddWithValue("@PageSize", request.PageSize);
        command.Parameters.AddWithValue("@PageNumber", request.PageNumber);
        command.Parameters.AddWithValue("@Sorting", request.Sorting ?? "DateCreated");
        command.Parameters.AddWithValue("@SortOrder", request.SortOrder ?? "DESC");

        var employeeDictionary = new Dictionary<int, Employee>();

    using SqlDataReader reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        int employeeId = reader.GetInt32(0);
        if (!employeeDictionary.TryGetValue(employeeId, out var employee))
        {
            employee = new Employee
            {
                EmployeeId = employeeId,
                Name = reader.GetString(1),
                Department = reader.GetString(2),
                Skills = new List<Skill>()
            };
            employeeDictionary[employeeId] = employee;
        }

        if (!reader.IsDBNull(3))
        {
            employee.Skills.Add(new Skill
            {
                Name = reader.GetString(3),
                SkillId = Convert.ToInt32(reader.GetString(4)),
                Category = reader.GetString(5)
            });
        }
    }
        return employeeDictionary.Values.ToList();
    }


}