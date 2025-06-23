using System.Drawing;

namespace MonkeyClient.DTOs
{
    public class InitialSetupDTO
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public List<(string, Point)> Puppets { get; set; }
    }
}
