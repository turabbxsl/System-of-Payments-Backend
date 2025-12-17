namespace Payments.Common.Models
{
    public class Result<T>
    {
        private readonly T? _value;

        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public Error Error { get; }

        protected Result(T? value, bool isSuccess, Error error)
        {
            if (isSuccess && error != Error.None)
                throw new InvalidOperationException("Success result cannot contain an error.");

            if (!isSuccess && error == Error.None)
                throw new InvalidOperationException("Failure result must contain an error.");

            IsSuccess = isSuccess;
            Error = error;
            _value = value;
        }

        public static Result<T> Success(T value) => new(value, true, Error.None);
        public static Result<T> Failure(Error error) => new(default, false, error);

        public T Value => IsSuccess
            ? _value!
            : throw new InvalidOperationException("Cannot access the value of a failed result.");

        public static implicit operator Result<T>(T value) => Success(value);
        public static implicit operator Result<T>(Error error) => new(default, false, error);

    }

    public static class Result
    {
        public static Result<T> Success<T>(T value) => Result<T>.Success(value);
        public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);
    }

    public sealed record Error(string Code, string Message)
    {
        public static readonly Error None = new(string.Empty, string.Empty);
        public static readonly Error Failure = new("General.Failure", "An unexpected or general failure occurred.");
        public static readonly Error NotFound = new("General.NotFound", "The requested resource was not found.");
        public static readonly Error NullValue = new("General.NullValue", "Null value was provided where a non-null value was required.");
        public static readonly Error Unauthorized = new("General.Unauthorized", "Access is denied due to invalid credentials.");
        public static readonly Error Forbidden = new("General.Forbidden", "Access is denied, insufficient permissions.");

        public static readonly Error ValidationFailed = new("Validation.Failed", "One or more validation errors occurred.");
        public static readonly Error InvalidInput = new("Validation.InvalidInput", "The input data provided is invalid or malformed.");
        public static readonly Error EmptyPaymentId = new ("Validation.EmptyPaymentId", "The payment Id is empty.");

        public static readonly Error ConcurrencyConflict = new("DB.Concurrency", "The record was modified or deleted by another operation.");
        public static readonly Error DatabaseFailure = new("DB.ConnectionFailed", "Could not establish connection to the database.");
        public static readonly Error UpdateFailed = new("DB.UpdateFailed", "Failed to persist changes to the database.");
        public static readonly Error UniqueConstraintViolated = new("DB.UniqueConstraint", "A unique constraint violation occurred.");

        public static readonly Error InvalidState = new("Business.InvalidState", "The operation is not allowed in the current state of the entity.");
        public static readonly Error InsufficientFunds = new("Business.InsufficientFunds", "The account does not have enough balance for this transaction.");
        public static readonly Error ExternalServiceFailure = new("Business.ExternalService", "A required external service is unavailable or failed.");

        public static readonly Error SerializationError = new("Messaging.Serialization", "Failed to serialize/deserialize the message payload.");
        public static readonly Error PublisherFailure = new("Messaging.PublisherFailed", "Failed to publish the event to the message broker (RabbitMQ).");
        public static readonly Error BrokerConnectionFailure = new("Messaging.BrokerConnection", "Failed to connect to the message broker.");
        public static readonly Error MaxRetriesExceeded = new("Messaging.MaxRetries", "The message processing failed after maximum allowed retries.");
    }
}
