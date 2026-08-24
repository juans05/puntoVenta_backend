using AutoMapper;
using Application.Helper;
using Application.Interfaces;
using Domain.Entities.Identity;
using Domain.Models;
using Domain.Payloads;
using Domain.Tenant;
using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Domain.DTO;

namespace Infrastructure.Repositories
{
    public class AuthenticationRepository : IAuthenticationRepository
    {
        private readonly SpaContext _context;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly TokenManagement _tokenSettings;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthenticationRepository> _logger;

        public AuthenticationRepository(
            SpaContext context,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IOptions<TokenManagement> tokenSettings,
            TokenValidationParameters tokenValidationParameters,
            IMapper mapper,
            ILogger<AuthenticationRepository> logger)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenSettings = tokenSettings.Value;
            _tokenValidationParameters = tokenValidationParameters;
            _mapper = mapper;
            _logger = logger;
        }

        //--------------------------------------------------------------------
        public async Task<(ServiceStatus, int, AuthenticationModel)> Token(LoginPayload model)
        {
            var authenticationModel = new AuthenticationModel();

            try
            {
                var user = await FindByUsername(model.UserName);

                if (user == null)
                {
                    authenticationModel.MessageData = $"El usuario: {model.UserName} no existe";

                    return (ServiceStatus.FailedValidation, 1100, authenticationModel);
                }

                if (user.Tenant != null && !user.Tenant.Activo)
                {
                    authenticationModel.MessageData = "La empresa se encuentra deshabilitada. Contacte al administrador.";

                    return (ServiceStatus.FailedValidation, 1500, authenticationModel);
                }

                var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, true);

                if (!user.EmailConfirmed)
                {
                    authenticationModel.MessageData = $"Por favor, verifique su cuenta a través del enlace enviado a su correo electronico";
                    return (ServiceStatus.FailedValidation, 1400, authenticationModel);
                }

                if (result.IsLockedOut)
                {
                    authenticationModel.MessageData = $"Se ha superado el limite de intentos o ha sido vetado del sistema";
                    return (ServiceStatus.FailedValidation, 1300, authenticationModel);
                }

                if (result.Succeeded)
                {
                    List<Claim> claims = new List<Claim>();

                    var empresa = user.Tenant.TenantKey;

                    claims = DefaultClaims(user.Id, user.FirstName, user.LastName, user.Email, user.NormalizedUserName, user.PhoneNumber, user.AvatarUrl, user.Tenant.Name, empresa,
                        user.SucursalId?.ToString(), user.Tenant.PaisId?.ToString(), user.Tenant.RubroId.ToString(), user.Tenant.MonedaId?.ToString());

                    if (_userManager.SupportsUserRole)
                    {
                        IList<string> userRoles = await _userManager.GetRolesAsync(user);
                        foreach (string role in userRoles)
                        {
                            claims.Add(new Claim(ClaimTypes.Role, role));
                        }
                    }


                    var applicationUserDto = _mapper.Map<ApplicationUserDto>(user);

                    var rutas = applicationUserDto.Resumen;

                    claims.Add(new Claim("rutas", Newtonsoft.Json.JsonConvert.SerializeObject(rutas)));



                    var key = Encoding.ASCII.GetBytes(_tokenSettings.SecretKey);

                    var encriptionSecret = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_tokenSettings.EncryptionSecret));

                    var tokenHandler = new JwtSecurityTokenHandler();

                    var expiresAt = new DateTime();

                    if (user.UserName == "DEVELOPER")
                    {
                        expiresAt = DateTime.UtcNow.AddMinutes(2);
                    }
                    else
                    {
                        expiresAt = DateTime.UtcNow.AddMinutes(_tokenSettings.AccessTokenExpiration); //SOLO AQUI NO SE DEBE AGREGAR EL -5
                    }


