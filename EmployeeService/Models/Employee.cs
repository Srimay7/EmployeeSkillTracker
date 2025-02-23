using System.Text.Json.Serialization;

public class Employee
{
    public int EmployeeId { get; set; }
    public string Name { get; set; }
    public string Department { get; set; }
    public bool IsDeleted { get; set; }//soft deletion for historical data analysis
    [JsonIgnore]
    public DateTime DateCreated { get; set; }//future scope
    public List<Skill> Skills { get; set; }
}