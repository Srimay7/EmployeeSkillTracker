// using Microsoft.EntityFrameworkCore;

// public class AppDbContext : DbContext
// {
//     public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
//     public DbSet<Employee> Employees { get; set; }
//     public DbSet<Skill> Skills { get; set; }
//     public DbSet<EmployeeSkill> EmployeeSkills { get; set; }

//     protected override void OnModelCreating(ModelBuilder modelBuilder)
//     {
//         modelBuilder.Entity<EmployeeSkill>()
//             .HasKey(es => new { es.EmployeeId, es.SkillId });
//     }
// }