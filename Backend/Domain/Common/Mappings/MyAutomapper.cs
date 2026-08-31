using AutoMapper;
using Domain.Payloads;
using Domain.DTO;
using Domain.Entities;
using Domain.Entities.Identity;
using Domain.Enumerations;

namespace Domain.Common.Mappings

{
    public class MyAutomapper : Profile
    {
        public MyAutomapper()
        {
            CreateMap<ComprobantePayload, ComprobanteCabecera>();
            CreateMap<ComprobanteDetallePayload, ComprobanteDetalle>();
            CreateMap<PagoPayload, Pago>();

            CreateMap<ComprobanteCabecera, ComprobanteCabeceraDTO>()
                .ForMember(dest => dest.IdComprobante, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.NombreVendedor, opt => opt.MapFrom(src => src.UsuarioCreacion))
                .ForMember(dest => dest.TipoDocumentoVenta, opt => opt.MapFrom(src => src.TipoDocumentoVenta.Nombre))
                .ForMember(dest => dest.ClienteNombre, opt => opt.MapFrom(src => src.RazonSocial))

                .ForMember(dest => dest.EstadoEnvioSunat, opt => opt.MapFrom(src => src.EnviadoSunat == EstatusEnvioSunat.Enviado ? "ENVIADO" :
                                                                                    src.EnviadoSunat == EstatusEnvioSunat.Pendiente ? "PENDIENTE" : "ERROR"))

                .ForMember(dest => dest.EstadoComprobante, opt => opt.MapFrom(src => src.EstadoComprobante == EstatusComprobante.Creado ? "CREADO" :
                                                                                     src.EstadoComprobante == EstatusComprobante.Facturado ? "FACTURADO" : "ANULADO"))

                .ForMember(dest => dest.Correlativo, opt => opt.MapFrom(src => src.Correlativo.ToString().PadLeft(7, '0')))
                .ForMember(dest => dest.Fecha, opt => opt.MapFrom(src => src.FechaCreacion.ToString("dd/MM/yyyy HH:mm:ss")))
                .ForMember(dest => dest.FechaVenta, opt => opt.MapFrom(src => (src.FechaVenta ?? src.FechaCreacion).ToString("dd/MM/yyyy HH:mm:ss")));


            CreateMap<ComprobanteDetalle, ComprobanteDetalleDTO>()
                .ForMember(dest => dest.Producto, opt => opt.MapFrom(src => src.Producto.Nombre))
                .ForMember(dest => dest.RutaImagen, opt => opt.MapFrom(src => src.Producto.RutaImagen));

            CreateMap<Pago, PagoDTO>()
                .ForMember(dest => dest.MetodoPago, opt => opt.MapFrom(src => src.Metodopago.Descripcion));


            CreateMap<Cliente, ClienteDto>()
                .ForMember(dest => dest.Sexo, opt => opt.MapFrom(src => src.Sexo == "M" ? "MASCULINO" : "FEMENINO"))
                .ForMember(dest => dest.FechaNacimiento, opt => opt.MapFrom(src => src.FechaNacimiento.HasValue ? src.FechaNacimiento.Value.ToString("dd/MM/yyyy") : null));

            CreateMap<Retiros, RetiroDto>();



            CreateMap<User, ApplicationUserDto>()
                                                .ForMember(x => x.Resumen, y => y.MapFrom(z => z.UserSubmodules.GroupBy(x => x.Submodule.Module.Identificador)
                                                                       .Select(s => new AccesosDetalle
                                                                       {
                                                                           Modulo = s.Key,
                                                                           ModuloNombre = s.Select(p => p.Submodule.Module.Nombre).Max(),
                                                                           SubModulos = new List<SubModuloDetalle>(s.Select(p => new SubModuloDetalle
                                                                           {
                                                                               SubModulo = p.Submodule.Identificador,
                                                                               SubModuloNombre = p.Submodule.Nombre
                                                                           }
                                                                                                                                                     ).ToList())
                                                                       }
                                                                           ).ToList()));

            CreateMap<CreateProductPayload, Producto>();
            //.ForMember(x => x.Comentarios, y => y.MapFrom(z => z.Comentarios));



            CreateMap<CreateClientePayload, Cliente>()
                .ForMember(x => x.Nombre, y => y.MapFrom(z => z.Nombre.ToUpper()))
                .ForMember(x => x.Direccion, y => y.MapFrom(z => z.Direccion.ToUpper()))
                .ForMember(x => x.FechaNacimiento, y => y.MapFrom(z => z.FechaNacimiento));


            CreateMap<UpdateClientePayload, Cliente>()
                .ForMember(x => x.Nombre, y => y.MapFrom(z => z.Nombre.ToUpper()))
                .ForMember(x => x.Direccion, y => y.MapFrom(z => z.Direccion.ToUpper()));

            CreateMap<CreateComentarioPayload, Comentario>();

            CreateMap<UpdateProductPayload, Producto>()
                .ForMember(x => x.Stock, y => y.Ignore());
            //.ForMember(x => x.Comentarios, y => y.MapFrom(z => z.Comentarios));

            CreateMap<Producto, ProductoDto>()
                .ForMember(x => x.productoId, y => y.MapFrom(z => z.Id));

            CreateMap<CreateCategoryPayload, Categoria>();
            CreateMap<Categoria, CategoriaDto>()
                .ForMember(x => x.CategoriaId, y => y.MapFrom(z => z.Id));
            CreateMap<UpdateCategoryPayload, Categoria>();

            CreateMap<CreateProveedorPayload, Proveedor>();
            CreateMap<Proveedor, ProveedorDto>()
                .ForMember(x => x.ProveedorId, y => y.MapFrom(z => z.Id));
            CreateMap<UpdateProveedorPayload, Proveedor>();

            CreateMap<CreateGrupoPayload, Grupo>();
            CreateMap<Grupo, GrupoDto>()
                .ForMember(x => x.GrupoId, y => y.MapFrom(z => z.Id));
            CreateMap<UpdateGrupoPayload, Grupo>();



            CreateMap<InventoryMovement, InventoryMovementDto>()
                .ForMember(x => x.Producto, y => y.MapFrom(z => z.Producto != null ? z.Producto.Nombre : null))
                .ForMember(x => x.TipoMovimiento, y => y.MapFrom(z => ((TipoMovimientoInventario)z.TipoMovimiento).ToString()))
                .ForMember(x => x.Fecha, y => y.MapFrom(z => z.FechaCreacion.ToString("dd/MM/yyyy HH:mm:ss")))
                .ForMember(x => x.Usuario, y => y.MapFrom(z => z.UsuarioCreacion));

            CreateMap<Compra, CompraDto>()
                .ForMember(x => x.Proveedor, y => y.MapFrom(z => z.Proveedor != null ? z.Proveedor.Nombre : null))
                .ForMember(x => x.MetodoPago, y => y.MapFrom(z => z.Metodopago != null ? z.Metodopago.Descripcion ?? z.Metodopago.Nombre : null))
                .ForMember(x => x.FechaCompra, y => y.MapFrom(z => z.FechaCompra.ToString("dd/MM/yyyy HH:mm:ss")))
                .ForMember(x => x.FechaRegistro, y => y.MapFrom(z => z.FechaCreacion.ToString("dd/MM/yyyy HH:mm:ss")))
                .ForMember(x => x.Usuario, y => y.MapFrom(z => z.UsuarioCreacion))
                .ForMember(x => x.Detalle, y => y.MapFrom(z => z.CompraDetalles));

            CreateMap<CompraDetalle, CompraDetalleDto>()
                .ForMember(x => x.Producto, y => y.MapFrom(z => z.Producto != null ? z.Producto.Nombre : null))
                .ForMember(x => x.Subtotal, y => y.MapFrom(z => z.Cantidad * z.CostoUnitario));

            CreateMap<Gasto, GastoDto>()
                .ForMember(x => x.MetodoPago, y => y.MapFrom(z => z.Metodopago != null ? z.Metodopago.Descripcion ?? z.Metodopago.Nombre : null))
                .ForMember(x => x.FechaGasto, y => y.MapFrom(z => z.FechaGasto.ToString("dd/MM/yyyy HH:mm:ss")))
                .ForMember(x => x.FechaRegistro, y => y.MapFrom(z => z.FechaCreacion.ToString("dd/MM/yyyy HH:mm:ss")))
                .ForMember(x => x.Usuario, y => y.MapFrom(z => z.UsuarioCreacion));

            CreateMap<GastoPublicidad, GastoPublicidadDto>()
                .ForMember(x => x.NombreGrupo, y => y.MapFrom(z => z.Grupo != null ? z.Grupo.Nombre : null));

            CreateMap<Ingreso, IngresoDto>()
                .ForMember(x => x.MetodoPago, y => y.MapFrom(z => z.Metodopago != null ? z.Metodopago.Descripcion ?? z.Metodopago.Nombre : null))
                .ForMember(x => x.FechaIngreso, y => y.MapFrom(z => z.FechaIngreso.ToString("dd/MM/yyyy HH:mm:ss")))
                .ForMember(x => x.Usuario, y => y.MapFrom(z => z.UsuarioCreacion));

            CreateMap<CierreDiario, CierreDiarioDto>()
                .ForMember(x => x.FechaCierre, y => y.MapFrom(z => z.FechaCierre.ToString("dd/MM/yyyy HH:mm:ss")))
                .ForMember(x => x.Usuario, y => y.MapFrom(z => z.UsuarioCreacion));

            CreateMap<CreateEmpresaPayload, Empresa>();
            CreateMap<UpdateTenantPayload, Empresa>();


            CreateMap<Empresa, EmpresaDto>()
                .ForMember(x => x.Ubigeo, y => y.MapFrom(z => z.UbigeoId == null ? string.Empty : $"{z.Ubigeo.Provincia} {z.Ubigeo.Departamento} {z.Ubigeo.Distrito}"));


        }

    }

}