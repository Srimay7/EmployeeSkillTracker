using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

public class EmployeeRepository : IEmployeeRepository
{
    //private readonly AppDbContext _context;
    private readonly string _connectionString;

    public EmployeeRepository(IConfiguration configuration)//AppDbContext context, 
    {
        //_context = context;
        _connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new ArgumentNullException(nameof(_connectionString), "Database connection string is missing.");
    }

    // public async Task<Employee?> GetEmployeeByIdAsync(int id)
    // {
    //     return await _context.Employees.FindAsync(id); // EF Core
    // }

    // public async Task<List<Employee>> GetAllEmployeesAsync()
    // {
    //     return await _context.Employees.ToListAsync(); // EF Core
    // }

    // public async Task AddEmployeeAsync(Employee employee)
    // {
    //     await _context.Employees.AddAsync(employee); // EF Core
    //     await _context.SaveChangesAsync();
    // }

    // public async Task UpdateEmployeeAsync(Employee employee)
    // {
    //     _context.Employees.Update(employee); // EF Core
    //     await _context.SaveChangesAsync();
    // }

    // public async Task DeleteEmployeeAsync(int id)
    // {
    //     var employee = await _context.Employees.FindAsync(id);
    //     if (employee != null)
    //     {
    //         _context.Employees.Remove(employee); // EF Core
    //         await _context.SaveChangesAsync();
    //     }
    // }

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
        skillsTable.Columns.Add("Name", typeof(string));
        skillsTable.Columns.Add("Category", typeof(string));

        foreach (var skill in employee.Skills)
        {
            skillsTable.Rows.Add(skill.Name, skill.Category);
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




}