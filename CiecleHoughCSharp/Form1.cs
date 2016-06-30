using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace CiecleHoughCSharp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private Bitmap GetPicFromDialog()
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "Open Image";
                dlg.Filter = "bmp files (*.bmp)|*.bmp";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    return new Bitmap(dlg.FileName);
                }
            }

            return null;
        }

        private void CircleHough(object sender, EventArgs e)
        {
            var baseImage = GetPicFromDialog();
            var workingCopy = (Bitmap)baseImage.Clone();

            var radius = Microsoft.VisualBasic.Interaction.InputBox("Insert radius", "Input radius");

            Result[] pick = null;

            var grayImg = CreateNegative(workingCopy);

            var binarMap = DetectEdge(grayImg);

            var tabs = new List<KeyValuePair<string, Bitmap>>
                    {
                        new KeyValuePair<string, Bitmap>("Original", baseImage),
                        new KeyValuePair<string, Bitmap>("Edge", CreateImgFromMap(binarMap))
                    };

            if (string.IsNullOrEmpty(radius))
            {
                var houghMap = CreateHoughSpace(binarMap);

                pick = GetHighestVotes(houghMap);
            }
            else
            {
                var r = short.Parse(radius);
                var houghMap = CreateHoughSpace(binarMap, r);

                var houghSpaceImg = DrawHoughSpace(baseImage, houghMap, r);
                tabs.Add(new KeyValuePair<string, Bitmap>("Hough", houghSpaceImg));

                pick = GetHighestVotes(houghMap, r) ;
            }

            using (Graphics g = Graphics.FromImage(workingCopy))
            {
                for (int i = 0; i < pick.Length; i++)
                {
                    var result = pick[i];
                    DrawCircle(g, new Point { X = result.x, Y = result.y }, result.r);
                }
            }

            tabs.Add(new KeyValuePair<string, Bitmap>("Result", workingCopy));

            AddTabs(tabs);
        }

        private void CircleVector(object sender, EventArgs e)
        {
            var baseImage = GetPicFromDialog();
            var workingCopy = (Bitmap)baseImage.Clone();

            var radius = Microsoft.VisualBasic.Interaction.InputBox("Insert radius", "Input radius");

            Result[] pick = null;
            var grayImg = CreateNegative(workingCopy);

            var binarMap = DetectEdge(grayImg);

            var tabs = new List<KeyValuePair<string, Bitmap>>
                    {
                        new KeyValuePair<string, Bitmap>("Original", baseImage),
                        new KeyValuePair<string, Bitmap>("Edge", CreateImgFromMap(binarMap))
                    };

            if (string.IsNullOrEmpty(radius))
            {
                var vSpace = CreateVectorVoteSpace(binarMap, grayImg);

                pick = GetHighestVotes(vSpace, 10);
            }
            else
            {
                var r = short.Parse(radius);
                var vSpace = CreateVectorVoteSpace(binarMap, r);

                pick = GetHighestVotes(vSpace, r, 0);
            }

            using (Graphics g = Graphics.FromImage(workingCopy))
            {
                for (int i = 0; i < pick.Length; i++)
                {
                    var result = pick[i];
                    DrawCircle(g, new Point { X = result.x, Y = result.y }, result.r);
                }
            }

            tabs.Add(new KeyValuePair<string, Bitmap>("Result", workingCopy));

            AddTabs(tabs);
        }

        private void SquareVector(object sender, EventArgs e)
        {
            var baseImage = GetPicFromDialog();

            var workingCopy = (Bitmap)baseImage.Clone();

            var grayImg = CreateNegative(workingCopy);

            var binarMap = DetectEdge(grayImg);

            var vSpace = CreateVectorVoteSpace(binarMap, grayImg, Table.Square);

            var pick = GetHighestVotes(vSpace, 4);
            
            using (Graphics g = Graphics.FromImage(workingCopy))
            {
                for (int i = 0; i < pick.Length; i++)
                {
                    var result = pick[i];
                    DrawSquare(g, new Point { X = result.x, Y = result.y }, 2 * result.r, 2 * result.r);
                }
            }

            var tabs = new List<KeyValuePair<string, Bitmap>>
            {
                new KeyValuePair<string, Bitmap>("Original", baseImage),
                new KeyValuePair<string, Bitmap>("Edge", CreateImgFromMap(binarMap)),
                new KeyValuePair<string, Bitmap>("Result", workingCopy)
            };

            AddTabs(tabs);
        }

        private short[,] CreateVectorVoteSpace(bool[,] binarMap, int radius)
        {
            var height = binarMap.GetLength(0);
            var width = binarMap.GetLength(1);
            var result = new short[height, width];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (binarMap[y, x])
                    {
                        if (y + radius < height) { result[y + radius, x]++; };
                        if (x + radius < width) { result[y, x + radius]++; };
                        if (y - radius >= 0) { result[y - radius, x]++; };
                        if (x - radius >= 0) { result[y, x - radius]++; };
                    }
                }
            }

            return result;
        }

        private static short[, ,] CreateVectorVoteSpace(bool[,] BinarEdgeMap, short[,] grayImg)
        {
            var binarHeight = BinarEdgeMap.GetLength(0);
            var binarWidth = BinarEdgeMap.GetLength(1);

            var radius = binarHeight < binarWidth ? binarHeight : binarWidth;

            var resultCube = new short[radius, binarHeight, binarWidth];
            for (int r = 1; r < radius; r++)
            {
                for (int y = 0; y < binarHeight; y++)
                {
                    for (int x = 0; x < binarWidth; x++)
                    {
                        if (BinarEdgeMap[y, x])
                        {
                            var angle = GetGradAngle(grayImg, x, y);

                            var x0 = (int)(r * Math.Cos(angle));
                            var y0 = (int)(r * Math.Sin(angle));

                            if (x + x0 >= 0 && y + y0 >= 0 && x + x0 < binarWidth && y + y0 < binarHeight) { resultCube[r, y + y0, x + x0]++; };

                            x0 = (int)(r * Math.Cos(angle + Math.PI));
                            y0 = (int)(r * Math.Sin(angle + Math.PI));
                            if (x + x0 >= 0 && y + y0 >= 0 && x + x0 < binarWidth && y + y0 < binarHeight) { resultCube[r, y + y0, x + x0]++; };

                            //if (y + r < binarHeight) { resultCube[r, y + r, x]++; };
                            //if (x + r < binarWidth) { resultCube[r, y, x + r]++; };
                            //if (y - r >= 0) { resultCube[r, y - r, x]++; };
                            //if (x - r >= 0) { resultCube[r, y, x - r]++; };
                        }
                    }
                }
            }
            return resultCube;
        }

        private class Result
        {
            public short x;
            public short y;
            public short r;
            public short value;
        }

        private static Result[] GetHighestVotes(short[, ,] accCube, int trashold = 254)
        {
            var radius = accCube.GetLength(0);
            var height = accCube.GetLength(1);
            var width = accCube.GetLength(2);

            var ResultList = new List<Result>();
            for (short r = 1; r < radius; r++)
            {
                Result result = new Result();
                for (short y = 0; y < height; y++)
                {
                    for (short x = 0; x < width; x++)
                    {
                        if (accCube[r, y, x] > trashold && result.value < accCube[r, y, x])
                        {
                            result.x = x;
                            result.y = y;
                            result.r = r;
                            result.value = accCube[r, y, x];
                        }
                    }
                }

                if (result.value != 0)
                {
                    ResultList.Add(result);
                }
            }
            return ResultList.ToArray();
        }

        private static Result[] GetHighestVotes(short[,] accMatrix, short radius, int trashold = 254)
        {
            var height = accMatrix.GetLength(0);
            var width = accMatrix.GetLength(1);

            var results = new List<Result>();
            for (short y = 0; y < height; y++)
            {
                for (short x = 0; x < width; x++)
                {
                    if (accMatrix[y, x] > trashold)// && result.value < accMatrix[y, x])
                    {
                        Result result = new Result();
                        result.x = x;
                        result.y = y;
                        result.r = radius;
                        result.value = accMatrix[y, x];
                        results.Add(result);
                    }
                }
            }
            return results.ToArray();
        }

        private static short[,] CreateHoughSpace(bool[,] BinarEdgeMap, int radius)
        {
            var binarHeight = BinarEdgeMap.GetLength(0);
            var binarWidth = BinarEdgeMap.GetLength(1);

            var resultMatrix = new short[binarHeight, binarWidth];
            for (int y = 0; y < binarHeight; y++)
            {
                for (int x = 0; x < binarWidth; x++)
                {
                    if (BinarEdgeMap[y, x])
                    {
                        UpdateHoughMatrix(ref resultMatrix, x, y, radius);
                    }
                }
            }
            return resultMatrix;
        }

        private static short[, ,] CreateHoughSpace(bool[,] BinarEdgeMap)
        {
            var binarHeight = BinarEdgeMap.GetLength(0);
            var binarWidth = BinarEdgeMap.GetLength(1);

            var radius = binarHeight < binarWidth ? binarHeight : binarWidth;

            var resultCube = new short[radius, binarHeight, binarWidth];
            for (int y = 0; y < binarHeight; y++)
            {
                for (int x = 0; x < binarWidth; x++)
                {
                    if (BinarEdgeMap[y, x])
                    {
                        UpdateHoughMatrix(ref resultCube, x, y, radius);
                    }
                }
            }
            return resultCube;
        }

        private static void UpdateHoughMatrix(ref short[,] matrix, int x, int y, int radius)
        {
                for (int teta = 0; teta < 360; teta++)
                {
                    var a = (int)(x + radius * Math.Cos(teta));
                    var b = (int)(y + radius * Math.Sin(teta));

                    if (a < 0 || b < 0 || b >= matrix.GetLength(0) || a >= matrix.GetLength(1))
                    {
                        continue;
                    }

                    matrix[b, a]++;
                }
            }

        private static void UpdateHoughMatrix(ref short[, ,] cube, int x, int y, int maxRadius)
        {
            for (int radius = 1; radius < maxRadius; radius++)
            {
                for (int teta = 0; teta < 360; teta++)
                {
                    var a = (int)(x + radius * Math.Cos(teta));
                    var b = (int)(y + radius * Math.Sin(teta));

                    if (a < 0 || b < 0 || a >= cube.GetLength(0) || b >= cube.GetLength(1))
                    {
                        continue;
                    }

                    cube[radius, b, a]++;
                }
            }
        }

        private static bool[,] CreateBinarMap(Bitmap negativeImage, int thrashold)
        {
            var binarMap = new bool[negativeImage.Height, negativeImage.Width];

            for (int y = 0; y < negativeImage.Height; y++)
            {
                for (int x = 0; x < negativeImage.Width; x++)
                {
                    var color = negativeImage.GetPixel(x, y);
                    if ((color.R + color.G + color.B) / 3 > thrashold)
                    {
                        binarMap[y, x] = true;
                    }
                }
            }

            return binarMap;
        }

        private static Bitmap CreateImgFromMap(bool[,] map)
        {
            var height = map.GetLength(0);
            var width = map.GetLength(1);
            Bitmap img = new Bitmap(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (map[y, x])
                    {
                        img.SetPixel(x, y, Color.White);
                    }
                    else
                    {
                        img.SetPixel(x, y, Color.Black);
                    }
                }
            }
            return img;
        }

        private static short[,] CreateNegative(Bitmap img)
        {
            var grayMatrix = ToGrayMatrix(img);

            var height = grayMatrix.GetLength(0);
            var width = grayMatrix.GetLength(1);
            var result = new short[height, width];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    result[y, x] = (short)(255 - grayMatrix[y, x]);
                }
            }

            return result;
        }

        private static Bitmap CreateImage(int width, int height)
        {
            var img = new Bitmap(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    img.SetPixel(x, y, Color.Black);
                }
            }
            return img;
        }

        private static void DrawCircle(Graphics drawingArea, Point center, int radius)
        {
            Pen pen = new Pen(Color.Red, 3);
            Rectangle rect = new Rectangle(center.X - radius, center.Y - radius, radius * 2, radius * 2);
            drawingArea.DrawEllipse(pen, rect);
        }

        private void DrawSquare(Graphics drawingArea, Point center, int width, int height)
        {
            // Create pen.
            Pen pen = new Pen(Color.Red, 3);

            // Create rectangle.
            Rectangle rect = new Rectangle(center.X - width / 2 , center.Y - height / 2, width, height);

            // Draw rectangle to screen.
            drawingArea.DrawRectangle(pen, rect);
        }

        private static Bitmap DrawHoughSpace(Bitmap original, short[,] accMatrix, int r, int trashold = 255) 
        {
            var spaceImg = CreateImage(accMatrix.GetLength(1), accMatrix.GetLength(0));
            for (int y = 0; y < spaceImg.Height; y++)
            {
                for (int x = 0; x < spaceImg.Width; x++)
                {
                    var num = accMatrix[y, x] > trashold - 1 ? 255 : accMatrix[y, x];
                    spaceImg.SetPixel(x, y, Color.FromArgb(num, num, num));
                }
            }
            return spaceImg;
        }

        private static bool[,] DetectEdge(short[,] binarImg)
        {
            int height = binarImg.GetLength(0);
            int width = binarImg.GetLength(1);
            var bb = new bool[height, width];

            int[,] gx = new int[,] 
                { 
                  { -1, 0, 1 }, 
                  { -2, 0, 2 }, 
                  { -1, 0, 1 } 
                };

            int[,] gy = new int[,] 
            { 
                { 1, 2, 1 }, 
                { 0, 0, 0 }, 
                { -1, -2, -1 } 
            };

            int limit = 128 * 128;

            int newX = 0, newY = 0, c = 0;
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {

                    newX = 0;
                    newY = 0;
                    c = 0;

                    for (int hw = -1; hw < 2; hw++)
                    {
                        for (int ww = -1; ww < 2; ww++)
                        {
                            c = binarImg[y + hw, x + ww];
                            newX += gx[hw + 1, ww + 1] * c;
                            newY += gy[hw + 1, ww + 1] * c;
                        }
                    }
                    if (newX * newX + newY * newY > limit)
                        bb[y, x] = true;
                    else
                        bb[y, x] = false;
                }
            }
            return bb;
        }

        private static double GetGradAngle(short[,] grayImg, int x0, int y0) 
        {
            var defX = (grayImg[y0, x0 + 1] - grayImg[y0, x0]);

            return Math.Atan2(grayImg[y0 + 1, x0] - grayImg[y0, x0], defX);
        }

        private static short[,] ToGrayMatrix(Bitmap img) 
        {
            var grayMatrix = new short[img.Height, img.Width];
            for (int y = 0; y < img.Height; y++)
            {
                for (int x = 0; x < img.Width; x++)
                {
                    var color = img.GetPixel(x, y);
                    grayMatrix[y,x] = (short)((color.R + color.G + color.B) / 3);
                }
            }

            return grayMatrix;
        }

        private static short[,,] CreateVectorVoteSpace(bool[,] BinarEdgeMap, short[,] grayImg, List<TableRow> table)
        {
            var binarHeight = BinarEdgeMap.GetLength(0);
            var binarWidth = BinarEdgeMap.GetLength(1);

            var factor = binarHeight < binarWidth ? binarHeight : binarWidth;

            var resultCube = new short[factor, binarHeight, binarWidth];
            for (int f = 1; f < factor; f++)
            {
                for (int y = 0; y < binarHeight; y++)
                {
                    for (int x = 0; x < binarWidth; x++)
                    {
                        if (BinarEdgeMap[y, x])
                        {
                            var angle = GetGradAngle(grayImg, x, y);

                            var row = GetTableRow(table, angle);

                            if (row == null)
                            {
                                continue;
                            }

                            var x0 = (int)(f * row.Length * Math.Cos(angle));
                            var y0 = (int)(f * row.Length * Math.Sin(angle));

                            if (x + x0 >= 0 && y + y0 >= 0 && x + x0 < binarWidth && y + y0 < binarHeight) { resultCube[f, y + y0, x + x0]++; };

                            x0 = (int)(f * row.Length * Math.Cos(angle + Math.PI));
                            y0 = (int)(f * row.Length * Math.Sin(angle + Math.PI));
                            if (x + x0 >= 0 && y + y0 >= 0 && x + x0 < binarWidth && y + y0 < binarHeight) { resultCube[f, y + y0, x + x0]++; };

                            //if (y + r < binarHeight) { resultCube[r, y + r, x]++; };
                            //if (x + r < binarWidth) { resultCube[r, y, x + r]++; };
                            //if (y - r >= 0) { resultCube[r, y - r, x]++; };
                            //if (x - r >= 0) { resultCube[r, y, x - r]++; };
                        }
                    }
                }
            }
            return resultCube;
        }

        public static TableRow GetTableRow(List<TableRow> rows, double angle)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].Angle == angle)
                {
                    return rows[i];
                }
            }

            return null;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //this.TopMost = true;
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;


            tabControl1.TabPages.Clear();
            tabControl1.Dock = DockStyle.Fill;
        }

        private void AddTabs(List<KeyValuePair<string, Bitmap>> tabs)
        {
            tabControl1.TabPages.Clear();

            for (int i = 0; i < tabs.Count; i++)
            {
                TabPage originalTab = new TabPage(tabs[i].Key);
                originalTab.Name = tabs[i].Key;
                tabControl1.TabPages.Add(originalTab);

                PictureBox pb = new PictureBox();
                tabControl1.SuspendLayout();
                tabControl1.TabPages[tabs[i].Key].Controls.Add(pb);
                tabControl1.ResumeLayout();
                pb.Parent = this.tabControl1.TabPages[tabs[i].Key];
                pb.Dock = DockStyle.Fill;
                pb.Image = tabs[i].Value;
                pb.BringToFront();
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}