namespace HFiles.API.Models;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Gender { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string? AvatarPath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<MedicalFile> Files { get; set; } = new();
}