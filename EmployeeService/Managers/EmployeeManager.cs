using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;

public class EmployeeManager : IEmployeeManager
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IMemoryCache _cache;
    private readonly ICloudService _cloudService;
    private readonly IEmailService _emailService;
    private readonly ILogger<EmployeeManager> _logger;

    private static readonly HashSet<string> AllowedSortFields = new()
    {
        "Name", "Skill", "Category", "Department", "DateCreated"
    };

    public EmployeeManager(
        IEmployeeRepository employeeRepository, 
        IMemoryCache cache, 
        ICloudService cloudService, 
        IEmailService emailService, 
        ILogger<EmployeeManager> logger)
    {
        _employeeRepository = employeeRepository;
        _cache = cache;
        _cloudService = cloudService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<(bool IsValidationError, string Message)> AddEmployeeDetailsAsync(Employee employee)
    {
        try
        {
            _logger.LogInformation("Processing AddEmployeeDetails for EmployeeId: {EmployeeId}", employee.EmployeeId);
            var result = await _employeeRepository.AddEmployeeDetailsAsync(employee);
            _logger.LogInformation("AddEmployeeDetails completed for EmployeeId: {EmployeeId}. Success: {Success}", 
                employee.EmployeeId, !result.IsValidationError);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AddEmployeeDetails for EmployeeId: {EmployeeId}", employee.EmployeeId);
            return (true, $"An error occurred: {ex.Message}");
        }
    }

    private const int BatchSize = 100;

    public async Task<(int EmployeesAdded, int EmployeesUpdated, List<Employee>)> BulkAddEmployeeDetailsAsync(List<Employee> employees)
    {
        _logger.LogInformation("Received BulkAddEmployeeDetails request with {Count} employees", employees?.Count ?? 0);

        if (employees == null || employees.Count == 0)
        {
            _logger.LogWarning("BulkAddEmployeeDetails failed: Employee list is empty.");
            return (0, 0, new List<Employee>());
        }

        var failedRecords = new List<Employee>();
        var validEmployees = new List<Employee>();

        foreach (var emp in employees)
        {
            if (emp.EmployeeId <= 0 || string.IsNullOrWhiteSpace(emp.Name))
            {
                emp.ErrorMessage = "Invalid EmployeeId or Name is missing";
                failedRecords.Add(emp);
                continue;
            }

            emp.Department ??= "General";

            if (emp.Skills != null)
            {
                foreach (var skill in emp.Skills)
                {
                    if (string.IsNullOrWhiteSpace(skill.Name))
                    {
                        emp.ErrorMessage = "Skill name is required";
                        failedRecords.Add(emp);
                        break;
                    }
                    skill.Category ??= "General";
                }
                continue;
            }

            validEmployees.Add(emp);
        }

        if (validEmployees.Count == 0)
        {
            _logger.LogWarning("BulkAddEmployeeDetails failed: No valid employees to process.");
            return (0, 0, failedRecords);
        }

        _logger.LogInformation("Processing BulkAddEmployeeDetails in batches of {BatchSize}", BatchSize);

        var tasks = new List<Task<(int, int, List<Employee>)>>();
        foreach (var batch in validEmployees.Chunk(BatchSize))
        {
            tasks.Add(_employeeRepository.BulkAddEmployeeDetailsAsync(batch.ToList()));
        }

        var results = await Task.WhenAll(tasks);

        int totalEmployeesAdded = results.Sum(r => r.Item1);
        int totalEmployeesUpdated = results.Sum(r => r.Item2);

        foreach (var batchResult in results)
        {
            foreach (var invalidEmp in batchResult.Item3)
            {
                invalidEmp.ErrorMessage = "EmployeeId exists but Name does not match";
                failedRecords.Add(invalidEmp);
            }
        }

        _logger.LogInformation("BulkAddEmployeeDetails completed: {EmployeesAdded} added, {EmployeesUpdated} updated, {FailedRecords} failed",
            totalEmployeesAdded, totalEmployeesUpdated, failedRecords.Count);

        return (totalEmployeesAdded, totalEmployeesUpdated, failedRecords);
    }

    public async Task<object> GetEmployeesBySkill(EmployeeSearchRequest request, bool returnBlob)
    {
        _logger.LogInformation("Received GetEmployeesBySkill request: {Request}", JsonSerializer.Serialize(request));

        if (string.IsNullOrWhiteSpace(request.Sorting) || !AllowedSortFields.Contains(request.Sorting))
        {
            _logger.LogWarning("Invalid Sorting field: {Sorting}. Using default.", request.Sorting);
            request.Sorting = "DateCreated";
        }

        if (string.IsNullOrWhiteSpace(request.SortOrder))
        {
            request.SortOrder = "ASC";
        }

        string cacheKey = GenerateCacheKey(request);

        if (_cache.TryGetValue(cacheKey, out List<Employee> cachedEmployees))
        {
            _logger.LogInformation("Cache hit for GetEmployeesBySkill.");
            return returnBlob ? new { BlobUrl = await _cloudService.UploadEmployeesToBlob(cachedEmployees) } : cachedEmployees;
        }

        _logger.LogInformation("Cache miss for GetEmployeesBySkill. Fetching from database...");
        var employees = await _employeeRepository.GetEmployeesBySkill(request);
        _cache.Set(cacheKey, employees, new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(10)));

        _logger.LogInformation("GetEmployeesBySkill completed. Returning {EmployeeCount} records.", employees.Count);
        return returnBlob ? new { BlobUrl = await _cloudService.UploadEmployeesToBlob(employees) } : employees;
    }

    public async Task<object> GetEmployeesWithoutSkill(EmployeeSearchRequest request, string userEmail, bool generateReport = false)
    {
        _logger.LogInformation("Received GetEmployeesWithoutSkill request for Skill: {SkillName}, GenerateReport: {GenerateReport}",
            request.SkillName, generateReport);

        string cacheKey = $"SkillGap:{request.SkillName}:{request.Department}:{request.PageSize}:{request.PageNumber}:{request.Sorting}:{request.SortOrder}";

        if (_cache.TryGetValue(cacheKey, out List<Employee> cachedEmployees))
        {
            _logger.LogInformation("Cache hit for GetEmployeesWithoutSkill.");
            if (generateReport)
            {
                var reportUrl = await _cloudService.UploadAndSendSkillGapReport(cachedEmployees, userEmail);
                return new { Message = "Report generated and email sent successfully", ReportUrl = reportUrl };
            }
            return cachedEmployees;
        }

        _logger.LogInformation("Cache miss for GetEmployeesWithoutSkill. Fetching from database...");
        var employees = await _employeeRepository.GetEmployeesWithoutSkill(request);
        _cache.Set(cacheKey, employees, new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(10)));

        _logger.LogInformation("GetEmployeesWithoutSkill completed. Returning {EmployeeCount} records.", employees.Count);

        if (generateReport)
        {
            var reportUrl = await _cloudService.UploadAndSendSkillGapReport(employees, userEmail);
            return new { Message = "Report generated and email sent successfully", ReportUrl = reportUrl };
        }

        return employees;
    }

    private string GenerateCacheKey(EmployeeSearchRequest request)
    {
        return $"EmployeeBySkill:{request.SkillName ?? "All"}:{request.SkillCategory ?? "All"}:{request.Department ?? "All"}:{request.PageSize}:{request.PageNumber}:{request.Sorting}:{request.SortOrder ?? "ASC"}";
    }
}
