using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Common
{
    public class Result
    {
        public bool IsSuccessful { get; private set; }
        public string? Message { get; private set; }
        public string? ErrorCode { get; private set; }
        public IDictionary<string, string[]>? ValidationErrors { get; private set; }

        public bool IsFailure => !IsSuccessful;

        protected Result(bool isSuccessful, string? message = null, string? errorCode = null, IDictionary<string, string[]>? validationErrors = null)
        {
            IsSuccessful = isSuccessful;
            Message = message;
            ErrorCode = errorCode;
            ValidationErrors = validationErrors;
        }

        public static Result Success(string? message = null)
        {
            return new Result(true, message);
        }

        public static Result Failure(Error error)
        {
            if (error.IsNone)
            {
                throw new InvalidOperationException("Não é permitido criar uma falha com Error.None.");
            }

            return new Result(false, error.Message, error.Code, error.ValidationErrors);
        }

        public static implicit operator Result(Error error) => Failure(error);
    }

    public class Result<T> : Result
    {
        public Error Error { get; }
        public T? Value { get; private set; }

        protected Result(bool isSuccessful, T? value, Error error, string? message = null, string? errorCode = null, IDictionary<string, string[]>? validationErrors = null)
            : base(isSuccessful, message ?? error.Message, error.Code, validationErrors ?? error.ValidationErrors)
        {
            Value = value;
            Error = error;
        }

        public static Result<T> Success(T value, string? message = null)
        {
            return new Result<T>(true, value, Error.None(), message);
        }

        public static Result<T> Failure(Error error)
        {
            return new Result<T>(false, default(T), error);
        }

        public static implicit operator Result<T>(T value) => Success(value);

        public static implicit operator Result<T>(Error error)
        {
            if (error.IsNone)
            {
                return Success(default!);
            }

            return Failure(error);
        }
    }
}
