using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

public class EmployeeManagerTests
{
    private readonly Mock<IEmployeeRepository> _mockRepository;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly Mock<ICloudService> _mockCloudService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly EmployeeManager _manager;

    public EmployeeManagerTests()
    {
        _mockRepository = new Mock<IEmployeeRepository>();
        _mockCache = new Mock<IMemoryCache>();
        _mockCloudService = new Mock<ICloudService>();
        _mockEmailService = new Mock<IEmailService>();

        _manager = new EmployeeManager(_mockRepository.Object, _mockCache.Object, _mockCloudService.Object, _mockEmailService.Object);
    }

    [Fact]
    public async Task BulkAddEmployeeDetailsAsync_ShouldReturnCorrectResults()
    {
        // Arrange
        var employees = new List<Employee>
        {
            new Employee { EmployeeId = 101, Name = "John Doe", Department = "IT" },
            new Employee { EmployeeId = 102, Name = "Jane Doe", Department = "HR" }
        };

        _mockRepository.Setup(repo => repo.BulkAddEmployeeDetailsAsync(It.IsAny<List<Employee>>()))
            .ReturnsAsync((2, 1, new List<Employee>()));

        // Act
        var (employeesAdded, employeesUpdated, failedRecords) = await _manager.BulkAddEmployeeDetailsAsync(employees);

        // Assert
        Assert.Equal(2, employeesAdded);
        Assert.Equal(1, employeesUpdated);
        Assert.Empty(failedRecords);
    }
}
