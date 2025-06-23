namespace SessionService.Models
{
    public class StatusDto
    {
        public required string Username { get; set; }
        public required bool IsAlive { get; set; }
    }
}
