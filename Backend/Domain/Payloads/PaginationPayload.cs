using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Payloads
{
    public class PaginationPayload 
    {
        public int Page { get; set; } = 1;
        public int Amount { get; set; } = 5;
    }
}
