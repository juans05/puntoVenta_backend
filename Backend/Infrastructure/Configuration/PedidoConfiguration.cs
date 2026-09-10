using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration;

public class PedidoConfiguration
{
    public PedidoConfiguration(EntityTypeBuilder<Pedido> entityBuilder)
    {
        entityBuilder.HasKey(e => e.Id);
        entityBuilder.Property(e => e.Token).IsRequired().HasMaxLength(64);
        entityBuilder.HasIndex(e => e.Token).IsUnique();
        entityBuilder.Property(e => e.EstadoPedido).IsRequired();
        entityBuilder.Property(e => e.Total).HasColumnType("decimal(13,2)");
        entityBuilder.Property(e => e.Nombre).HasMaxLength(150);
        entityBuilder.Property(e => e.Dni).HasMaxLength(20);
        entityBuilder.Property(e => e.Celular).HasMaxLength(20);
        entityBuilder.Property(e => e.UbigeoId).HasMaxLength(10);
        entityBuilder.Property(e => e.TipoEnvio).HasMaxLength(20);
        entityBuilder.Property(e => e.Direccion).HasMaxLength(250);
        entityBuilder.Property(e => e.Referencia).HasMaxLength(250);
        entityBuilder.Property(e => e.Latitud).HasColumnType("decimal(10,7)");
        entityBuilder.Property(e => e.Longitud).HasColumnType("decimal(10,7)");
        entityBuilder.Property(e => e.CodigoSeguimiento).HasMaxLength(100);
        entityBuilder.Property(e => e.PasswordEnviado).HasDefaultValue(false);

        entityBuilder.HasOne(e => e.Cliente)
                     .WithMany()
                     .HasForeignKey(e => e.ClienteId)
                     .IsRequired(false)
                     .OnDelete(DeleteBehavior.Restrict);

        entityBuilder.HasOne(e => e.Ubigeo)
                     .WithMany()
                     .HasForeignKey(e => e.UbigeoId)
                     .IsRequired(false)
                     .OnDelete(DeleteBehavior.Restrict);

        entityBuilder.HasOne(e => e.ComprobanteCabecera)
                     .WithMany()
                     .HasForeignKey(e => e.ComprobanteCabeceraId)
                     .IsRequired(false)
                     .OnDelete(DeleteBehavior.Restrict);

        entityBuilder.HasMany(e => e.PedidoDetalles)
                     .WithOne(d => d.Pedido)
                     .HasForeignKey(d => d.PedidoId)
                     .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PedidoDetalleConfiguration
{
    public PedidoDetalleConfiguration(EntityTypeBuilder<PedidoDetalle> entityBuilder)
    {
        entityBuilder.HasKey(e => e.Id);
        entityBuilder.Property(e => e.Cantidad).IsRequired();
        entityBuilder.Property(e => e.ValorUnitario).HasColumnType("decimal(13,2)");

        entityBuilder.HasOne(e => e.Pedido)
                     .WithMany(p => p.PedidoDetalles)
                     .HasForeignKey(e => e.PedidoId)
                     .OnDelete(DeleteBehavior.Cascade);

        entityBuilder.HasOne(e => e.Producto)
                     .WithMany()
                     .HasForeignKey(e => e.ProductoId)
                     .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ClienteCuentaConfiguration
{
    public ClienteCuentaConfiguration(EntityTypeBuilder<ClienteCuenta> entityBuilder)
    {
        entityBuilder.HasKey(e => e.Id);
        entityBuilder.Property(e => e.Email).IsRequired().HasMaxLength(150);
        entityBuilder.Property(e => e.PasswordHash).IsRequired().HasMaxLength(256);
        entityBuilder.HasIndex(e => e.Email).IsUnique();
        entityBuilder.HasIndex(e => e.ClienteId).IsUnique();

        entityBuilder.HasOne(e => e.Cliente)
                     .WithOne()
                     .HasForeignKey<ClienteCuenta>(e => e.ClienteId)
                     .OnDelete(DeleteBehavior.Cascade);
    }
}