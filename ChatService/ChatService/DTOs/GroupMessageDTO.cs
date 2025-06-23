using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatService.DTOs
{
    public class GroupMessageDTO
    {
        public string SendingUser { get; set; }
        public string Message { get; set; }
    }
}
