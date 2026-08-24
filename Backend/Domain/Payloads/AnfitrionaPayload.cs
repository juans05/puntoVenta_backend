using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Payloads
{
    public class AnfitrionaPayload : PaginationPayload
    {
        public string? Value { get; set; }
    }
}
