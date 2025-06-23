using MonkeyClient.DTOs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyClient.GameStuff
{
    public class ServerBrowser
    {
        private string registryAddress;
        private List<RoomForClientDTO> rooms;
        private int indexer;
        private bool didGetRooms;
        private SubWindow browserWindow;
        private bool stopper = false;
        public ServerBrowser(string registryAddress)
        {
            this.registryAddress = registryAddress;
            rooms = new List<RoomForClientDTO>();
            indexer = 0;

            browserWindow = new SubWindow(new System.Drawing.Rectangle(5, 5, 20, 10), '¤', 1);
        }
        public async void Start()
        {
            didGetRooms = await GetRooms();
            //if (didGetRooms)
            //{
            //    InputLoop();
            //}
        }
        private async Task<bool> GetRooms()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ClientInfo.jwt);
                var response = await client.GetAsync(registryAddress + "api/getroomlist");
                if(response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    rooms = JsonConvert.DeserializeObject<RoomListDTO>(content).Rooms;
                    rooms.Add(new RoomForClientDTO() { RoomName = "Fake", TcpIPAddress = "none", TcpPort = 4 });
                    return true;
                }
                return false;
            }
        }
        private void InputLoop()
        {
            bool selected = false;
            while (selected == false)
            {
                RenderSelection();
                ConsoleKey key = Console.ReadKey(true).Key;
                if(key == ConsoleKey.Spacebar)
                {
                    selected = true;
                    ClientStateHandler.Instance.RoomSelected(rooms[indexer]);
                }
                else if(key == ConsoleKey.UpArrow)
                    indexer--;
                else if (key == ConsoleKey.DownArrow)
                    indexer++;
                else if (key == ConsoleKey.R)
                {
                    browserWindow.ResetLog();
                    browserWindow.WriteLine("Updating rooms list, please wait...");
                    Task<bool> refreshList = GetRooms();
                    refreshList.Wait(5000);
                }
                else if (key == ConsoleKey.Tab)
                {
                    ClientStateHandler.Instance.ChatFocusedFromAny();
                }
                indexer = (int)RealMod(indexer, rooms.Count);
                //indexer = indexer % rooms.Count;
            }
            browserWindow.ClearWholeWindow();
        }
        public void StateFedInputLoop()
        {
            if(didGetRooms == false)
            {
                //If we're not done getting the rooms, wait a bit
                Thread.Sleep(1000);
                if(didGetRooms == false)
                {
                    browserWindow.WriteLine("No server right now. Press R to refresh");
                }
            }
            if(didGetRooms)
            {
                RenderSelection();
            }
            ConsoleKey key = Console.ReadKey(true).Key;
            if (stopper)
            {
                stopper = false;
                return;
            }
            if (key == ConsoleKey.Spacebar)
            {
                browserWindow.ClearWholeWindow();
                ClientStateHandler.Instance.RoomSelected(rooms[indexer]);
            }
            else if (key == ConsoleKey.UpArrow)
                indexer--;
            else if (key == ConsoleKey.DownArrow)
                indexer++;
            else if (key == ConsoleKey.R)
            {
                browserWindow.ResetLog();
                browserWindow.WriteLine("Updating rooms list, please wait...");
                Task<bool> refreshList = GetRooms();
                didGetRooms = refreshList.Wait(5000);
            }
            else if (key == ConsoleKey.Tab)
            {
                ClientStateHandler.Instance.ChatFocusedFromAny();
            }
            indexer = (int)RealMod(indexer, rooms.Count);
        }
        private void RenderSelection()
        {
            browserWindow.ResetLog();
            for (int i = 0; i < rooms.Count; i++)
            {
                string message = "";
                if (i == indexer)
                    message += "-> ";
                message += rooms[i].RoomName;
                browserWindow.WriteLine(message, false);
            }
            browserWindow.RenderLog();

            //Console.SetCursorPosition(0, 0);
            //for (int i = 0; i < rooms.Count; i++)
            //{
            //    Console.WriteLine("            ");
            //}

            //Console.SetCursorPosition(0, 0);
            //for (int i = 0; i < rooms.Count; i++)
            //{
            //    string message = "";
            //    if (i == indexer)
            //        message += "-> ";
            //    message += rooms[i].RoomName;
            //    Console.WriteLine(message);
            //}
        }
        public void Shutdown()
        {
            browserWindow.ClearWholeWindow();
            stopper = true;
        }
        private float RealMod(float a, float b)
        {
            return (float)(a - b * Math.Floor(a / b));
        }

    }
}
