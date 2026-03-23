namespace HFiles.API.Models;

public class MedicalFile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FileName { get; set; } = "";
    public string FileType { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string? MimeType { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.Now;
    public User? User { get; set; }
}