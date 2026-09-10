using Application.Interfaces.IRepository;
using Application.Interfaces.IServices;
using Domain.Entities;
using Domain.Models;
using Domain.Payloads;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Application.Abstractions;

namespace Application.Services;

public class ClienteAuthService : IClienteAuthService
{
    private readonly IClienteCuentaRepository _clienteCuentaRepository;
    private readonly IConfiguration _configuration;
    private readonly PasswordHasher<ClienteCuenta> _passwordHasher = new();

    public ClienteAuthService(IClienteCuentaRepository clienteCuentaRepository, IConfiguration configuration)
    {
        _clienteCuentaRepository = clienteCuentaRepository;
        _configuration = configuration;
    }

    public async Task<MessageResult<ClienteLoginResult>> Login(ClienteLoginPayload payload)
    {
        try
        {
            // Buscar ClienteCuenta por email
            var (estado, clienteCuenta, message) = await _clienteCuentaRepository.ObtenerPorEmail(payload.Email);

            if (estado != ServiceStatus.Ok || clienteCuenta == null)
            {
                return MessageResult<ClienteLoginResult>.Of("Credenciales inválidas", null!);
            }

            // Verificar password
            var result = _passwordHasher.VerifyHashedPassword(clienteCuenta, clienteCuenta.PasswordHash, payload.Password);
            
            if (result == PasswordVerificationResult.Failed)
            {
                return MessageResult<ClienteLoginResult>.Of("Credenciales inválidas", null!);
            }

            // Generar JWT
            var token = GenerarJwtToken(clienteCuenta);
            
            var loginResult = new ClienteLoginResult
            {
                Token = token,
                ClienteId = clienteCuenta.ClienteId,
                Email = clienteCuenta.Email,
                Expiracion = DateTime.UtcNow.AddHours(24)
            };

            return MessageResult<ClienteLoginResult>.Of("Login exitoso", loginResult);
        }
        catch (Exception ex)
        {
            throw new ErrorHandler(HttpStatusCode.InternalServerError, "Error en login", ex.Message);
        }
    }

    public async Task<MessageResult<bool>> ValidarToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["TokenManagement:SecretKey"] ?? "");
            
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return MessageResult<bool>.Of("Token válido", true);
        }
        catch
        {
            return MessageResult<bool>.Of("Token inválido", false);
        }
    }

    private string GenerarJwtToken(ClienteCuenta clienteCuenta)
    {
        var key = Encoding.ASCII.GetBytes(_configuration["TokenManagement:SecretKey"] ?? "");
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("clienteId", clienteCuenta.ClienteId.ToString()),
                new Claim(ClaimTypes.Email, clienteCuenta.Email),
                new Claim("tenantId", clienteCuenta.TenantId)
            }),
            Expires = DateTime.UtcNow.AddHours(24),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}