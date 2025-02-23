using Microsoft.EntityFrameworkCore;

public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IEmployeeManager, EmployeeManager>();

        // services.AddDbContext<AppDbContext>(options =>
        //     options.UseSqlServer(_configuration.GetConnectionString("DefaultConnection")));
    }

    public void Configure(WebApplication app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Employee API V1"));
        }

        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}