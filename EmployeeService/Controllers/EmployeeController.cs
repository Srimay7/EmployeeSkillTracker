using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

[ApiController]
[Route("api")]
public class EmployeeController : ControllerBase
{
    private readonly IEmployeeManager _employeeManager;
    private readonly ILogger<EmployeeController> _logger;

    public EmployeeController(IEmployeeManager employeeManager, ILogger<EmployeeController> logger)
    {
        _employeeManager = employeeManager;
        _logger = logger;
    }

    [HttpPost("AddEmployeeDetails")]
    public async Task<IActionResult> AddEmployeeDetails([FromBody] JsonObject jsonData)
    {
        try
        {
            _logger.LogInformation("Received AddEmployeeDetails request: {Request}", jsonData.ToJsonString());

            if (!jsonData.ContainsKey("employeeId") || !jsonData.ContainsKey("name"))
            {
                _logger.LogWarning("AddEmployeeDetails failed: Missing EmployeeId or Name.");
                return BadRequest(new { message = "EmployeeId or Name is required." });
            }

            var employee = new Employee
            {
                EmployeeId = jsonData["employeeId"].GetValue<int>(),
                Name = jsonData["name"].GetValue<string>(),
                Department = jsonData.ContainsKey("department") ? jsonData["department"].GetValue<string>() : "General",
                Skills = new List<Skill>()
            };

            if (jsonData.ContainsKey("skills") && jsonData["skills"] is JsonArray skillArray)
            {
                foreach (var skillNode in skillArray)
                {
                    var skill = skillNode.AsObject();
                    if (!skill.ContainsKey("name"))
                    {
                        _logger.LogWarning("AddEmployeeDetails failed: Skill Name is missing.");
                        return BadRequest(new { message = "Skill Name is required if skills are provided." });
                    }

                    employee.Skills.Add(new Skill
                    {
                        Name = skill["name"].GetValue<string>(),
                        Category = skill.ContainsKey("category") ? skill["category"].GetValue<string>() : "Uncategorized"
                    });
                }
            }

            var result = await _employeeManager.AddEmployeeDetailsAsync(employee);
            if (result.IsValidationError)
            {
                _logger.LogWarning("AddEmployeeDetails validation error: {Message}", result.Message);
                return BadRequest(new { message = result.Message });
            }

            _logger.LogInformation("AddEmployeeDetails successful for EmployeeId: {EmployeeId}", employee.EmployeeId);
            return Ok(new { message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AddEmployeeDetails");
            return StatusCode(500, new { message = $"An error occurred: {ex.Message}" });
        }
    }


    private const int BatchSize = 100;

    [HttpPost("BulkAddEmployeeDetails")]
    public async Task<IActionResult> BulkAddEmployeeDetails([FromBody] List<Employee> employees)
    {
        _logger.LogInformation("Received BulkAddEmployeeDetails request with {Count} employees", employees?.Count ?? 0);

        if (employees == null || employees.Count == 0)
        {
            _logger.LogWarning("BulkAddEmployeeDetails failed: Employee list is empty.");
            return BadRequest("Employee list cannot be empty.");
        }

        var (employeesAdded, employeesUpdated, failedRecords) = 
            await _employeeManager.BulkAddEmployeeDetailsAsync(employees);

        _logger.LogInformation("BulkAddEmployeeDetails completed: {EmployeesAdded} added, {EmployeesUpdated} updated, {FailedRecords} failed", 
            employeesAdded, employeesUpdated, failedRecords.Count);

        return Ok(new
        {
            Message = "Bulk Employee Details Insert Completed",
            EmployeesAdded = employeesAdded,
            EmployeesUpdated = employeesUpdated,
            FailedRecords = failedRecords
        });
    }


    [HttpPost("GetEmployeesData")]
    public async Task<IActionResult> GetEmployeesBySkill([FromBody] EmployeeSearchRequest request, [FromHeader] bool ProvideBlobPath = false)
    {
        try
        {
            _logger.LogInformation("Received GetEmployeesBySkill request: {Request}", JsonSerializer.Serialize(request));

            var result = await _employeeManager.GetEmployeesBySkill(request, ProvideBlobPath);

            _logger.LogInformation("GetEmployeesBySkill completed successfully.");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetEmployeesBySkill");
            return StatusCode(500, new { Error = "An unexpected error occurred.", Details = ex.Message });
        }
    }

    [HttpPost("GenerateSkillGapReport")]
    public async Task<IActionResult> GenerateSkillGapReport([FromBody] EmployeeSearchRequest request, [FromHeader] string UserEmail = "abc@gmail.com", [FromHeader] bool GenerateReport = false)
    {
        try
        {
            _logger.LogInformation("Received GenerateSkillGapReport request for Skill: {SkillName} with Report Generation: {GenerateReport}", 
                request.SkillName, GenerateReport);

            var result = await _employeeManager.GetEmployeesWithoutSkill(request, UserEmail, GenerateReport);

            _logger.LogInformation("GenerateSkillGapReport completed successfully. Report generated: {GenerateReport}", GenerateReport);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GenerateSkillGapReport");
            return StatusCode(500, new { Error = "An unexpected error occurred.", Details = ex.Message });
        }
    }
}
