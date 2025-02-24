public class EmployeeSearchRequest
{
    public string SkillName { get; set; }
    public string SkillCategory { get; set; }
    public string Department { get; set; }
    public int PageSize { get; set; } = 10;
    public int PageNumber { get; set; } = 1;
    public string Sorting { get; set; } = "DateCreated";
    public string SortOrder { get; set; } = "ASC";
}
