using Microsoft.AspNetCore.Mvc;
using LasanthaPOS.API.Data;
using LasanthaPOS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LasanthaPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    public AuthController(AppDbContext db) => _db = db;

    public record LoginRequest(string Username, string Password);

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid username or password." });

        return Ok(new
        {
            user.Id,
            user.Username,
            user.FullName,
            user.Role
        });
    }
}
