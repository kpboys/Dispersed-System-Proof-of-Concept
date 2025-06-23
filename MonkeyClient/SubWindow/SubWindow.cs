using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyClient
{
    /// <summary>
    /// Class for making "windows" in the console and easily writing in them. 
    /// This lets you have multiple areas to write in in one window.
    /// </summary>
    public class SubWindow
    {
        internal List<string> log;
        internal Rectangle area;
        private Point cursorPos;
        internal string clearLine;
        private bool drawBorder;
        internal char borderChar;
        internal int borderSpacing;

        private Object writeLock = new Object();
        private static Object cursorLock = new Object();

        public SubWindow(Rectangle area)
        {
            BaseSetup(area);
            drawBorder = false;
        }
        public SubWindow(Rectangle area, char borderChar, int borderSpacing)
        {
            BaseSetup(area);
            drawBorder = true;
            this.borderChar = borderChar;
            this.borderSpacing = borderSpacing;
            DrawBorder(this.area, this.borderChar, this.borderSpacing);

        }
        private void BaseSetup(Rectangle area)
        {
            log = new List<string>();
            this.area = area;
            cursorPos = new Point(area.X, area.Y);
            clearLine = "";
            for (int i = 0; i < area.Width; i++)
            {
                clearLine += " ";
            }
        }
        internal void DrawBorder(Rectangle area, char borderChar, int borderSpacing)
        {
            (int, int) startPos = Console.GetCursorPosition();
            DrawHorizontal(area.X - 1 - borderSpacing, area.Y - 1 - borderSpacing);
            DrawHorizontal(area.X - 1 - borderSpacing, area.Y + area.Height + borderSpacing);
            DrawVertical(area.X - 1 - borderSpacing, area.Y - 1 - borderSpacing);
            DrawVertical(area.X + area.Width + borderSpacing, area.Y - 1 - borderSpacing);

            Console.SetCursorPosition(startPos.Item1, startPos.Item2);

            //Methods
            void DrawHorizontal(int x, int y)
            {
                string line = "";
                for (int i = 0; i < area.Width + 2 + (borderSpacing * 2); i++)
                {
                    line += borderChar;
                }
                Console.SetCursorPosition(x, y);
                Console.Write(line);
            }
            void DrawVertical(int x, int y)
            {
                int startY = y;
                for (int i = 0; i < area.Height + 2 + (borderSpacing * 2); i++)
                {
                    Console.SetCursorPosition(x, startY + i);
                    Console.Write(borderChar);
                }
            }
        }
        
        //public void WriteLine(string content)
        //{
        //    (int, int) startPos = Console.GetCursorPosition();
        //    int lineCount = 1;
        //    if(content.Length > area.Width)
        //    {
        //        lineCount = (int)MathF.Ceiling((float)content.Length / (float)area.Width);
        //    }
        //    for(int i = 0; i < lineCount; i++)
        //    {
        //        Console.SetCursorPosition(cursorPos.X, cursorPos.Y);
        //        char[] characters = content.ToCharArray();
        //        int remaining = characters.Length - area.Width * i;
        //        Console.Write(characters, area.Width * i, area.Width > remaining ? remaining : area.Width);
        //        cursorPos.Y++;
        //        if(cursorPos.Y > area.Y + area.Height)
        //        {
        //            Clear();
        //        }
        //    }
        //    Console.SetCursorPosition(startPos.Item1, startPos.Item2);
        //}
        public void WriteLine(string content, bool renderImmediately = true)
        {
            if (content == "") return;

            int lineCount = 1;
            if (content.Length > area.Width)
            {
                lineCount = (int)MathF.Ceiling((float)content.Length / (float)area.Width);
            }

            //Lock so we can handle multiple threads trying to write here
            lock (writeLock)
            {
                for (int i = 0; i < lineCount; i++)
                {
                    int remaining = content.Length - area.Width * i;
                    log.Add(content.Substring(i * area.Width, area.Width > remaining ? remaining : area.Width));
                }
            }
            if(renderImmediately)
                RenderLog();
        }
        
        public void RenderLog()
        {
            lock (cursorLock)
            {
                //Get where the cursor was before we do this
                (int, int) startPos = Console.GetCursorPosition();

                //Clear area before rendering
                ClearTextArea();

                int startYIndex = log.Count - area.Height;
                for (int i = 0; i < area.Height; i++)
                {
                    Console.SetCursorPosition(area.X, area.Y + i);
                    int logIndex = startYIndex + i;
                    if (logIndex >= 0)
                    {
                        Console.Write(log[logIndex]);
                    }
                }

                //Set cursor back where we found it :)
                Console.SetCursorPosition(startPos.Item1, startPos.Item2);
            }
        }
        public void ClearTextArea()
        {
            lock(cursorLock)
            {
                for (int i = 0; i < area.Height; i++)
                {
                    Console.SetCursorPosition(area.X, area.Y + i);
                    Console.Write(clearLine);
                }
                cursorPos = new Point(area.X, area.Y);
            }
        }
        public virtual void ClearWholeWindow()
        {
            lock (cursorLock)
            {
                //Get where the cursor was before we do this
                (int, int) startPos = Console.GetCursorPosition();

                string clearLine = "";
                for (int i = 0; i < area.Width + (borderSpacing + 1) * 2; i++)
                {
                    clearLine += " ";
                }
                int startY = area.Y - (borderSpacing + 1);
                int xPos = area.X - (borderSpacing + 1);
                for (int i = 0; i < area.Height + (borderSpacing + 1) * 2; i++)
                {
                    Console.SetCursorPosition(xPos, startY + i);
                    Console.Write(clearLine);
                }

                //Set cursor back where we found it :)
                Console.SetCursorPosition(startPos.Item1, startPos.Item2);
            }
        }
        public void ResetLog()
        {
            log.Clear();
        }
        public virtual void MoveWindow(int x, int y)
        {
            //Get where the cursor was before we do this
            (int, int) startPos = Console.GetCursorPosition();

            ClearWholeWindow();
            area.X = x;
            area.Y = y;
            DrawBorder(area, borderChar, borderSpacing);
            RenderLog();

            //Set cursor back where we found it :)
            Console.SetCursorPosition(startPos.Item1, startPos.Item2);
        }
    }
}
