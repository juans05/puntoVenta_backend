namespace Domain.Entities;

public static class OrdenCompraEstado
{
    // Estado derivado de las cantidades. No toca borrador/pendiente/anulada, ni las cerradas a mano.
    public static void Recalcular(OrdenCompra orden)
    {
        if (orden.EstadoOrden is EstadoOrdenCompra.Borrador or EstadoOrdenCompra.PendienteAprobacion or EstadoOrdenCompra.Anulada)
            return;
        if (orden.MotivoCierre != null)
        {
            orden.EstadoOrden = EstadoOrdenCompra.Cerrada;
            return;
        }

        var recibido = orden.Detalles.Sum(d => d.CantidadRecibida);
        var todoRecibido = orden.Detalles.All(d => d.CantidadRecibida >= d.CantidadPedida);
        var todoFacturado = orden.Detalles.All(d => d.CantidadFacturada >= d.CantidadRecibida);

        orden.EstadoOrden = recibido == 0 ? EstadoOrdenCompra.Emitida
            : !todoRecibido ? EstadoOrdenCompra.RecibidaParcial
            : todoFacturado ? EstadoOrdenCompra.Cerrada
            : EstadoOrdenCompra.Recibida;
    }
}
