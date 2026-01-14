using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Common
{
    public enum ErrorType
    {
        None = 0,

        NotFound = 404,
        Validation = 400,
        Unauthorized = 401,
        Forbidden = 403,
        Conflict = 409,
        BusinessRuleViolation = 422,

        InternalServer = 500
    }
}
