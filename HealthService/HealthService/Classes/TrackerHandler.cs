using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using HealthService.DTOs;

namespace HealthService.Classes
{
    public class TrackerHandler
    {
        private Dictionary<string, ClientTracker> trackers;
        private Thread checkingThread;
        private Object dictLock = new Object();

        private int checksPerFrame;
        private bool checkingRunning;
        private int counter;

        public TrackerHandler(int checksPerFrame)
        {
            trackers = new Dictionary<string, ClientTracker>();
            this.checksPerFrame = checksPerFrame;
            checkingRunning = false;
        }
        public void StartHeartbeatChecking()
        {
            checkingRunning = true;
            checkingThread = new Thread(() => CheckingLoop());
            checkingThread.IsBackground = true;
            checkingThread.Start();
        }
        private void CheckingLoop()
        {
            int perCounter = 0;
            int wCounter = 0;
            while (checkingRunning)
            {
                if(trackers.Count == 0)
                {
                    //If there's nothing to do yet, just wait a bit
                    wCounter = 0;
                    perCounter = 0;
                    Thread.Sleep(100);
                    continue;
                }

                var item = trackers.ElementAt(wCounter);
                //Console.WriteLine("Checking from loop: " + item.Key);
                CheckClientTracker(item.Key, false);
                if (trackers.Count == 0) //Skip the rest if all clients are gone
                    continue;
                wCounter++;
                wCounter = wCounter % trackers.Count;
                perCounter++;
                if(perCounter == checksPerFrame)
                {
                    perCounter = 0;
                    Thread.Sleep(1000); //Wait a little more than a frame
                }
                

                //int length = checksPerFrame < trackers.Count ? checksPerFrame : trackers.Count; 
                //for (int i = 0; i < length; i++)
                //{
                //    //Potential inaccuracy here:
                //    //We can get a name, then HeartbeatController checks Tracker and it has run out.
                //    //That tracker is removed from the list, and we might skip a tracker here because of it
                //    var item = trackers.ElementAt(i + counter);
                //    CheckClientTracker(item.Key);

                //    counter++;
                //    counter = counter % trackers.Count;
                //}
                //Thread.Sleep(40); //Wait a little more than a frame
            }
        }
        public bool CheckClientTracker(string trackerName, bool resetTimer)
        {
            bool result = false;
            lock (dictLock)
            {
                if(trackers.ContainsKey(trackerName) == false)
                {
                    Console.WriteLine("Client: " + trackerName + " not in dict");
                    return false;
                }
                result = trackers[trackerName].CheckBeat(resetTimer);
                if(result == false)
                {
                    Console.WriteLine("Removing client " + trackerName);
                    ReportToSessionService(trackerName);
                    trackers.Remove(trackerName);

                }
            }
            return result;
        }
        private void ReportToSessionService(string username)
        {
            using var client = new HttpClient();
            
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, Program.endSessionAddress);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", trackers[username].jwt);
            request.Content = CreateJson(new UsernameDto() { Username = username });

            try { client.SendAsync(request); }
            catch (Exception) { }
        }
        public bool AddClientToTrack(string username, string jwt)
        {
            if (trackers.ContainsKey(username))
            {
                Console.WriteLine("Couldn't add user: " +  username);
                return false;
            }
            ClientTracker tracker = new ClientTracker(username, DateTime.Now, jwt);
            trackers.Add(username, tracker);
            return true;
        }
        private StringContent CreateJson(object jsonObject)
        {
            string jsonString = JsonSerializer.Serialize(jsonObject);
            return new StringContent(jsonString, encoding: Encoding.UTF8, "application/json");
        }

    }
}
