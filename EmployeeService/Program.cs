using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure dependency injection
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IEmployeeManager, EmployeeManager>();

// Enable CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Employee API V1"));

app.UseHttpsRedirection(); // ✅ Redirect HTTP to HTTPS
app.UseRouting();          // ✅ Ensure routing is set
app.UseCors("AllowAll");   // ✅ Enable CORS if needed
app.UseAuthorization();    // ✅ Ensure authorization works

app.MapControllers();
app.Run();




