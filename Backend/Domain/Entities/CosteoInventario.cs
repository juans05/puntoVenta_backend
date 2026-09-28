namespace Domain.Entities;

// Costeo promedio ponderado (moving average) para Producto.CostoUnitario: cada recepcion de
// mercaderia (compra directa, compra desde orden, edicion de compra) mezcla lo que ya habia en
// stock con lo que entra. Mas simple que PEPS -- no necesita rastrear capas/lotes de costo por
// separado -- y es el metodo por defecto de ERPs chicos comparables (ej. SAP B1 "Moving Average").
// Los movimientos de SALIDA (ventas, devolucion a proveedor) usan el costo promedio vigente pero
// no lo recalculan; solo las ENTRADAS lo hacen.
public static class CosteoInventario
{
    public static decimal PromedioPonderado(int stockAnterior, decimal costoAnterior, int cantidadRecibida, decimal costoRecibido)
    {
        if (cantidadRecibida <= 0) return costoAnterior;
        // Sin stock previo (o en cero/negativo por un ajuste manual): no hay nada que mezclar, el
        // costo pasa a ser directamente el de esta recepcion.
        if (stockAnterior <= 0) return costoRecibido;

        var stockNuevo = stockAnterior + cantidadRecibida;
        return Math.Round((stockAnterior * costoAnterior + cantidadRecibida * costoRecibido) / stockNuevo, 4);
    }

    // Inverso de PromedioPonderado: le quita a un promedio ya mezclado la contribucion de una
    // entrada especifica (usado al anular esa compra). Exacto si no hubo mas compras del mismo
    // producto entre medio; si las hubo, es la misma aproximacion de mejor esfuerzo que cualquier
    // costeo por promedio movil hace al revertir una transaccion pasada -- no hay forma exacta de
    // "desmezclar" un promedio sin guardar el historial completo de capas (eso ya seria PEPS).
    public static decimal QuitarDePromedio(int stockActual, decimal costoActual, int cantidadARetirar, decimal costoDeEsaEntrada)
    {
        var stockRestante = stockActual - cantidadARetirar;
        if (stockRestante <= 0) return costoActual;

        var valorRestante = (stockActual * costoActual) - (cantidadARetirar * costoDeEsaEntrada);
        return valorRestante > 0 ? Math.Round(valorRestante / stockRestante, 4) : costoActual;
    }
}
