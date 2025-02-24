public interface IEmployeeRepository
{
    Task<(bool IsValidationError, string Message)> AddEmployeeDetailsAsync(Employee employee);
    Task<(int EmployeesAdded, int EmployeesUpdated, List<Employee> InvalidEmployees)> BulkAddEmployeeDetailsAsync(List<Employee> employees);
    Task<List<Employee>> GetEmployeesBySkill(EmployeeSearchRequest request);
    Task<List<Employee>> GetEmployeesWithoutSkill(EmployeeSearchRequest request);
}