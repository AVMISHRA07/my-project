using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HFiles.API.Data;
using System.Security.Claims;

namespace HFiles.API.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ProfileController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var user = await _db.Users.FindAsync(UserId);
        if (user == null) return NotFound();
        return Ok(new { user.Id, user.FullName, user.Email, user.Phone, user.Gender, user.AvatarPath });
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateDto dto)
    {
        var user = await _db.Users.FindAsync(UserId);
        if (user == null) return NotFound();

        user.Email = dto.Email ?? user.Email;
        user.Phone = dto.Phone ?? user.Phone;
        user.Gender = dto.Gender ?? user.Gender;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Profile updated" });
    }

    [HttpPost("avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        if (file == null) return BadRequest(new { message = "No file provided" });

        var user = await _db.Users.FindAsync(UserId);
        if (user == null) return NotFound();

        var folder = Path.Combine(_env.ContentRootPath, "Uploads", "avatars");
        Directory.CreateDirectory(folder);
        var fileName = $"{UserId}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var path = Path.Combine(folder, fileName);

        using var stream = System.IO.File.Create(path);
        await file.CopyToAsync(stream);

        user.AvatarPath = $"/uploads/avatars/{fileName}";
        await _db.SaveChangesAsync();

        return Ok(new { avatarPath = user.AvatarPath });
    }
}

public record UpdateDto(string? Email, string? Phone, string? Gender);