using DashboardVendas.Api.Data;
using DashboardVendas.Api.Dtos;
using DashboardVendas.Api.Models;
using DashboardVendas.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DashboardVendas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly TokenService _tokenService;

    public AuthController(AppDbContext context, TokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var emailExiste = await _context.Users.AnyAsync(u => u.Email == dto.Email);

        if (emailExiste)
            return BadRequest(new { message = "E-mail já cadastrado." });

        var user = new User
        {
            Nome = dto.Nome,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Senha)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Usuário cadastrado com sucesso." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null)
            return Unauthorized(new { message = "Credenciais inválidas." });

        var senhaValida = BCrypt.Net.BCrypt.Verify(dto.Senha, user.PasswordHash);

        if (!senhaValida)
            return Unauthorized(new { message = "Credenciais inválidas." });

        var token = _tokenService.GenerateToken(user);

        return Ok(new
        {
            token,
            nome = user.Nome,
            email = user.Email
        });
    }
}