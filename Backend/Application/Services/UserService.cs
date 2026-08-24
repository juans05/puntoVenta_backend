using Application.Interfaces;
using Application.Interfaces.IServices;
using Domain.Models;
using Domain.Payloads;
using System.Net;

namespace Application.Services

{
    public class UserService : IUserService
    {
        private readonly IUsersRepository _userRepository;

        public UserService(IUsersRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<MessageResult<string>> CreateUserAsync(CreateUserPayload request)
        {

            var (estado, message, internalCode) = await _userRepository.CreateUser(request);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation ? HttpStatusCode.BadRequest : HttpStatusCode.InternalServerError
                    , message, null, internalResponse: internalCode, status: estado == ServiceStatus.FailedValidation ? 400 : 500);

            return MessageResult<string>.Of(message, "Succeeded");
        }

        public async Task<MessageResult<object>> GetAllUserAccess(string username)
        {

            var (estado, enitity, message) = await _userRepository.GetAllUserAccess(username);

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , message, "error");

            return MessageResult<object>.Of("Succeeded", enitity);
        }
        public async Task<MessageResult<object>> GetAllUsers()
        {

            var (estado, enitities, message) = await _userRepository.GetAllUsers();

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , message);

            return MessageResult<object>.Of("Succeeded", enitities);
        }

        public async Task<MessageResult<object>> ListarUsuarios()
        {

            var (estado, enitities, message) = await _userRepository.ListarUsuarios();

            if (estado != ServiceStatus.Ok)
                throw new ErrorHandler(
                        estado == ServiceStatus.FailedValidation
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
                    , message);

            return MessageResult<object>.Of("Succeeded", enitities);
        }


    }
}
