using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Common
{
    public record struct Error
    {
        public ErrorType Type { get; init; }
        public string Code { get; init; }
        public string Message { get; init; }
        public IDictionary<string, string[]>? ValidationErrors { get; init; }

        public bool IsNone => Type == ErrorType.None;

        internal Error(ErrorType type, string code, string message, IDictionary<string, string[]>? validationErrors = null)
        {
            Type = type;
            Code = code;
            Message = message;
            ValidationErrors = validationErrors;
        }

        public static Error None() => new(ErrorType.None, string.Empty, string.Empty);

        public static Error NotFound(string code, string? description = null)
            => new(ErrorType.NotFound, code, description ?? "Recurso não encontrado.");

        public static Error NotFound(string code, string? description = null, IDictionary<string, string[]>? validationErrors = null)
            => new(ErrorType.NotFound, code, description ?? "Recurso não encontrado.", validationErrors);

        public static Error Validation(string? description = null, IDictionary<string, string[]>? validationErrors = null)
            => new(ErrorType.Validation, ErrorCodes.InputInvalid, description ?? "Falha de validação.", validationErrors);

        public static Error Unauthorized(string code, string? description = null)
            => new(ErrorType.Unauthorized, code, description ?? "Não autorizado.");

        public static Error Conflict(string code, string? description = null)
            => new(ErrorType.Conflict, code, description ?? "Conflito de estado.");

        public static Error Conflict(string code, string? description = null, IDictionary<string, string[]>? validationErrors = null)
            => new(ErrorType.Conflict, code, description ?? "Conflito de estado.", validationErrors);

        public static Error Forbidden(string code, string? description = null)
            => new(ErrorType.Forbidden, code, description ?? "Acesso proibido.");

        public static Error InternalServer(string? description = null)
            => new(ErrorType.InternalServer, ErrorCodes.ServerError, description ?? "Erro interno do servidor.");

        public static Error BusinessRuleViolation(string code, string? description = null)
            => new(ErrorType.BusinessRuleViolation, code, description ?? "Violação de regra de negócio.");

        public static Error BusinessRuleViolation(string code, string? description = null, IDictionary<string, string[]>? validationErrors = null)
            => new(ErrorType.BusinessRuleViolation, code, description ?? "Violação de regra de negócio.", validationErrors);

        public static Error Unknown(string? description = null)
            => InternalServer(description ?? "Ocorreu um erro desconhecido.");
    }
}
