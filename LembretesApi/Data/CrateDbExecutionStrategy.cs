using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LembretesApi.Data
{
    /// <summary>
    /// Estratégia de execução customizada para CrateDB que não usa transações
    /// CrateDB não suporta transações (BEGIN/COMMIT/ROLLBACK)
    /// </summary>
    public class CrateDbExecutionStrategy : IExecutionStrategy
    {
        private readonly ExecutionStrategyDependencies _dependencies;

        public CrateDbExecutionStrategy(ExecutionStrategyDependencies dependencies)
        {
            _dependencies = dependencies;
        }

        public bool RetriesOnFailure => false;

        public TResult Execute<TState, TResult>(
            TState state,
            Func<DbContext, TState, TResult> operation,
            Func<DbContext, TState, ExecutionResult<TResult>>? verifySucceeded)
        {
            return operation(_dependencies.CurrentContext.Context!, state);
        }

        public Task<TResult> ExecuteAsync<TState, TResult>(
            TState state,
            Func<DbContext, TState, CancellationToken, Task<TResult>> operation,
            Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>>? verifySucceeded,
            CancellationToken cancellationToken = default)
        {
            return operation(_dependencies.CurrentContext.Context!, state, cancellationToken);
        }

        public TResult Execute<TState, TResult>(
            TState state,
            Func<DbContext, TState, TResult> operation,
            Func<DbContext, TState, ExecutionResult<TResult>>? verifySucceeded,
            Func<DbContext, TState, ExecutionResult<TResult>>? verifySucceededOnRetry)
        {
            return operation(_dependencies.CurrentContext.Context!, state);
        }

        public Task<TResult> ExecuteAsync<TState, TResult>(
            TState state,
            Func<DbContext, TState, CancellationToken, Task<TResult>> operation,
            Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>>? verifySucceeded,
            Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>>? verifySucceededOnRetry,
            CancellationToken cancellationToken = default)
        {
            return operation(_dependencies.CurrentContext.Context!, state, cancellationToken);
        }
    }
}
