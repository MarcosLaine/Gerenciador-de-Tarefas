using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuario = new Usuario
            {
                Nome = dto.Nome,
                UserName = dto.Email,
                Email = dto.Email,
                DataCriacao = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(usuario, dto.Senha);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return BadRequest(ModelState);
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

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuario = await _userManager.FindByEmailAsync(dto.Email);

            if (usuario == null)
                return Unauthorized(new { message = "Email ou senha inválidos" });

            var result = await _signInManager.CheckPasswordSignInAsync(usuario, dto.Senha, false);

            if (!result.Succeeded)
                return Unauthorized(new { message = "Email ou senha inválidos" });

            var token = _tokenService.GerarToken(usuario);

            return Ok(new AuthResponseDto
            {
                Token = token,
                Email = usuario.Email ?? string.Empty,
                Nome = usuario.Nome ?? string.Empty,
                Expiracao = DateTime.UtcNow.AddDays(7)
            });
        }
    }
}

