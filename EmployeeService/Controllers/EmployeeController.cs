using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api")]
public class EmployeeController : ControllerBase
{
    private readonly IEmployeeManager _employeeManager;

    public EmployeeController(IEmployeeManager employeeManager)
    {
        _employeeManager = employeeManager;
    }

    // // GET: api/GetEmployeeById/{id}
    // [HttpGet("GetEmployeeById/{id}")]
    // public async Task<IActionResult> GetEmployeeById(int id)
    // {
    //     var employee = await _employeeManager.GetEmployeeByIdAsync(id);
    //     if (employee == null) return NotFound();
    //     return Ok(employee);
    // }

    // // GET: api/GetAllEmployees
    // [HttpGet("GetAllEmployees")]
    // public async Task<IActionResult> GetAllEmployees()
    // {
    //     var employees = await _employeeManager.GetAllEmployeesAsync();
    //     return Ok(employees);
    // }

    // // POST: api/CreateEmployee
    // [HttpPost("CreateEmployee")]
    // public async Task<IActionResult> CreateEmployee([FromBody] Employee employee)
    // {
    //     await _employeeManager.AddEmployeeAsync(employee);
    //     return CreatedAtAction(nameof(GetEmployeeById), new { id = employee.Id }, employee);
    // }

    // // PUT: api/UpdateEmployeeById/{id}
    // [HttpPut("UpdateEmployeeById/{id}")]
    // public async Task<IActionResult> UpdateEmployeeById(int id, [FromBody] Employee employee)
    // {
    //     if (id != employee.Id) return BadRequest("Employee ID mismatch.");
    //     await _employeeManager.UpdateEmployeeAsync(employee);
    //     return NoContent();
    // }

    // // DELETE: api/DeleteEmployeeById/{id}
    // [HttpDelete("DeleteEmployeeById/{id}")]
    // public async Task<IActionResult> DeleteEmployeeById(int id)
    // {
    //     await _employeeManager.DeleteEmployeeAsync(id);
    //     return NoContent();
    // }

    // // PUT: api/BulkUpdateEmployees
    // [HttpPut("BulkUpdateEmployees")]
    // public async Task<IActionResult> BulkUpdateEmployees([FromBody] List<Employee> employees)
    // {
    //     await _employeeManager.BulkUpdateEmployeesAsync(employees);
    //     return NoContent();
    // }




    [HttpPost("AddEmployeeDetails")]
    public async Task<IActionResult> AddEmployeeDetails([FromBody] JsonObject jsonData)
    {
        try
        {
            if (!jsonData.ContainsKey("employeeId") || !jsonData.ContainsKey("name"))
            {
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
            return result.IsValidationError ? BadRequest(new { message = result.Message }) : Ok(new { message = result.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"An error occurred: {ex.Message}" });
        }
    }
}
