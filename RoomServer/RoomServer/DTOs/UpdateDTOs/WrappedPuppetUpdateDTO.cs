using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyClient.DTOs.UpdateDTOs
{
    public class WrappedPuppetUpdateDTO
    {
        public string UpdateType { get; set; }
        public string JsonUpdateData { get; set; }
    }
}
