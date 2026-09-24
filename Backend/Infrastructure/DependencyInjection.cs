using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Application.Interfaces.IRepository;
using Application.Services;
using Application.Interfaces.IServices;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Application.Interfaces;
using Application.Interfaces.IProxies;
using Application.Proxies;
using Application.Abstractions;
using Application.Services.BaseService;

namespace Identity.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {

            services
                    .AddScoped<IAuthenticationRepository, AuthenticationRepository>()
                    .AddScoped<IComprobanteRepository, ComprobanteRepository>()
                    .AddScoped<IUsersRepository, UsersRepository>()
                    .AddScoped<IExtensionesRepository, ExtensionesRepository>()
                    .AddScoped<IRoleRepository, RoleRepository>()
                    .AddScoped<ICargaInicialRepository, CargaInicialRepository>()
                    .AddScoped<IAuthenticationService, AuthenticationService>()
                    .AddScoped<IComprobanteService, ComprobanteService>()
                    .AddScoped<IUserService, UserService>()
                    .AddScoped<IExtensionesService, ExtensionesService>()
                    .AddScoped<IRoleService, RoleService>()
                    .AddScoped<ICargaInicialService, CargaInicialService>()
                    .AddScoped<ICajaRepository, CajaRepository>()
                    .AddScoped<IRentaRepositorio, RentaRepositorio>()
                    .AddScoped<IAnfitrionaRepository, AnfitrionaRepository>()
                    .AddScoped<IInventoryRepository, InventoryRepository>()
                    .AddScoped<ICompraRepository, CompraRepository>()
                    .AddScoped<IOrdenCompraRepository, OrdenCompraRepository>()
                    .AddScoped<IGastoRepository, GastoRepository>()
                    .AddScoped<IGastoPublicidadRepository, GastoPublicidadRepository>()
                    .AddScoped<IIngresoRepository, IngresoRepository>()
                    .AddScoped<ICierreDiarioRepository, CierreDiarioRepository>()
                    .AddScoped<IDashboardRepository, DashboardRepository>()
                    .AddScoped<IWhatsappRepository, WhatsappRepository>()
            .AddScoped<IPedidoRepository, PedidoRepository>()
            .AddScoped<IClienteCuentaRepository, ClienteCuentaRepository>();


            services.AddTransient<IProductRepository, ProductRepository>()
                    .AddTransient<ICategoryRepository, CategoriaRepository>()
                    .AddTransient<IProveedorRepository, ProveedorRepository>()
                    .AddTransient<IGrupoRepository, GrupoRepository>()
                    .AddScoped<ITenantRepository, TenantRepository>()


                    .AddScoped<IProductService, ProductService>()
                    .AddScoped<ICategoryService, CategoryService>()
                    .AddScoped<ICajaService, CajaService>()
                    .AddScoped<IGrupoService, GrupoService>()
                    .AddScoped<IProveedorService, ProveedorService>()
                    .AddScoped<IClienteService, ClienteService>()
                    .AddScoped<ITenantService, TenantService>()
                    .AddScoped<IRentaService, RentaService>()
                    .AddScoped<IAnfitrionaService, AnfitrionaService>()
                    .AddScoped<IProductoImagenService, ProductoImagenService>()
                    .AddScoped<IInventoryService, InventoryService>()
                    .AddScoped<ICompraService, CompraService>()
                    .AddScoped<OrdenCompraService>()
                    .AddScoped<IGastoService, GastoService>()
                    .AddScoped<IGastoPublicidadService, GastoPublicidadService>()
                    .AddScoped<IIngresoService, IngresoService>()
                    .AddScoped<ICierreDiarioService, CierreDiarioService>()
                    .AddScoped<IDashboardService, DashboardService>()
                    .AddScoped<IAIService, AIService>()
                    .AddScoped<IWhatsappService, WhatsappService>()
                    .AddScoped<IPedidoService, PedidoService>()
                    .AddScoped<IWhatsappOutboundService, WhatsappOutboundService>()
                    .AddScoped<IClienteAuthService, ClienteAuthService>()


                    .AddTransient<IClienteRepository, ClienteRepository>();


            services.AddHttpClient();
            services.AddScoped<IFacturacionProxy, FacturacionProxy>();
            services.AddScoped<IBaseService, BaseService>();

            services.AddSingleton<TaxCalculatorFactory>();


            return services;
        }
    }
}
