using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class TestStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IEmployeeManager, EmployeeManager>();
    }
}
