using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(AppDbContext context, PushNotificationService pushService, ILogger<NotificationsController> logger)
        {
            _context = context;
            _pushService = pushService;
            _logger = logger;
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

                // Validações
                if (dto == null)
                {
                    _logger.LogWarning("⚠️ DTO de subscription é null");
                    return BadRequest(new { message = "Dados de subscription inválidos" });
                }

                if (string.IsNullOrWhiteSpace(dto.Endpoint))
                {
                    _logger.LogWarning("⚠️ Endpoint está vazio");
                    return BadRequest(new { message = "Endpoint é obrigatório" });
                }

                if (dto.Keys == null || string.IsNullOrWhiteSpace(dto.Keys.P256dh) || string.IsNullOrWhiteSpace(dto.Keys.Auth))
                {
                    _logger.LogWarning("⚠️ Chaves de subscription estão vazias ou inválidas");
                    return BadRequest(new { message = "Chaves de subscription são obrigatórias" });
                }

                // Log para debug
                _logger.LogInformation($"📥 Recebendo subscription - UsuarioId: {usuarioId}");
                _logger.LogInformation($"📍 Endpoint: {dto.Endpoint.Substring(0, Math.Min(50, dto.Endpoint.Length))}...");
                _logger.LogInformation($"🔑 P256dh (primeiros 30 chars): {dto.Keys.P256dh.Substring(0, Math.Min(30, dto.Keys.P256dh.Length))}...");
                _logger.LogInformation($"🔐 Auth (primeiros 30 chars): {dto.Keys.Auth.Substring(0, Math.Min(30, dto.Keys.Auth.Length))}...");

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
                _logger.LogInformation($"💾 Subscription salva no banco de dados para usuário {usuarioId}");

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
                _logger.LogInformation($"🧪 Iniciando teste de notificação para usuário: {usuarioId}");

                // Verificar se o usuário tem subscription
                var subscriptions = await _context.PushSubscriptions
                    .Where(ps => ps.UsuarioId == usuarioId)
                    .ToListAsync();

                _logger.LogInformation($"📋 Encontradas {subscriptions.Count} subscription(s) para o usuário {usuarioId}");

                if (!subscriptions.Any())
                {
                    _logger.LogWarning($"⚠️ Nenhuma subscription encontrada para o usuário {usuarioId}");
                    return BadRequest(new { 
                        message = "Você precisa ativar as notificações primeiro. Use o botão 'Ativar Notificações' no frontend." 
                    });
                }

                var title = dto?.Title ?? "🔔 Teste de Notificação";
                var body = dto?.Body ?? "Esta é uma notificação de teste! Se você está vendo isso, as notificações estão funcionando corretamente.";

                _logger.LogInformation($"📤 Enviando notificação de teste - Título: {title}, Corpo: {body}");

                await _pushService.SendNotificationAsync(usuarioId, title, body, dto?.Data);

                _logger.LogInformation($"✅ Notificação de teste enviada com sucesso para o usuário {usuarioId}");

                return Ok(new { 
                    message = "Notificação de teste enviada com sucesso!",
                    title,
                    body
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, $"❌ Erro de configuração ao enviar notificação de teste: {ex.Message}");
                return StatusCode(500, new { 
                    message = "Erro de configuração", 
                    error = ex.Message,
                    details = "Verifique se as chaves VAPID estão configuradas corretamente no appsettings.json"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erro ao enviar notificação de teste: {ex.Message}");
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
