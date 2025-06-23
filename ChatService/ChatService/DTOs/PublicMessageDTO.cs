using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatService.DTOs
{
    public class PublicMessageDTO
    {
        public string SendingUser { get; set; }
        public string Message { get; set; }
    }
}
