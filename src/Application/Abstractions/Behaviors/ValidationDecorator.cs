using Application.Abstractions.Messaging;
using FluentValidation;
using FluentValidation.Results;
using SharedKernel;

namespace Application.Abstractions.Behaviors
{
    internal static class ValidationDecorator
    {
        internal sealed class CommandHandler<TCommand, TResponse>
            : ICommandHandler<TCommand, TResponse>
            where TCommand : ICommand<TResponse>
        {
            private readonly ICommandHandler<TCommand, TResponse> _innerHandler;
            private readonly IEnumerable<IValidator<TCommand>> _validators;

            public CommandHandler(
                ICommandHandler<TCommand, TResponse> innerHandler,
                IEnumerable<IValidator<TCommand>> validators)
            {
                _innerHandler = innerHandler;
                _validators = validators;
            }

            public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
            {
                ValidationFailure[] validationFailures = await ValidateAsync(command, _validators);

                if (validationFailures.Length == 0)
                {
                    return await _innerHandler.Handle(command, cancellationToken);
                }

                return Result.Failure<TResponse>(CreateValidationError(validationFailures));
            }
        }

        internal sealed class CommandBaseHandler<TCommand>
            : ICommandHandler<TCommand>
            where TCommand : ICommand
        {
            private readonly ICommandHandler<TCommand> _innerHandler;
            private readonly IEnumerable<IValidator<TCommand>> _validators;

            public CommandBaseHandler(
                ICommandHandler<TCommand> innerHandler,
                IEnumerable<IValidator<TCommand>> validators)
            {
                _validators = validators;
                _innerHandler = innerHandler;
            }

            public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
            {
                ValidationFailure[] validationFailures = await ValidateAsync(command, _validators);

                if (validationFailures.Length == 0)
                {
                    return await _innerHandler.Handle(command, cancellationToken);
                }

                return Result.Failure(CreateValidationError(validationFailures));
            }
        }

        private static async Task<ValidationFailure[]> ValidateAsync<TCommand>(
            TCommand command,
            IEnumerable<IValidator<TCommand>> validators)
        {
            if (!validators.Any())
            {
                return Array.Empty<ValidationFailure>();;
            }

            var context = new ValidationContext<TCommand>(command);

            ValidationResult[] validationResults = await Task.WhenAll(
                validators.Select(validator => validator.ValidateAsync(context)));

            ValidationFailure[] validationFailures = validationResults
                .Where(validationResult => !validationResult.IsValid)
                .SelectMany(validationResult => validationResult.Errors)
                .ToArray();

            return validationFailures;
        }

        private static ValidationError CreateValidationError(ValidationFailure[] validationFailures) =>
            new(validationFailures.Select(f => Error.Problem(f.ErrorCode, f.ErrorMessage)).ToArray());
    }
}
