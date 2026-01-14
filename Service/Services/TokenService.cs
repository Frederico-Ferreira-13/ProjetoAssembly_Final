using Contracts.Service;
using Core.Common;
using Core.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Service.Services
{
    public class TokenService : ITokenService
    {
        private readonly IHttpContextAccessor _httpContextAcessor;
        private readonly JwtSettings _jwtSettings;

        public TokenService(IHttpContextAccessor httpContextAcessor, JwtSettings jwtSettings)
        {
            _httpContextAcessor = httpContextAcessor ?? throw new ArgumentNullException(nameof(httpContextAcessor));
            _jwtSettings = jwtSettings ?? throw new ArgumentNullException(nameof(jwtSettings));
        }

        public TokenResponse GenerateToken(Users user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, user.UsersRoleId.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecurityKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expiresAt,
                SigningCredentials = credentials,
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return new TokenResponse
            {
                Token = tokenHandler.WriteToken(token),
                Expiration = expiresAt,
                Email = user.Email
            };
        }

        public Task<Result<int>> GetUserIdFromContextAsync()
        {
            var httpContext = _httpContextAcessor.HttpContext;
            if (httpContext == null)
            {
                return Task.FromResult(Result<int>.Failure(
                     Error.InternalServer(
                     "Contexto HTTP não disponível. Não foi possível obter o ID do utilizador.")
                ));
            }

            if (httpContext.User == null || !httpContext.User.Identity?.IsAuthenticated == true)
            {
                return Task.FromResult(Result<int>.Failure(
                    Error.Unauthorized(
                    ErrorCodes.AuthUnauthorized,
                    "O utilizador não está autenticado.")
                ));
            }

            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdClaim != null && int.TryParse(userIdClaim, out int userId))
            {
                return Task.FromResult(Result<int>.Success(userId));
            }
            else
            {
                return Task.FromResult(Result<int>.Failure(
                    Error.Unauthorized(
                    ErrorCodes.AuthFailed,
                    "ID do utilizador (Claim NameIdentifier) não encontrado ou inválido no token.")
                ));
            }
        }

        public Task InvalidateTokenAsync()
        {
            Console.WriteLine("LOGOUT: Tentativa de invalidação do token/sessão.");

            return Task.CompletedTask;
        }
    }
}
