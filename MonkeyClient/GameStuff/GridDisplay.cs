using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyClient.GameStuff
{
    public class GridDisplay
    {
        public string[,] grid;
        private PuppetManager manager;
        private SubWindow gridWindow;
        private int iconPadding;
        public GridDisplay(int width, int height, PuppetManager manager, int iconPadding = 1)
        {
            this.iconPadding = iconPadding;
            int paddedWidth = width + (width * iconPadding * 2);
            gridWindow = new SubWindow(new Rectangle(5,5, paddedWidth, height), '+', 1);
            grid = new string[width, height];
            this.manager = manager;
            ClearGrid();
        }
        private void ClearGrid()
        {
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    AddGridIcon(x, y, '.');
                }
            }
        }
        private void AddGridIcon(int x, int y, char icon)
        {
            string text = "";
            for (int i = 0; i < iconPadding; i++)
            {
                text += " ";
            }
            text += icon;
            for (int i = 0; i < iconPadding; i++)
            {
                text += " ";
            }
            grid[x, y] = text;
        }
        public void UpdateGrid()
        {
            ClearGrid();
            foreach (var pair in manager.Puppets)
            {
                //Flipped here to render properly
                int xPos = pair.Value.Y;
                int yPos = pair.Value.X;
                AddGridIcon(xPos, yPos, pair.Key[0]);
            }
            RenderGridWindowed();
        }
        public void RenderGrid()
        {
            Console.SetCursorPosition(0, 0);
            for (int x = 0; x < grid.GetLength(1); x++)
            {
                for (int y = 0; y < grid.GetLength(0); y++)
                {
                    Console.Write(grid[x, y]);
                }
                Console.WriteLine();
            }
        }
        public void RenderGridWindowed()
        {
            List<string> linesToWrite = new List<string>();
            for (int x = 0; x < grid.GetLength(1); x++)
            {
                linesToWrite.Add("");
                for (int y = 0; y < grid.GetLength(0); y++)
                {
                    linesToWrite[x] += grid[x, y];
                }
            }
            gridWindow.ResetLog();
            for (int i = 0; i < linesToWrite.Count; i++)
            {
                gridWindow.WriteLine(linesToWrite[i], false); //Set to not render immediately, to save some power
            }
            gridWindow.RenderLog();
        }
        public void RemoveGrid()
        {
            gridWindow.ClearWholeWindow();
        }
    }
}
