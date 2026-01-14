using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Common
{
    public class ErrorCodes
    {
        public const string AuthFailed = "Auth.Failed";
        public const string AuthUnauthorized = "Auth.Unauthorized";
        public const string AuthForbidden = "Auth.Forbidden";
        public const string AuthInvalidToken = "Auth.InvalidToken";
        public const string AuthExpired = "Auth.Expired";
        public const string AuthLockedOut = "Auth.LockedOut";

        public const string InputInvalid = "Input.Invalid";
        public const string InputMissing = "Input.Missing";
        public const string InputEmailFormat = "Input.EmailFormat";
        public const string InputPasswordShort = "Input.PasswordShort";

        public const string NotFound = "NotFound"; // 404
        public const string BadRequest = "BadRequest"; // 400
        public const string Conflict = "Conflict"; // 409
        public const string AlreadyExists = "Conflict.Exists"; // Mais específico para conflito de criação

        public const string BizInvalidOperation = "Biz.InvalidOp";
        public const string BizHasDependencies = "Biz.Dependencies";
        public const string BusinessRuleViolationCode = "Difficulty.Deactivated";

        public const string ServerError = "Server.Error"; // 500
        public const string DBError = "DB.Error";
        public const string DBConnection = "DB.Connection";
        public const string ForeignKeyViolation = "DB.FKViolation";
    }
}
