using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatRelay.DTOs
{
    public class PrivateMessageDTO
    {
        public string SendingUser { get; set; }
        public string Message { get; set; }
        public string ReceivingUser { get; set; }
    }
}
