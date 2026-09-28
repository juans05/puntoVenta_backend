using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Payloads
{
    public class ProductPayload : PaginationPayload
    {
        public int? CategoriaId { get; set; }
        public int? GrupoId { get; set; }
        public string? Value { get; set; }
        // Filtran el picker de Ventas (SeVende) o Compras (SeCompra) -- null = sin filtrar (pantalla
        // de administracion de productos, que debe mostrar todos para poder editar los toggles).
        public bool? SeVende { get; set; }
        public bool? SeCompra { get; set; }
    }
}
