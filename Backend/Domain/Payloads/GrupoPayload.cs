using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Payloads
{
    public class GrupoPayload //: PaginationPayload
    {
        public int? CategoriaId { get; set; }
        public string? Value { get; set; }
    }
}
