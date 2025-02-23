public interface IEmployeeManager
{
    // Task<Employee?> GetEmployeeByIdAsync(int id);
    // Task<List<Employee>> GetAllEmployeesAsync();
    // Task AddEmployeeAsync(Employee employee);
    // Task UpdateEmployeeAsync(Employee employee);
    // Task DeleteEmployeeAsync(int id);
    // Task BulkUpdateEmployeesAsync(List<Employee> employees);
    Task<(bool IsValidationError, string Message)> AddEmployeeDetailsAsync(Employee employee);
}