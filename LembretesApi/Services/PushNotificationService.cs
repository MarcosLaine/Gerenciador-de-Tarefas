using Microsoft.EntityFrameworkCore;
using LembretesApi.Data;
using LembretesApi.Models;
using WebPush;
using System.Text.Json;

namespace LembretesApi.Services
{
    public class PushNotificationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PushNotificationService> _logger;
        private readonly string _vapidPublicKey;
        private readonly string _vapidPrivateKey;
        private readonly string _vapidSubject;

        public PushNotificationService(
            IServiceProvider serviceProvider,
            ILogger<PushNotificationService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;

            // Obter chaves VAPID das variáveis de ambiente ou configuração
            _vapidPublicKey = Environment.GetEnvironmentVariable("VAPID_PUBLIC_KEY")
                ?? configuration["VAPID:PublicKey"]
                ?? throw new InvalidOperationException("VAPID_PUBLIC_KEY não configurada");

            _vapidPrivateKey = Environment.GetEnvironmentVariable("VAPID_PRIVATE_KEY")
                ?? configuration["VAPID:PrivateKey"]
                ?? throw new InvalidOperationException("VAPID_PRIVATE_KEY não configurada");

            var subjectValue = Environment.GetEnvironmentVariable("VAPID_SUBJECT")
                ?? configuration["VAPID:Subject"]
                ?? "mailto:admin@lembretes.com";
            
            // Validar e garantir que o subject está no formato correto (mailto: ou https://)
            if (string.IsNullOrWhiteSpace(subjectValue))
            {
                subjectValue = "mailto:admin@lembretes.com";
            }
            else
            {
                subjectValue = subjectValue.Trim();
                
                // Se não começar com mailto: ou http(s)://, adicionar mailto:
                if (!subjectValue.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) && 
                    !subjectValue.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                    !subjectValue.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                {
                    // Se contém @, provavelmente é um email, adicionar mailto:
                    if (subjectValue.Contains("@"))
                    {
                        subjectValue = $"mailto:{subjectValue}";
                    }
                    else
                    {
                        throw new InvalidOperationException($"VAPID_SUBJECT inválido: '{subjectValue}'. Deve ser um endereço mailto: (ex: mailto:email@exemplo.com) ou uma URL válida (ex: https://exemplo.com).");
                    }
                }
            }
            
            _vapidSubject = subjectValue;
            _logger.LogInformation($"VAPID Subject configurado: {_vapidSubject}");
        }

