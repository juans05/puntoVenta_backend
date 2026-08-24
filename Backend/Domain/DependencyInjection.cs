using Microsoft.Extensions.DependencyInjection;
using Domain.Common.Mappings;

namespace Domain
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDomain(this IServiceCollection services)
        {
            services.AddAutoMapper(typeof(DependencyInjection).Assembly, typeof(MyAutomapper).Assembly);

            return services;
        }
    }
}
