using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatService.DTOs
{
    public class WrappedChatMessageDTO
    {
        public string MessageType { get; set; }
        public string JsonContent { get; set; }
    }
}
