namespace SessionService.Models
{
    public class StateDto
    {
        public required string Username { get; set; }
        public required string SessionId { get; set; }
        public required string RoomName { get; set; }
    }
}
