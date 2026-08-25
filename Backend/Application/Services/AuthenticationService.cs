using Application.Interfaces;
using Application.Interfaces.IServices;
using Domain.Models;
using Domain.Payloads;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Application.Services

{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IAuthenticationRepository _handleSecurity;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(IAuthenticationRepository handleSecurity, ILogger<AuthenticationService> logger)
        {
            _handleSecurity = handleSecurity;
            _logger = logger;
        }

        public async Task<MessageResult<AuthenticationModel>> GetTokenAsync(LoginPayload request)
        {

            var (estado, intercalResponse, entity) = await _handleSecurity.Token(request);

            _logger.LogInformation("GetTokenAsync() user {UserName} result: {Estado} ({Code}) - {Message}",
                request.UserName, estado, intercalResponse, entity.MessageData);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , entity.MessageData, entity, intercalResponse);

            return MessageResult<AuthenticationModel>.Of(entity.MessageData, entity);

        }


    }
}