                    var tokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = new ClaimsIdentity(claims),
                        Expires = expiresAt,
                        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                        //EncryptingCredentials = new EncryptingCredentials(encriptionSecret, JwtConstants.DirectKeyUseAlg, SecurityAlgorithms.Aes256CbcHmacSha512)
                    };

                    authenticationModel.IsAuthenticated = true;
                    authenticationModel.Token = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
                    authenticationModel.UserName = user.UserName;
                    authenticationModel.TokenExpirationAt = DateTimeHelper.DateToSpanish(_tokenSettings.AccessTokenExpiration, _tokenSettings.DesfaceTimeWithServer);
                    authenticationModel.TokenExpirationTimestamp = DateTimeHelper.GetUnixTimeMilliseconds(expiresAt);
                    authenticationModel.TokenExpirationTimestampShort = DateTimeHelper.GetUnixTime(_tokenSettings.AccessTokenExpiration);

                    if (user.UserTokens.Any(a => a.IsActive))
                    {
                        var activeRefreshToken = user.UserTokens.Where(a => a.IsActive == true).FirstOrDefault();
                        activeRefreshToken.Revoked = DateTime.UtcNow.AddHours(-5);
                    }

                    var refreshToken = CreateRefreshToken();

                    authenticationModel.RefreshToken = refreshToken.Token;

                    authenticationModel.RefreshTokenExpirationAt = DateTimeHelper.Conversion(refreshToken.Expires, _tokenSettings.DesfaceTimeWithServer);

                    authenticationModel.RefreshTokenExpirationTimestamp = DateTimeHelper.GetUnixTimeMilliseconds(refreshToken.Expires);
                    authenticationModel.RefreshTokenExpirationTimestampShort = DateTimeHelper.GetUnixTime(_tokenSettings.RefreshTokenExpiration);

                    user.UserTokens.Add(refreshToken);

                    //_context.Update(user);

                    await _context.SaveChangesAsync();

                    return (ServiceStatus.Ok, 1, authenticationModel);
                }

                authenticationModel.MessageData = "La contraseña es incorrecta";

                return (ServiceStatus.FailedValidation, 1200, authenticationModel);

            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Token() failed for user {UserName}", model.UserName);
                authenticationModel.MessageData = ex.Message;
                return (ServiceStatus.InternalError, 0, authenticationModel);
            }

        }

        //--------------------------------------------------------------------
        private ClaimsPrincipal GetPrincipalFromToken(string token)
        {
            try
            {
                _tokenValidationParameters.ValidateLifetime = false;

                var principal = new JwtSecurityTokenHandler().ValidateToken(token, _tokenValidationParameters, out var validatedToken);

                _tokenValidationParameters.ValidateLifetime = true;
                if (!IsJwtWithSecurityAlgorithm(validatedToken))
                {
                    return null;
                }

                return principal;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private bool IsJwtWithSecurityAlgorithm(SecurityToken validatedToken)
        {

            return validatedToken is JwtSecurityToken jwtSecurityToken
                && jwtSecurityToken.Header.Alg.Equals(JwtConstants.DirectKeyUseAlg, StringComparison.InvariantCultureIgnoreCase)
            && jwtSecurityToken.Header.Enc.Equals(SecurityAlgorithms.Aes256CbcHmacSha512, StringComparison.InvariantCultureIgnoreCase);
        }

        private async Task<User> FindByEmail(string email)
        {

            var appUser = await _context.Users.AsTracking().FirstOrDefaultAsync(x => x.Email == email);


            if (appUser != null && appUser.UserTokens != null && appUser.UserTokens.Count > 25)
            {

                appUser.UserTokens.Clear();

                var res = await _context.SaveChangesAsync();
            }


            if (appUser != null) return appUser;

            return null;
        }

        private async Task<User> FindByUsername(string username)
        {
            var usuario = username.Trim().ToUpper();

            var appUser = await _context.Users.AsTracking()
                                              .IgnoreQueryFilters()//SE AÑADIO ULTIMO
                                              .Include(p => p.Tenant)
                                              .Include(p => p.UserSubmodules).ThenInclude(p => p.Submodule).ThenInclude(p => p.Module)
                                              .FirstOrDefaultAsync(x => x.NormalizedUserName == usuario);


            if (appUser != null && appUser.UserTokens != null && appUser.UserTokens.Count > 25)
            {

                appUser.UserTokens.Clear();

                var res = await _context.SaveChangesAsync();
            }


            if (appUser != null) return appUser;

            return null;
        }



        private List<Claim> DefaultClaims(
          string userId, string firstName, string lastname, string email, string username, string? phone, string avatar, string tenantNombre, string empresa,
          string? sucursal = null, string? pais = null, string? rubro = null, string? moneda = null)
        {
            var claims = new List<Claim>
                {
                new Claim(JwtRegisteredClaimNames.NameId, userId),
                new Claim("nombre", firstName),
                new Claim(JwtRegisteredClaimNames.FamilyName, lastname ==null?"":lastname),
                new Claim(JwtRegisteredClaimNames.Email, email ==null?"":email),
                new Claim("phone",  phone==null?"":phone),
                new Claim("avatar", avatar==null?"":avatar),
                new Claim("username", username),
                new Claim(ClaimConstants.TenantId,tenantNombre),
                new Claim("empresa", empresa)
                };

            if (!string.IsNullOrEmpty(sucursal)) claims.Add(new Claim(ClaimConstants.Sucursal, sucursal));
            if (!string.IsNullOrEmpty(pais)) claims.Add(new Claim(ClaimConstants.Pais, pais));
            if (!string.IsNullOrEmpty(rubro)) claims.Add(new Claim(ClaimConstants.Rubro, rubro));
            if (!string.IsNullOrEmpty(moneda)) claims.Add(new Claim(ClaimConstants.Moneda, moneda));

            return claims;
        }

        private AspNetUserToken CreateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var generator = RandomNumberGenerator.Create())
            {
                generator.GetBytes(randomNumber);
                return new AspNetUserToken
                {
                    Token = Convert.ToBase64String(randomNumber),
                    Expires = DateTime.UtcNow.AddHours(-5).AddMinutes(_tokenSettings.RefreshTokenExpiration),
                    Created = DateTime.UtcNow.AddHours(-5)
                };
            }
        }
    }
}


