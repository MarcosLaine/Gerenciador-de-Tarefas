using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using LembretesApi.Data;
using LembretesApi.Models;

namespace LembretesApi.Services
{
    public class ReminderNotificationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReminderNotificationBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); // Verificar a cada minuto

        public ReminderNotificationBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<ReminderNotificationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Serviço de notificações de lembretes iniciado");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndSendNotificationsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao verificar e enviar notificações");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task CheckAndSendNotificationsAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                // Verificar se o banco está disponível antes de tentar acessar
                if (!await context.Database.CanConnectAsync())
                {
                    _logger.LogWarning("Banco de dados não está disponível. Pulando verificação de notificações.");
                    return;
                }
                
                var pushService = scope.ServiceProvider.GetRequiredService<PushNotificationService>();
                var timezoneService = scope.ServiceProvider.GetRequiredService<TimezoneService>();

                var agoraUtc = DateTime.UtcNow;

                // Buscar todos os lembretes não concluídos que podem precisar de notificação
                // Vamos verificar cada um individualmente considerando o timezone do usuário
                var lembretesParaVerificar = await context.Lembretes
                    .Include(l => l.Usuario)
                    .Where(l => !l.Concluido)
                    .ToListAsync();

            foreach (var lembrete in lembretesParaVerificar)
            {
                try
                {
                    // Obter timezone do usuário (padrão: São Paulo)
                    var userTimezone = lembrete.Usuario?.Timezone ?? "America/Sao_Paulo";
                    
                    // Converter data do lembrete (UTC) para o timezone do usuário
                    var dataLembreteLocal = timezoneService.ConvertFromUtc(lembrete.Data, userTimezone);
                    var agoraLocal = timezoneService.ConvertFromUtc(agoraUtc, userTimezone);

                    // Verificar se o horário específico está próximo (se houver horário)
                    if (lembrete.Horario.HasValue)
                    {
                        var horarioLembrete = dataLembreteLocal.Date.Add(lembrete.Horario.Value);
                        var diferenca = (horarioLembrete - agoraLocal).TotalMinutes;

                        // Notificar se estiver entre agora e 5 minutos no futuro (no timezone do usuário)
                        if (diferenca >= 0 && diferenca <= 5)
                        {
                            await pushService.SendReminderNotificationAsync(lembrete);
                            _logger.LogInformation($"Notificação enviada para lembrete {lembrete.Id} - {lembrete.Nome} (timezone: {userTimezone})");
                        }
                    }
                    else
                    {
                        // Sem horário específico, notificar quando a data estiver próxima
                        var dataLembrete = dataLembreteLocal.Date;
                        var hoje = agoraLocal.Date;
                        var diferencaDias = (dataLembrete - hoje).TotalDays;

                        // Notificar no dia do lembrete (no timezone do usuário)
                        if (diferencaDias == 0)
                        {
                            await pushService.SendReminderNotificationAsync(lembrete);
                            _logger.LogInformation($"Notificação enviada para lembrete {lembrete.Id} - {lembrete.Nome} (timezone: {userTimezone})");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Erro ao enviar notificação para lembrete {lembrete.Id}");
                }
            }
            }
            catch (Npgsql.NpgsqlException ex)
            {
                // Erro de conexão PostgreSQL - não logar como erro crítico, apenas warning
                _logger.LogWarning(ex, "Erro de conexão PostgreSQL. Verifique se o banco está rodando.");
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Erro de banco de dados. Banco pode não estar disponível.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao verificar notificações");
            }
        }
    }
}
