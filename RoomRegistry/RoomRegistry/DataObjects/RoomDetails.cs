namespace RoomRegistry.DataObjects
{
    public class RoomDetails
    {
        public string RoomName { get; set; }
        public string HealthcheckUrl { get; set; }
        public string TcpIPAddress { get; set; }
        public int TcpPort { get; set; }
    }
}
