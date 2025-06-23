namespace HealthService.Classes
{
    public class ClientTracker
    {
        private string username;
        private DateTime lastBeat;
        public string jwt;
        public ClientTracker(string username, DateTime lastBeat, string jwt)
        {
            this.username = username;
            this.lastBeat = lastBeat;
            this.jwt = jwt;
        }
        public bool CheckBeat(bool resetTimer)
        {
            TimeSpan timeDiff = DateTime.Now.Subtract(lastBeat);
            if(timeDiff > Program.HeartbeatDuration)
            {
                return false;
            }
            else
            {
                if(resetTimer)
                    lastBeat = DateTime.Now;
                return true;
            }
        }

    }
}
