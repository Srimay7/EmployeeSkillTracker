public interface IEmployeeRepository
{
    // Task<Employee?> GetEmployeeByIdAsync(int id);
    // Task<List<Employee>> GetAllEmployeesAsync();
    // Task AddEmployeeAsync(Employee employee);
    // Task UpdateEmployeeAsync(Employee employee);
    // Task BulkUpdateEmployeesAsync(List<Employee> employees);
    // Task DeleteEmployeeAsync(int id);
    Task<(bool IsValidationError, string Message)> AddEmployeeDetailsAsync(Employee employee);
}