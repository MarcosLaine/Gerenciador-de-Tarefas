using System;
using TimeZoneConverter;

namespace LembretesApi.Services
{
    public class TimezoneService
    {

        /// <summary>
        /// Converte UTC para o timezone do usuário
        /// </summary>
        public DateTime ConvertFromUtc(DateTime utcDateTime, string userTimezone)
        {
            try
            {
                var timeZoneInfo = GetTimeZoneInfo(userTimezone);
                return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZoneInfo);
            }
            catch
            {
                // Se falhar, usar timezone padrão (SP)
                var defaultTimeZone = GetTimeZoneInfo("America/Sao_Paulo");
                return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, defaultTimeZone);
            }
        }

        /// <summary>
        /// Converte do timezone do usuário para UTC
        /// </summary>
        public DateTime ConvertToUtc(DateTime localDateTime, string userTimezone)
        {
            try
            {
                var timeZoneInfo = GetTimeZoneInfo(userTimezone);
                return TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZoneInfo);
            }
            catch
            {
                // Se falhar, usar timezone padrão (SP)
                var defaultTimeZone = GetTimeZoneInfo("America/Sao_Paulo");
                return TimeZoneInfo.ConvertTimeToUtc(localDateTime, defaultTimeZone);
            }
        }

        /// <summary>
        /// Obtém o TimeZoneInfo baseado no nome do timezone (IANA ou Windows)
        /// </summary>
        private TimeZoneInfo GetTimeZoneInfo(string timezone)
        {
            try
            {
                // Tentar usar TimeZoneConverter para converter IANA para Windows
                if (TZConvert.TryGetTimeZoneInfo(timezone, out var timeZoneInfo))
                {
                    return timeZoneInfo;
                }

                // Se não conseguir converter, tentar usar diretamente (pode ser um ID do Windows)
                return TimeZoneInfo.FindSystemTimeZoneById(timezone);
            }
            catch
            {
                // Último fallback: São Paulo
                try
                {
                    return TZConvert.GetTimeZoneInfo("America/Sao_Paulo");
                }
                catch
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
                }
            }
        }

        /// <summary>
        /// Obtém o offset UTC do timezone em horas
        /// </summary>
        public int GetUtcOffset(string userTimezone)
        {
            try
            {
                var timeZoneInfo = GetTimeZoneInfo(userTimezone);
                return (int)timeZoneInfo.GetUtcOffset(DateTime.UtcNow).TotalHours;
            }
            catch
            {
                // Fallback: UTC-3 (São Paulo)
                return -3;
            }
        }
    }
}
