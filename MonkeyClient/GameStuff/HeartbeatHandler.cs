using MonkeyClient.DTOs.HealthDTOs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http.Headers;

namespace MonkeyClient.GameStuff
{
    public class HeartbeatHandler
    {
        private bool heartbeatRunning;
        private int heartbeatInterval;
        private string healthServiceAddress;
        public HeartbeatHandler(string healthServiceAddress, int heartbeatInterval)
        {
            heartbeatRunning = false;
            this.healthServiceAddress = healthServiceAddress;
            this.heartbeatInterval = heartbeatInterval;
        }
        public void StartHeartbeat()
        {
            Thread beatThread = new Thread(() => HeartbeatLoop());
            beatThread.IsBackground = true;
            beatThread.Start();
        }
        private async void HeartbeatLoop()
        {
            heartbeatRunning = true;
            var dto = new ClientHeartbeatDTO() { Username = ClientInfo.username };
            var content = new StringContent(JsonConvert.SerializeObject(dto), encoding: Encoding.UTF8, "application/json");
            
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ClientInfo.jwt);
                async Task<HttpStatusCode> Beat(string addressAddon)
                {
                    DateTime startingCall = DateTime.Now;

                    try
                    {
                        var response = await client.PostAsync(healthServiceAddress + addressAddon, content);
                        if ((int)response.StatusCode == 200)
                        {
                            Program.WriteInDebug("Heartbeat was okay");

                            //Adjust for time it took to post
                            TimeSpan timePostTook = DateTime.Now.Subtract(startingCall);
                            int sleepTime = heartbeatInterval - (int)timePostTook.TotalMilliseconds;
                            Thread.Sleep(sleepTime);
                            return response.StatusCode;
                        }
                        else
                        {
                            return response.StatusCode;
                        }
                    }
                    catch (Exception ex)
                    {
                        Program.WriteInDebug("Failed to post heartbeat");
                        return HttpStatusCode.NotFound;
                    }
                    
                }

                HttpStatusCode firstCode = await Beat("firstheartbeat");

                //Wait a bit and keep trying to register heartbeat
                while (firstCode == HttpStatusCode.NotFound)
                {
                    Thread.Sleep(5000);
                    firstCode = await Beat("firstheartbeat");
                }

                if (firstCode == HttpStatusCode.BadRequest)
                {
                    heartbeatRunning = false;
                }

                while (heartbeatRunning)
                {
                    HttpStatusCode code = await Beat("heartbeat");
                    if ((int)code == 419)
                    {
                        Program.WriteInDebug("Heartbeat too late");
                        ClientStateHandler.Instance.FailedHeartbeat();
                        heartbeatRunning = false;
                    }
                }
            }
        }
        public void StopHeartbeat()
        {
            heartbeatRunning = false;
        }
    }
}
