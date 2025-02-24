public interface IEmployeeManager
{
    // Task<Employee?> GetEmployeeByIdAsync(int id);
    // Task<List<Employee>> GetAllEmployeesAsync();
    // Task AddEmployeeAsync(Employee employee);
    // Task UpdateEmployeeAsync(Employee employee);
    // Task DeleteEmployeeAsync(int id);
    // Task BulkUpdateEmployeesAsync(List<Employee> employees);
    Task<(bool IsValidationError, string Message)> AddEmployeeDetailsAsync(Employee employee);
    Task<(int EmployeesAdded, int EmployeesUpdated, List<Employee>)> BulkAddEmployeeDetailsAsync(List<Employee> employees);
    Task<object> GetEmployeesBySkill(EmployeeSearchRequest request, bool returnBlob);
    Task<object> GetEmployeesWithoutSkill(EmployeeSearchRequest request, string userEmail, bool generateReport = false);

}