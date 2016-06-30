using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;

namespace CiecleHoughCSharp
{
    public class TableRow
    {
        public double Length;

        public double Angle;

        public TableRow(double angle, double length)
        {
            Length = length;
            Angle = angle;
        }
    }

    public class Table
    {
        public static readonly List<TableRow> Square = new List<TableRow>()
        {
            new TableRow(0, 1),
            new TableRow(Math.PI / 4, Math.Sqrt(2.0d)),
            new TableRow(Math.PI / 2, 1),
            new TableRow(3 * Math.PI / 4, Math.Sqrt(2.0d)),

            new TableRow(Math.PI, 1),
            new TableRow(- Math.PI / 4, 1),
            new TableRow(- Math.PI / 2, 1),
            new TableRow(- 3 * Math.PI / 4, Math.Sqrt(2.0d)),
        };
    }
}
