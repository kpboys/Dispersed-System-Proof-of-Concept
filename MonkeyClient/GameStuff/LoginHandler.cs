using MonkeyClient.DTOs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace MonkeyClient.GameStuff
{
    public class LoginHandler
    {
        private string loginAddress;
        private string registerAddress;

        private AdvancedInputArea inputArea;
        private SubWindowWithInput loginWindow;
        private bool running;
        private bool doingNetCall;
        private int indexer;
        private string[] options;
        private readonly string loginOption = "Login";
        private readonly string registerOption = "Create Account";

        private string currentUsername;
        private string currentPassword;

        private bool autoLogin = false;

        public bool LoginSuccess { get; private set; }
        public LoginHandler(string loginAddress, string registerAddress)
        {
            this.loginAddress = loginAddress;
            this.registerAddress = registerAddress;
            options = new string[] { loginOption, registerOption };
            indexer = 0;
            inputArea = new AdvancedInputArea(default, '#', 1);
            loginWindow = new SubWindowWithInput(new System.Drawing.Rectangle(5, 5, 20, 10), '#', 1, inputArea, true);
            doingNetCall = false;
        }
        public void StartLoginScreen()
        {
            if (autoLogin)
            {
                bool notDone = true;
                notDone = TryLogin("Peter", "pass").Result;
                while (notDone)
                {
                    Thread.Sleep(500);
                }
                return;
            }
            running = true;
            while (running)
            {
                while (doingNetCall)
                {
                    Thread.Sleep(500);
                }
                if (running == false)
                    break;
                RenderOptions();
                ConsoleKey key = Console.ReadKey(true).Key;

                if(key == ConsoleKey.Spacebar)
                {
                    if (options[indexer] == loginOption)
                        LoginInput();
                    else if (options[indexer] == registerOption)
                        RegisterInput();
                }
                else if (key == ConsoleKey.UpArrow)
                    indexer--;
                else if (key == ConsoleKey.DownArrow)
                    indexer++;
                indexer = (int)RealMod(indexer, options.Length);
            }
        }
        private void RenderOptions()
        {
            loginWindow.ResetLog();
            for (int i = 0; i < options.Length; i++)
            {
                string message = "";
                if (i == indexer)
                    message += "-> ";
                message += options[i];
                loginWindow.WriteLine(message, false);
            }
            loginWindow.RenderLog();
        }
        private async void LoginInput()
        {  
            loginWindow.ResetLog();
            loginWindow.ClearTextArea();

            loginWindow.WriteLine("Input username...");
            string currentUsername = loginWindow.ReadLineInInput();

            loginWindow.WriteLine("Input password...");
            string currentPassword = loginWindow.ReadLineInInput();

            doingNetCall = true;
            doingNetCall = await TryLogin(currentUsername, currentPassword);
        }
        private async void RegisterInput()
        {
            loginWindow.ResetLog();
            loginWindow.ClearTextArea();

            loginWindow.WriteLine("Input email...");
            string currentEmail = loginWindow.ReadLineInInput();

            loginWindow.WriteLine("Input username...");
            string currentUsername = loginWindow.ReadLineInInput();

            loginWindow.WriteLine("Input password...");
            string currentPassword = loginWindow.ReadLineInInput();

            doingNetCall = true;
            doingNetCall = await TryRegister(currentEmail, currentUsername, currentPassword);
        }
        private async Task<bool> TryLogin(string username, string password)
        {
            UserDto userDto = new UserDto() { Username = username, Password = password };
            var content = new StringContent(JsonConvert.SerializeObject(userDto), encoding: Encoding.UTF8, "application/json");
            using (HttpClient client = new HttpClient())
            {
                var response = await client.PostAsync(loginAddress, content);
                if (response.IsSuccessStatusCode)
                {
                    loginWindow.WriteLine("Login successful!");
                    ClientInfo.username = username;
                    ClientInfo.jwt = response.Content.ReadAsStringAsync().Result;
                    LoginSuccess = true;
                    running = false; //Stop login loop
                }
                else if(response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    loginWindow.WriteLine("Login failed: wrong username/password");
                    //Do nothing and let it go back to the option selection
                }
                else if(response.StatusCode == HttpStatusCode.Conflict)
                {
                    loginWindow.WriteLine("Login failed: this account is already logged in");
                }
                else if ((int)response.StatusCode == 503)
                {
                    loginWindow.WriteLine("Login failed: " + response.Content.ReadAsStringAsync().Result);
                }
                else
                {
                    loginWindow.WriteLine("Unknown reponse");
                    Program.WriteInDebug(response.ToString());
                }
                
                loginWindow.WriteLine("Press any key to continue...");
                Console.ReadKey(true);
            }
            return false;
        }
        private async Task<bool> TryRegister(string email, string username, string password)
        {
            RegisterDTO regDTO = new RegisterDTO()
            {
                Email = email,
                Username = username,
                Password = password
            };
            var content = new StringContent(JsonConvert.SerializeObject(regDTO), encoding: Encoding.UTF8, "application/json");
            using (HttpClient client = new HttpClient())
            {
                var response = await client.PostAsync(registerAddress, content);
                if(response.StatusCode == HttpStatusCode.Created)
                {
                    loginWindow.WriteLine("Successfully registered!");
                }
                else if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    loginWindow.WriteLine("Register failed: " + response.Content.ReadAsStringAsync().Result);
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    loginWindow.WriteLine("Register failed: " + response.Content.ReadAsStringAsync().Result);
                }
                else
                {
                    loginWindow.WriteLine("Unknown reponse");
                    Program.WriteInDebug(response.ToString());
                }

                loginWindow.WriteLine("Press any key to continue...");
                Console.ReadKey(true);
            }
            return false;
        }
        private float RealMod(float a, float b)
        {
            return (float)(a - b * Math.Floor(a / b));
        }
    }
}
