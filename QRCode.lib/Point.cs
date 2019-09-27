using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace QRCode.lib
{
    public class BlockPoint
    {
        private int x;
        private int y;
        private BlockPoint prevPoint;
        private BlockPoint nextPoint;


        public int X { get => x; set => x = value; }
        public int Y { get => y; set => y = value; }

        public BlockPoint() { }
        public BlockPoint(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public override bool Equals(object obj)
        {
            var point = (BlockPoint)obj;
            if (point.x == this.x && point.y == this.y) return true;
            else return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return $"x={x}, y={y}";
        }

        public BlockPoint LeftPoint { get => new BlockPoint(x - 1, y); }
        public BlockPoint RightPoint { get => new BlockPoint(x + 1, y); }
        public BlockPoint AbovePoint { get => new BlockPoint(x, y - 1); }
        public BlockPoint BelowPoint { get => new BlockPoint(x, y + 1); }


        public BlockPoint PrevPoint { get => prevPoint; set => prevPoint = value; }
        public BlockPoint NextPoint { get => nextPoint; set => nextPoint = value; }
    }

    public static class BoolMatrixExtensions
    {
        public static bool GetValue(this bool[,] boolMatrix, BlockPoint point)
        {
            return boolMatrix[point.X, point.Y];
        }

        public static void SetValue(this bool[,] boolMatrix, BlockPoint point,bool Value)
        {
            boolMatrix[point.X, point.Y] = Value;
        }

        public static bool Contains(this bool[,] boolMatrix,BlockPoint point)
        {
            if (point.X < boolMatrix.GetLength(0) &&
                point.X > -1 &&
                point.Y < boolMatrix.GetLength(1) &&
                point.Y > -1)
                return true;
            else return false;
        }
    }
}
