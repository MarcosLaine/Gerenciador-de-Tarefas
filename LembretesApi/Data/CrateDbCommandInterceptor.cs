using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using Npgsql;

namespace LembretesApi.Data
{
    /// <summary>
    /// Interceptor de comandos SQL para CrateDB que remove comandos de transação
    /// CrateDB não suporta transações (BEGIN/COMMIT/ROLLBACK)
    /// </summary>
    public class CrateDbCommandInterceptor : DbCommandInterceptor
    {
        private static bool IsTransactionCommand(string? commandText)
        {
            if (string.IsNullOrWhiteSpace(commandText))
                return false;

            var trimmed = commandText.Trim();
            return trimmed.StartsWith("ROLLBACK", StringComparison.OrdinalIgnoreCase) ||
                   trimmed.StartsWith("COMMIT", StringComparison.OrdinalIgnoreCase) ||
                   trimmed.StartsWith("BEGIN", StringComparison.OrdinalIgnoreCase) ||
                   trimmed.StartsWith("SAVEPOINT", StringComparison.OrdinalIgnoreCase) ||
                   trimmed.StartsWith("RELEASE", StringComparison.OrdinalIgnoreCase);
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            // Remove comandos de transação
            if (IsTransactionCommand(command.CommandText))
            {
                // Substitui por um comando vazio que não faz nada
                command.CommandText = "SELECT 1";
            }

            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            // Remove comandos de transação
            if (IsTransactionCommand(command.CommandText))
            {
                // Substitui por um comando vazio que não faz nada
                command.CommandText = "SELECT 1";
            }

            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override InterceptionResult<int> NonQueryExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result)
        {
            // Remove comandos de transação
            if (IsTransactionCommand(command.CommandText))
            {
                // Substitui por um comando vazio que não faz nada
                command.CommandText = "SELECT 1";
            }

            return base.NonQueryExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            // Remove comandos de transação
            if (IsTransactionCommand(command.CommandText))
            {
                // Substitui por um comando vazio que não faz nada
                command.CommandText = "SELECT 1";
            }

            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
