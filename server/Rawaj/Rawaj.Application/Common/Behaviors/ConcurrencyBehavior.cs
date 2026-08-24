using MediatR;
using Microsoft.EntityFrameworkCore;
using Rawaj.Application.Common.Exceptions;

namespace Rawaj.Application.Common.Behaviors;

/// <summary>
/// Translates a raw <c>DbUpdateConcurrencyException</c> (thrown by SaveChangesAsync when a
/// RowVersion-tracked entity was changed since it was loaded) into a <see cref="ConcurrencyConflictException"/>
/// with a clear Arabic message, so every command gets consistent conflict handling without each
/// handler needing its own try/catch.
/// </summary>
public class ConcurrencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException();
        }
    }
}