        public async Task SendNotificationAsync(string usuarioId, string title, string body, object? data = null)
        {
            try
            {
                _logger.LogInformation($"📨 Iniciando envio de notificação para usuário {usuarioId}");

                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Buscar todas as subscriptions do usuário
                var subscriptions = await context.PushSubscriptions
                    .Where(ps => ps.UsuarioId == usuarioId)
                    .ToListAsync();

                _logger.LogInformation($"🔍 Encontradas {subscriptions.Count} subscription(s) para o usuário {usuarioId}");

                if (!subscriptions.Any())
                {
                    _logger.LogWarning($"⚠️ Nenhuma subscription encontrada para o usuário {usuarioId}");
                    return;
                }

                // Validar chaves VAPID antes de usar
                if (string.IsNullOrWhiteSpace(_vapidPublicKey) || string.IsNullOrWhiteSpace(_vapidPrivateKey))
                {
                    _logger.LogError("❌ Chaves VAPID não configuradas corretamente");
                    throw new InvalidOperationException("Chaves VAPID não configuradas");
                }

                var pushClient = new WebPushClient();

                // Preparar payload da notificação
                var payload = JsonSerializer.Serialize(new
                {
                    title,
                    body,
                    icon = "/icon-192x192.png",
                    badge = "/icon-192x192.png",
                    tag = "lembrete-notification",
                    data = data ?? new { }
                });

                _logger.LogInformation($"📦 Payload preparado: {payload.Substring(0, Math.Min(100, payload.Length))}...");

                var successCount = 0;
                var errorCount = 0;

                // Enviar notificação para cada subscription
                foreach (var subscription in subscriptions)
                {
                    try
                    {
                        _logger.LogInformation($"📤 Tentando enviar para endpoint: {subscription.Endpoint.Substring(0, Math.Min(50, subscription.Endpoint.Length))}...");
                        _logger.LogInformation($"🔑 P256dh: {subscription.P256dh.Substring(0, Math.Min(20, subscription.P256dh.Length))}..., Auth: {subscription.Auth.Substring(0, Math.Min(20, subscription.Auth.Length))}...");

                        var pushSubscription = new WebPush.PushSubscription(
                            subscription.Endpoint,
                            subscription.P256dh,
                            subscription.Auth
                        );

                        var vapidDetails = new VapidDetails(
                            _vapidSubject,
                            _vapidPublicKey,
                            _vapidPrivateKey
                        );

                        _logger.LogInformation($"🔐 VAPID Details - Subject: {_vapidSubject}, PublicKey: {_vapidPublicKey.Substring(0, Math.Min(20, _vapidPublicKey.Length))}...");

                        await pushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails);
                        _logger.LogInformation($"✅ Notificação enviada com sucesso para {subscription.Endpoint.Substring(0, Math.Min(50, subscription.Endpoint.Length))}...");
                        successCount++;
                    }
                    catch (WebPushException ex)
                    {
                        errorCount++;
                        _logger.LogWarning(ex, $"⚠️ Erro ao enviar notificação para {subscription.Endpoint.Substring(0, Math.Min(50, subscription.Endpoint.Length))}...: {ex.Message} (Status: {ex.StatusCode})");

                        // Se a subscription expirou ou é inválida, remover do banco
                        if (ex.StatusCode == System.Net.HttpStatusCode.Gone ||
                            ex.StatusCode == System.Net.HttpStatusCode.NotFound ||
                            ex.StatusCode == System.Net.HttpStatusCode.BadRequest ||
                            ex.StatusCode == System.Net.HttpStatusCode.Unauthorized) // 401 indica subscription inválida ou chaves VAPID incorretas
                        {
                            _logger.LogInformation($"🗑️ Removendo subscription inválida/expirada (Status {ex.StatusCode}): {subscription.Endpoint.Substring(0, Math.Min(50, subscription.Endpoint.Length))}...");
                            
                            if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                            {
                                _logger.LogWarning("⚠️ Erro 401: Subscription foi criada com chaves VAPID diferentes. O usuário precisa reativar as notificações.");
                            }
                            
                            context.PushSubscriptions.Remove(subscription);
                            await context.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        _logger.LogError(ex, $"❌ Erro inesperado ao enviar notificação para {subscription.Endpoint.Substring(0, Math.Min(50, subscription.Endpoint.Length))}...");
                    }
                }

                _logger.LogInformation($"📊 Resultado do envio: {successCount} sucesso(s), {errorCount} erro(s)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erro ao enviar notificação para usuário {usuarioId}: {ex.Message}");
                throw; // Re-throw para que o controller possa tratar
            }
        }

        public async Task SendReminderNotificationAsync(Lembrete lembrete)
        {
            var horarioStr = lembrete.Horario.HasValue
                ? lembrete.Horario.Value.ToString(@"hh\:mm")
                : "";

            var title = "🔔 Lembrete";
            var body = lembrete.Horario.HasValue
                ? $"{lembrete.Nome ?? "Lembrete"} - {horarioStr}"
                : lembrete.Nome ?? "Lembrete";

            if (!string.IsNullOrEmpty(lembrete.Descricao))
            {
                body += $"\n{lembrete.Descricao}";
            }

            await SendNotificationAsync(
                lembrete.UsuarioId,
                title,
                body,
                new
                {
                    lembreteId = lembrete.Id,
                    nome = lembrete.Nome ?? "Lembrete",
                    data = lembrete.Data.ToString("yyyy-MM-dd"),
                    horario = horarioStr
                }
            );
        }
    }
}
