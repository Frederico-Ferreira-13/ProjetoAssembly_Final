using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Common
{
    public class TokenResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
        public string Email { get; set; } = string.Empty;
    }
}
