public interface ICloudService
{    Task<string> UploadEmployeesToBlob(List<Employee> employees);
    Task<string> UploadAndSendSkillGapReport(List<Employee> employees, string userEmail);
}
