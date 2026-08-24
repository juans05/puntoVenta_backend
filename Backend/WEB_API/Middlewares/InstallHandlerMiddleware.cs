using API.Middlewares.ErrorMiddlewares;

namespace API.Middlewares
{
    public static class InstallHandlerMiddleware
    {
        public static IApplicationBuilder UseHandlerUsers(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ErrorHandlerMiddleware>();
        }
    }
}
