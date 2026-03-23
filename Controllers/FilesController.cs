using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HFiles.API.Data;
using HFiles.API.Models;
using System.Security.Claims;

namespace HFiles.API.Controllers;

[ApiController]
[Route("api/files")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    private static readonly string[] AllowedTypes = { "application/pdf", "image/png", "image/jpeg", "image/webp" };
    private static readonly string[] AllowedFileTypes = { "Lab Report", "Prescription", "X-Ray", "Blood Report", "MRI Scan", "CT Scan" };

    public FilesController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var files = await _db.MedicalFiles
            .Where(f => f.UserId == UserId)
            .OrderByDescending(f => f.UploadedAt)
            .Select(f => new { f.Id, f.FileName, f.FileType, f.MimeType, f.FilePath, f.UploadedAt })
            .ToListAsync();
        return Ok(files);
    }

    [HttpPost]
    public async Task<IActionResult> Upload([FromForm] string fileName, [FromForm] string fileType, IFormFile file)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return BadRequest(new { message = "File name required" });
        if (!AllowedFileTypes.Contains(fileType)) return BadRequest(new { message = "Invalid file type" });
        if (file == null) return BadRequest(new { message = "No file provided" });
        if (!AllowedTypes.Contains(file.ContentType)) return BadRequest(new { message = "Only PDF, PNG, JPG allowed" });
        if (file.Length > 10 * 1024 * 1024) return BadRequest(new { message = "File must be under 10 MB" });

        var folder = Path.Combine(_env.ContentRootPath, "Uploads", "medical");
        Directory.CreateDirectory(folder);
        var storedName = $"{UserId}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var path = Path.Combine(folder, storedName);

        using var stream = System.IO.File.Create(path);
        await file.CopyToAsync(stream);

        var record = new MedicalFile
        {
            UserId = UserId,
            FileName = fileName,
            FileType = fileType,
            FilePath = $"/uploads/medical/{storedName}",
            MimeType = file.ContentType
        };

        _db.MedicalFiles.Add(record);
        await _db.SaveChangesAsync();

        return Ok(new { message = "File uploaded", file = new { record.Id, record.FileName, record.FileType, record.FilePath } });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var file = await _db.MedicalFiles.FirstOrDefaultAsync(f => f.Id == id && f.UserId == UserId);
        if (file == null) return NotFound(new { message = "File not found" });

        var fullPath = Path.Combine(_env.ContentRootPath, file.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);

        _db.MedicalFiles.Remove(file);
        await _db.SaveChangesAsync();

        return Ok(new { message = "File deleted" });
    }
}