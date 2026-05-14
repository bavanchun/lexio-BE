using FluentValidation;
using Lexio.SharedKernel.Primitives;
using Mediator;

namespace Lexio.Identity.Application.Common.Behaviors;

/// <summary>
/// Mediator pipeline behaviour that runs all <see cref="IValidator{T}"/> registered for the request.
/// On any failure returns a <see cref="Result"/>/<see cref="Result{T}"/> with the first error so
/// handlers never see invalid input.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async ValueTask<TResponse> Handle(
        TRequest message,
        CancellationToken cancellationToken,
        MessageHandlerDelegate<TRequest, TResponse> next)
    {
        if (!_validators.Any())
        {
            return await next(message, cancellationToken);
        }

        var context = new ValidationContext<TRequest>(message);
        var failures = (await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
        {
            return await next(message, cancellationToken);
        }

        var first = failures[0];
        var error = Error.Validation(
            $"validation.{first.PropertyName.ToLowerInvariant()}",
            first.ErrorMessage);

        var responseType = typeof(TResponse);
        if (responseType == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error);
        }
        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var failureMethod = typeof(Result)
                .GetMethods()
                .First(m => m.Name == nameof(Result.Failure) && m.IsGenericMethod)
                .MakeGenericMethod(responseType.GetGenericArguments()[0]);
            return (TResponse)failureMethod.Invoke(null, [error])!;
        }

        throw new ValidationException(failures);
    }
}
