using Newtonsoft.Json;
using Domain.Models;
using System.Net;
using System.Security.Claims;

namespace API.Middlewares.ErrorMiddlewares
{
    public sealed class ErrorHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration configuration;
        private readonly ILogger<ErrorHandlerMiddleware> _logger;

        public ErrorHandlerMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<ErrorHandlerMiddleware> logger)
        {
            _next = next;
            this.configuration = configuration;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {

                string apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
                var applicationApikey = configuration.GetSection("ApiKey").Get<string>();
                if (apiKey == applicationApikey)
                {
                    var claims = new[] { new Claim("username", "ADMIN") };
                    var identity = new ClaimsIdentity(claims, "ApiKey");
                    context.User = new ClaimsPrincipal(identity);
                }

                await _next(context);
            }
            catch (ErrorHandler ex)
            {
                _logger.LogError(ex, "Handled error on {Method} {Path}: {Message}", context.Request.Method, context.Request.Path, ex.Message);
                await ErrorHandlerAsync(context, ex);
            }
        }

        private async Task ErrorHandlerAsync(HttpContext context, Exception ex)
        {
            string message = null;

            context.Response.ContentType = "application/json";

            switch (ex)
            {
                case ErrorHandler eh:

                    context.Response.StatusCode = (int)eh.Code;

                    message = eh.Message;

                    await context.Response.WriteAsync(JsonConvert.SerializeObject(MessageResult<object>.Of(message, eh.ExceptionData, eh.Status, eh.InternalResponse)));

                    break;

                case Exception e:

                    message = string.IsNullOrWhiteSpace(e.Message) ? "Error desconocido" : e.Message;

                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                    await context.Response.WriteAsync(JsonConvert.SerializeObject(MessageResult<object>.Of(message, ex.Data, context.Response.StatusCode)));

                    break;
            }

        }
    }
}
