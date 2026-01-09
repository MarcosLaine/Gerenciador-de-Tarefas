using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LembretesApi.Data;
using LembretesApi.DTOs;
using LembretesApi.Models;
using LembretesApi.Services;
using System.Security.Claims;

namespace LembretesApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly PushNotificationService _pushService;

        public NotificationsController(AppDbContext context, PushNotificationService pushService)
        {
            _context = context;
            _pushService = pushService;
        }

        private string ObterUsuarioId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) 
                ?? throw new UnauthorizedAccessException("Usuário não autenticado");
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionDto dto)
        {
            try
            {
                var usuarioId = ObterUsuarioId();

                // Log para debug
                _logger.LogInformation($"📥 Recebendo subscription - UsuarioId: {usuarioId}, Endpoint: {dto.Endpoint?.Substring(0, Math.Min(50, dto.Endpoint?.Length ?? 0))}...");

                // Verificar se já existe subscription para este usuário
                var existingSubscription = await _context.PushSubscriptions
                    .FirstOrDefaultAsync(ps => ps.UsuarioId == usuarioId && ps.Endpoint == dto.Endpoint);

                if (existingSubscription != null)
                {
                    // Atualizar subscription existente
                    existingSubscription.P256dh = dto.Keys.P256dh;
                    existingSubscription.Auth = dto.Keys.Auth;
                    existingSubscription.DataCriacao = DateTime.UtcNow;
                    _logger.LogInformation($"🔄 Subscription atualizada para usuário {usuarioId}");
                }
                else
                {
                    // Criar nova subscription
                    var subscription = new PushSubscription
                    {
                        UsuarioId = usuarioId,
                        Endpoint = dto.Endpoint,
                        P256dh = dto.Keys.P256dh,
                        Auth = dto.Keys.Auth,
                        DataCriacao = DateTime.UtcNow
                    };

                    _context.PushSubscriptions.Add(subscription);
                    _logger.LogInformation($"✅ Nova subscription criada para usuário {usuarioId}");
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Subscription registrada com sucesso" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erro ao registrar subscription: {ex.Message}");
                return StatusCode(500, new { 
                    message = "Erro ao registrar subscription", 
                    error = ex.Message 
                });
            }
        }

        [HttpPost("unsubscribe")]
        public async Task<IActionResult> Unsubscribe()
        {
            try
            {
                var usuarioId = ObterUsuarioId();

                var subscriptions = await _context.PushSubscriptions
                    .Where(ps => ps.UsuarioId == usuarioId)
                    .ToListAsync();

                if (subscriptions.Any())
                {
                    _context.PushSubscriptions.RemoveRange(subscriptions);
                    await _context.SaveChangesAsync();
                }

                return Ok(new { message = "Subscription removida com sucesso" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Erro ao remover subscription", 
                    error = ex.Message 
                });
            }
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                var usuarioId = ObterUsuarioId();

                var subscription = await _context.PushSubscriptions
                    .FirstOrDefaultAsync(ps => ps.UsuarioId == usuarioId);

                return Ok(new { 
                    subscribed = subscription != null,
                    endpoint = subscription?.Endpoint 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Erro ao verificar status", 
                    error = ex.Message 
                });
            }
        }

        [HttpPost("test")]
        public async Task<IActionResult> SendTestNotification([FromBody] TestNotificationDto? dto = null)
        {
            try
            {
                var usuarioId = ObterUsuarioId();

                // Verificar se o usuário tem subscription
                var subscription = await _context.PushSubscriptions
                    .FirstOrDefaultAsync(ps => ps.UsuarioId == usuarioId);

                if (subscription == null)
                {
                    return BadRequest(new { 
                        message = "Você precisa ativar as notificações primeiro. Use o botão 'Ativar Notificações' no frontend." 
                    });
                }

                var title = dto?.Title ?? "🔔 Teste de Notificação";
                var body = dto?.Body ?? "Esta é uma notificação de teste! Se você está vendo isso, as notificações estão funcionando corretamente.";

                await _pushService.SendNotificationAsync(usuarioId, title, body, dto?.Data);

                return Ok(new { 
                    message = "Notificação de teste enviada com sucesso!",
                    title,
                    body
                });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, new { 
                    message = "Erro de configuração", 
                    error = ex.Message,
                    details = "Verifique se as chaves VAPID estão configuradas corretamente no appsettings.json"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Erro ao enviar notificação de teste", 
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
    }

    public class TestNotificationDto
    {
        public string? Title { get; set; }
        public string? Body { get; set; }
        public object? Data { get; set; }
    }
}
