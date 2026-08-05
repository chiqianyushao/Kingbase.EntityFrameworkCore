using Kingbase.EntityFrameworkCore.Infrastructure.Internal;
using Kdbndp;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kingbase.EntityFrameworkCore.Storage.Internal;

public sealed class KingbaseExecutionStrategy : ExecutionStrategy
{
    private readonly ISet<string> _additionalTransientErrorCodes;

    public KingbaseExecutionStrategy(
        ExecutionStrategyDependencies dependencies,
        int maxRetryCount,
        TimeSpan maxRetryDelay,
        IEnumerable<string>? additionalTransientErrorCodes = null)
        : base(dependencies, maxRetryCount, maxRetryDelay)
        => _additionalTransientErrorCodes = new HashSet<string>(
            additionalTransientErrorCodes ?? Array.Empty<string>(),
            StringComparer.Ordinal);

    protected override bool ShouldRetryOn(Exception exception)
        => exception switch
        {
            KingbaseException kingbaseException => kingbaseException.IsTransient
                || _additionalTransientErrorCodes.Contains(kingbaseException.SqlState),
            KdbndpException kdbndpException => kdbndpException.IsTransient,
            TimeoutException => true,
            _ when exception.InnerException is not null => ShouldRetryOn(exception.InnerException),
            _ => false
        };
}

public sealed class KingbaseExecutionStrategyFactory(ExecutionStrategyDependencies dependencies)
    : IExecutionStrategyFactory
{
    public IExecutionStrategy Create()
    {
        var extension = dependencies.Options.FindExtension<KingbaseOptionsExtension>();
        return extension?.MaxRetryCount is int maxRetryCount
            ? new KingbaseExecutionStrategy(
                dependencies,
                maxRetryCount,
                extension.MaxRetryDelay ?? TimeSpan.FromSeconds(30),
                extension.AdditionalTransientErrorCodes)
            : new NonRetryingExecutionStrategy(dependencies);
    }
}
