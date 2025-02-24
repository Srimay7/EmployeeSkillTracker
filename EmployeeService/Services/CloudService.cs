using Google.Cloud.Storage.V1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class CloudService : ICloudService
{
    private readonly StorageClient _storageClient;
    private readonly string _bucketName;
    private readonly ILogger<CloudService> _logger;
    private readonly IEmailService _emailService;

    public CloudService(IConfiguration configuration, ILogger<CloudService> logger, IEmailService emailService)
    {
        _storageClient = StorageClient.Create();
        _bucketName = configuration["GoogleCloud:BucketName"] 
            ?? throw new ArgumentNullException("Bucket name is missing in configuration.");
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<string> UploadEmployeesToBlob(List<Employee> employees)
    {
        if (employees == null || employees.Count == 0)
        {
            _logger.LogWarning("UploadEmployeesToBlob: No employees found to upload.");
            throw new ArgumentException("Employee list cannot be empty.");
        }

        string fileName = $"employees_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
        string filePath = Path.Combine(Path.GetTempPath(), fileName);

        try
        {
            _logger.LogInformation($"Generating CSV file at: {filePath}");

            // Convert employee data to CSV
            var csvData = new StringBuilder();
            csvData.AppendLine("EmployeeId,Name,Department,Skills");

            foreach (var employee in employees)
            {
                var skillNames = employee.Skills?.Count > 0 ? string.Join(" | ", employee.Skills.Select(s => s.Name)) : "No Skills";
                csvData.AppendLine($"{employee.EmployeeId},{employee.Name},{employee.Department},{skillNames}");
            }

            await File.WriteAllTextAsync(filePath, csvData.ToString());

            // Upload file to Google Cloud Storage
            _logger.LogInformation($"Uploading file {fileName} to GCS bucket {_bucketName}");
            using var fileStream = File.OpenRead(filePath);
            await _storageClient.UploadObjectAsync(_bucketName, fileName, "text/csv", fileStream);
            _logger.LogInformation($"Upload successful: {fileName}");

            return $"https://storage.googleapis.com/{_bucketName}/{fileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error uploading employees to GCS: {ex.Message}");
            throw new ApplicationException("Failed to upload employee data to cloud storage.", ex);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation($"Temp file deleted: {filePath}");
            }
        }
    }

    /// <summary>
    /// Uploads Skill Gap Report and sends an email with the blob URL.
    /// </summary>
    public async Task<string> UploadAndSendSkillGapReport(List<Employee> employees, string userEmail)
    {
        try
        {
            _logger.LogInformation("Generating Skill Gap Report...");

            string reportUrl = await UploadEmployeesToBlob(employees);

            await _emailService.SendEmailAsync(userEmail, "Skill Gap Report", 
                $"The Skill Gap Report you requested is ready. Please click the link below to download it:\n\n{reportUrl}");

            _logger.LogInformation($"Skill Gap Report sent to {userEmail}");

            return reportUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to send skill gap report: {ex.Message}");
            throw new ApplicationException("Failed to send skill gap report.", ex);
        }
    }
}
