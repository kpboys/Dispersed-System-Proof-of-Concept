using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyClient
{
    public class AdvancedInputArea : InputArea
    {
        private System.IO.StreamReader inpStreamReader;
        private bool stopReader;
        public AdvancedInputArea(Rectangle area, char borderChar, int borderSpacing) : base(area, borderChar, borderSpacing)
        {
            inpStreamReader = new System.IO.StreamReader(Console.OpenStandardInput());
            stopReader = false;
        }
        public override string ReadLine()
        {
            //Program.WriteInDebug("Custom readline being used");
            //Get where the cursor was before we do this
            (int, int) startPos = Console.GetCursorPosition();

            Console.SetCursorPosition(area.X, area.Y);

            string content = AdvancedReader();
            Console.SetCursorPosition(area.X, area.Y); //after pressing enter

            //Make a custom clearLine since the message might have gone beyond the borders
            string clearLine = "";
            for (int i = 0; i < content.Length; i++)
            {
                clearLine += " ";
            }
            Console.Write(clearLine);

            //Draw border in case the text goes outside the box
            DrawBorder();

            //Set cursor back where we found it :)
            Console.SetCursorPosition(startPos.Item1, startPos.Item2);

            return (content);
        }
        public void StopReading()
        {
            Program.WriteInDebug("Closing stream");
            stopReader = true;
        }
        private string AdvancedReader()
        {
            string chatBuffer = "";
            stopReader = false;
            bool typing = true;
            while (typing && stopReader == false)
            {
                var key = Console.ReadKey(true);
                if (stopReader)
                    break;
                if (key.Key == ConsoleKey.Enter)
                {
                    typing = false;
                }
                else if (key.Key == ConsoleKey.Backspace)
                {
                    if (chatBuffer.Length > 0)
                    {
                        ClearLine();
                        chatBuffer = chatBuffer[0..^1]; // Remove last character
                        Console.Write(chatBuffer);
                    }
                }
                else if(key.Key == ConsoleKey.Tab)
                {
                    ClearLine();
                    StopReading();
                }
                else if (key.KeyChar != 0)
                {
                    chatBuffer += key.KeyChar; // Append typed character
                    ClearLine();
                    Console.Write(chatBuffer);
                }


                void ClearLine()
                {
                    //Clear the text
                    Console.SetCursorPosition(area.X, area.Y);
                    string clearLine = "";
                    for (int i = 0; i < chatBuffer.Length; i++)
                    {
                        clearLine += " ";
                    }
                    Console.Write(clearLine);
                    Console.SetCursorPosition(area.X, area.Y);
                }
            }
            if(stopReader)
            {
                chatBuffer = "";
            }
            return chatBuffer;
        }
    }
}
