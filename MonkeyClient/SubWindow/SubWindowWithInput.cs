using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyClient
{
    public class InputArea
    {
        internal Rectangle area;
        internal char borderChar;
        internal int borderSpacing;
        internal string clearLine;
        public InputArea(Rectangle area, char borderChar, int borderSpacing)
        {
            GiveNewParams(area, borderChar, borderSpacing);
        }
        public virtual void GiveNewParams(Rectangle area, char borderChar, int borderSpacing)
        {
            this.area = area;
            this.borderChar = borderChar;
            this.borderSpacing = borderSpacing;
            clearLine = "";
            for (int i = 0; i < area.Width; i++)
            {
                clearLine += " ";
            }
        }
        public virtual string ReadLine()
        {
            //Get where the cursor was before we do this
            (int, int) startPos = Console.GetCursorPosition();

            Console.SetCursorPosition(area.X, area.Y);
            //Program.debugWindow.WriteLine("Doing standard readline");
            string content = Console.ReadLine();
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

            return(content);

        }
        internal void DrawBorder()
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
        public void ClearWhole()
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
        public virtual void MoveWindow(int x, int y)
        {
            //Get where the cursor was before we do this
            (int, int) startPos = Console.GetCursorPosition();

            ClearWhole();
            area.X = x;
            area.Y = y;
            DrawBorder();

            //Set cursor back where we found it :)
            Console.SetCursorPosition(startPos.Item1, startPos.Item2);
        }
    }
    public class SubWindowWithInput : SubWindow
    {
        internal InputArea inputArea;
        public SubWindowWithInput(Rectangle area, char borderChar, int borderSpacing) : base(area, borderChar, borderSpacing)
        {
            Rectangle rec = GetAttachedRectangle();
            inputArea = new InputArea(rec, borderChar, borderSpacing);
            inputArea.DrawBorder();
        }
        public SubWindowWithInput(Rectangle area, char borderChar, int borderSpacing, InputArea inputArea, bool attach = false) : base(area, borderChar, borderSpacing)
        {
            if(attach)
            {
                Rectangle rec = GetAttachedRectangle();
                inputArea.GiveNewParams(rec, inputArea.borderChar, inputArea.borderSpacing);
                this.inputArea = new InputArea(rec, inputArea.borderChar, inputArea.borderSpacing);
            }
            this.inputArea = inputArea;
            
            this.inputArea.DrawBorder();
        }
        /// <summary>
        /// Quick thing to get a rectangle for the input area that would be attached to the regular area
        /// </summary>
        /// <returns>The rectangle</returns>
        private Rectangle GetAttachedRectangle()
        {
            return new Rectangle(base.area.X, base.area.Y + base.area.Height + (base.borderSpacing * 2) + 1, base.area.Width, 1);
        }
        /// <summary>
        /// Readline for the window. Use parameter to decide if it should go to the log immediately
        /// </summary>
        /// <param name="writeToLog">Write to log immediately?</param>
        /// <returns>The read line of text</returns>
        public string ReadLineInInput(bool writeToLog = true)
        {
            string result = inputArea.ReadLine();
            if (writeToLog)
            {
                WriteLine(result);
            }
            return result;
        }
        public override void ClearWholeWindow()
        {
            base.ClearWholeWindow();
            inputArea.ClearWhole();
        }
        public override void MoveWindow(int x, int y)
        {
            //Get where the cursor was before we do this
            (int, int) startPos = Console.GetCursorPosition();

            int xInputOffset = inputArea.area.X - base.area.X;
            int yInputOffset = inputArea.area.Y - base.area.Y;

            //Bit of a funky setup here because if we fully clear and draw one first, then the
            //second clear and draw will erase part of the first
            base.ClearWholeWindow();
            inputArea.MoveWindow(x + xInputOffset, y + yInputOffset);
            area.X = x;
            area.Y = y;
            DrawBorder(area, borderChar, borderSpacing);
            RenderLog();

            //Set cursor back where we found it :)
            Console.SetCursorPosition(startPos.Item1, startPos.Item2);

        }
    }
}
