using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using LembretesApi.Models;
using LembretesApi.DTOs;
using LembretesApi.Services;

namespace LembretesApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly SignInManager<Usuario> _signInManager;
        private readonly TokenService _tokenService;

        public AuthController(
            UserManager<Usuario> userManager,
            SignInManager<Usuario> signInManager,
            TokenService tokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .SelectMany(x => x.Value!.Errors.Select(e => e.ErrorMessage))
                        .ToList();
                    return BadRequest(new { message = string.Join(", ", errors), errors });
                }

                var usuario = new Usuario
                {
                    Nome = dto.Nome,
                    UserName = dto.Email,
                    Email = dto.Email,
                    DataCriacao = DateTime.UtcNow,
                    Timezone = !string.IsNullOrEmpty(dto.Timezone) ? dto.Timezone : "America/Sao_Paulo"
                };

                var result = await _userManager.CreateAsync(usuario, dto.Senha);

                if (!result.Succeeded)
                {
                    var errorMessages = result.Errors.Select(e => e.Description).ToList();
                    return BadRequest(new { message = string.Join(", ", errorMessages), errors = errorMessages });
                }

                var token = _tokenService.GerarToken(usuario);

                return Ok(new AuthResponseDto
                {
                    Token = token,
                    Email = usuario.Email ?? string.Empty,
                    Nome = usuario.Nome ?? string.Empty,
                    Expiracao = DateTime.UtcNow.AddDays(7)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor", error = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .SelectMany(x => x.Value!.Errors.Select(e => e.ErrorMessage))
                        .ToList();
                    return BadRequest(new { message = string.Join(", ", errors), errors });
                }

                var usuario = await _userManager.FindByEmailAsync(dto.Email);

                if (usuario == null)
                    return Unauthorized(new { message = "Email ou senha inválidos" });

                var result = await _signInManager.CheckPasswordSignInAsync(usuario, dto.Senha, false);

                if (!result.Succeeded)
                    return Unauthorized(new { message = "Email ou senha inválidos" });

                // Atualizar timezone se fornecido e diferente do atual
                if (!string.IsNullOrEmpty(dto.Timezone) && usuario.Timezone != dto.Timezone)
                {
                    usuario.Timezone = dto.Timezone;
                    await _userManager.UpdateAsync(usuario);
                }

                var token = _tokenService.GerarToken(usuario);

                return Ok(new AuthResponseDto
                {
                    Token = token,
                    Email = usuario.Email ?? string.Empty,
                    Nome = usuario.Nome ?? string.Empty,
                    Expiracao = DateTime.UtcNow.AddDays(7)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor", error = ex.Message });
            }
        }
    }
}

