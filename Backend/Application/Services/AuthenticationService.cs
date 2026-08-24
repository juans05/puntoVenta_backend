using Application.Interfaces;
using Application.Interfaces.IServices;
using Domain.Models;
using Domain.Payloads;
using System.Net;

namespace Application.Services

{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IAuthenticationRepository _handleSecurity;

        public AuthenticationService(IAuthenticationRepository handleSecurity)
        {
            _handleSecurity = handleSecurity;
        }

        public async Task<MessageResult<AuthenticationModel>> GetTokenAsync(LoginPayload request)
        {

            var (estado, intercalResponse, entity) = await _handleSecurity.Token(request);

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
