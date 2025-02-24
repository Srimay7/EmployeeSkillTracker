using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

public class EmployeeControllerTests
{
    private readonly Mock<IEmployeeManager> _mockManager;
    private readonly EmployeeController _controller;

    public EmployeeControllerTests()
    {
        _mockManager = new Mock<IEmployeeManager>();
        _controller = new EmployeeController(_mockManager.Object);
    }

    [Fact]
    public async Task AddEmployeeDetails_ShouldReturnOk_WhenValidData()
    {
        // Arrange
        var jsonData = new JsonObject
        {
            ["employeeId"] = 101,
            ["name"] = "John Doe",
            ["department"] = "IT",
            ["skills"] = new JsonArray
            {
                new JsonObject { ["name"] = "C#", ["category"] = "Programming" }
            }
        };

        _mockManager.Setup(m => m.AddEmployeeDetailsAsync(It.IsAny<Employee>()))
            .ReturnsAsync((false, "Employee added successfully")); // Adjusted based on controller logic

        // Act
        var result = await _controller.AddEmployeeDetails(jsonData) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
        Assert.Contains("Employee added successfully", result.Value.ToString());
    }

    [Fact]
    public async Task AddEmployeeDetails_ShouldReturnBadRequest_WhenMissingFields()
    {
        // Arrange
        var jsonData = new JsonObject { ["employeeId"] = 101 }; // Missing "name"

        // Act
        var result = await _controller.AddEmployeeDetails(jsonData) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task BulkAddEmployeeDetails_ShouldReturnOk_WhenValidData()
    {
        // Arrange
        var employees = new List<Employee>
        {
            new Employee { EmployeeId = 101, Name = "John Doe", Department = "IT" },
            new Employee { EmployeeId = 102, Name = "Jane Doe", Department = "HR" }
        };

        _mockManager.Setup(m => m.BulkAddEmployeeDetailsAsync(It.IsAny<List<Employee>>()))
            .ReturnsAsync((2, 1, new List<Employee>())); // 2 added, 1 updated

        // Act
        var result = await _controller.BulkAddEmployeeDetails(employees) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
        Assert.Contains("Bulk Employee Details Insert Completed", result.Value.ToString());
    }

    [Fact]
    public async Task GetEmployeesBySkill_ShouldReturnOk_WhenValidRequest()
    {
        // Arrange
        var request = new EmployeeSearchRequest { SkillName = "C#", PageSize = 10, PageNumber = 1 };

        _mockManager.Setup(m => m.GetEmployeesBySkill(It.IsAny<EmployeeSearchRequest>(), false))
            .ReturnsAsync(new List<Employee> { new Employee { EmployeeId = 101, Name = "John Doe", Department = "IT" } });

        // Act
        var result = await _controller.GetEmployeesBySkill(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
        Assert.IsType<List<Employee>>(result.Value);
    }

    [Fact]
    public async Task GenerateSkillGapReport_ShouldReturnOk_WhenValidRequest()
    {
        // Arrange
        var request = new EmployeeSearchRequest { SkillName = "Azure DevOps", PageSize = 10, PageNumber = 1 };

        _mockManager.Setup(m => m.GetEmployeesWithoutSkill(It.IsAny<EmployeeSearchRequest>(), It.IsAny<string>(), false))
            .ReturnsAsync(new List<Employee> { new Employee { EmployeeId = 102, Name = "Jane Doe", Department = "HR" } });

        // Act
        var result = await _controller.GenerateSkillGapReport(request) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
        Assert.IsType<List<Employee>>(result.Value);
    }
}
