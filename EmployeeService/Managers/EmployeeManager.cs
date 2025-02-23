public class EmployeeManager : IEmployeeManager
{
    private readonly IEmployeeRepository _employeeRepository;

    public EmployeeManager(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    // public async Task<Employee?> GetEmployeeByIdAsync(int id) =>
    //     await _employeeRepository.GetEmployeeByIdAsync(id);

    // public async Task<List<Employee>> GetAllEmployeesAsync() =>
    //     await _employeeRepository.GetAllEmployeesAsync();

    // public async Task AddEmployeeAsync(Employee employee) =>
    //     await _employeeRepository.AddEmployeeAsync(employee);

    // public async Task UpdateEmployeeAsync(Employee employee) =>
    //     await _employeeRepository.UpdateEmployeeAsync(employee);

    // public async Task DeleteEmployeeAsync(int id) =>
    //     await _employeeRepository.DeleteEmployeeAsync(id);

    // public async Task BulkUpdateEmployeesAsync(List<Employee> employees) =>
    //     await _employeeRepository.BulkUpdateEmployeesAsync(employees);

    public async Task<(bool IsValidationError, string Message)> AddEmployeeDetailsAsync(Employee employee)
    {
        try
        {
            return await _employeeRepository.AddEmployeeDetailsAsync(employee);
        }
        catch (Exception ex)
        {
            return (true, $"An error occurred: {ex.Message}");
        }
        // finally
        // {
        //     // Log the operation status (Optional: Implement proper logging mechanism)
        //     Console.WriteLine("AddEmployeeDetails operation completed.");
        // }
    }
}