using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace QRCode.lib
{
    public class QrBlockList
    {
        private Collection<Block> qRBlockCollection;
        private Collection<Block> themeBlockCollection;
        private bool[,] BoolMask;
        private bool[,] MaskBoolIsSet;
        private int MaxLength = -1;

        public Collection<Block> QRBlockCollection { get => qRBlockCollection; }
        public Collection<Block> ThemeBlockCollection { get => themeBlockCollection; }

        public QrBlockList(bool[,] BoolMask) : this(BoolMask, -1) { }

        public QrBlockList(bool[,] BoolMask, int MaxLength)
        {
            this.BoolMask = BoolMask;
            this.MaxLength = MaxLength;
            MaskBoolIsSet = new bool[BoolMask.GetLength(0), BoolMask.GetLength(1)];
            qRBlockCollection = new Collection<Block>();
            themeBlockCollection = new Collection<Block>();
            InitBlockList();
        }

        private void InitBlockList()
        {
            var Length = BoolMask.GetLength(0);
            qRBlockCollection = new Collection<Block>();
            themeBlockCollection = new Collection<Block>();
            for (int x = 0; x < Length; x++)
            {
                for (int y = 0; y < Length; y++)
                {
                    if (BoolMask[x, y]) AddPoint(qRBlockCollection, new BlockPoint(x, y));
                    else AddPoint(themeBlockCollection, new BlockPoint(x, y), MaxLength);
                }
            }
        }

        private void AddPoint(Collection<Block> BlockCollection, BlockPoint point, int MaxCount = -1)
        {
            var value = BoolMask.GetValue(point);
            var Index = -1;
            if (MaskBoolIsSet.GetValue(point))
                Index = InBlock(BlockCollection, point);
            else
            {
                var tempBlock = new Block();
                tempBlock.Add(point);
                MaskBoolIsSet.SetValue(point, true);
                Index = BlockCollection.Count;
                BlockCollection.Add(tempBlock);
            }
            var block = BlockCollection[Index];
            AddPointCore(block, point, value, MaxCount);
        }

        private void AddPointCore(Block Block, BlockPoint point, bool value, int MaxCount = -1)
        {
            if (MaxCount != -1 && Block.Count >= MaxCount) return;
            var nextPoints = GetOtherPoint(point);
            for (int i = 0; i < nextPoints.Length; i++)
            {
                if (nextPoints[i] != null && !Block.Contains(nextPoints[i]) && BoolMask.GetValue(nextPoints[i]) == value && !MaskBoolIsSet.GetValue(nextPoints[i]))
                {
                    Block.Add(nextPoints[i]);
                    MaskBoolIsSet.SetValue(nextPoints[i], true);
                    AddPointCore(Block, nextPoints[i], value, MaxCount);
                }
            }
        }

        private BlockPoint[] GetOtherPoint(BlockPoint point)
        {
            var nextPoints = new BlockPoint[4];
            if (BoolMask.Contains(point.LeftPoint)) nextPoints[0] = point.LeftPoint;
            if (BoolMask.Contains(point.AbovePoint)) nextPoints[1] = point.AbovePoint;
            if (BoolMask.Contains(point.RightPoint)) nextPoints[2] = point.RightPoint;
            if (BoolMask.Contains(point.BelowPoint)) nextPoints[3] = point.BelowPoint;
            return nextPoints;
        }

        private int InBlock(Collection<Block> BlockCollection, BlockPoint point)
        {
            int Index = -1;
            for (int i = 0; i < BlockCollection.Count && Index < 0; i++)
            {
                var Block = BlockCollection[i];
                if (Block.Contains(point)) Index = i;
            }
            return Index;
        }
    }
}
