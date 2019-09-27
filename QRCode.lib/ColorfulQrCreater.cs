using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using ZXing.Common;
using System.Collections.ObjectModel;
using System.Drawing.Drawing2D;
using System.Linq;

namespace QRCode.lib
{
    public class ProgressChangedEventArgs : EventArgs
    {
        public float Progress { get; set; }

        internal ProgressChangedEventArgs() { }
    }

    public class ColorfulQrCreater
    {
        public ColorfulQrCreater(ArgsModel args)
        {
            this.args = args;
            Background = Color.Black;
            Foreground = Color.White;

            qrCreator = new QrCreator(args);
            if (!qrCreator.IsCreated) return;
            LoadResource();
        }

        private ArgsModel args;
        private ConfigModel config;
        private ResourceModel resource;
        private Bitmap ResourceBitmap;
        private QrCreator qrCreator;
        private QrBlockList qrBlockList;
        private IList<IList<Bitmap>> QRImages;
        private IList<IList<Bitmap>> ThemeImages;
        private IList<IList<Bitmap>> AnimationThemeImages;
        private IList<IList<Bitmap>> MovableImages;
        private IList<IList<Bitmap>> StaticMovableImages;
        private const int BlockLength = 16;
        private const int MarginLength = 1;
        private Collection<Block> AnimationThemeBlocks;

        public Color Background { get; set; }
        public Color Foreground { get; set; }
        public bool[,] BoolMask { get => qrCreator.BoolMask; }
        public bool IsCreated { get => qrCreator.IsCreated; }
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        private void OnProgressChanged(float progress)
        {
            ProgressChanged?.Invoke(this, new ProgressChangedEventArgs() { Progress = progress });
        }

        public Bitmap DrawBitmap()
        {
            Random rnd = new Random();
            var bitmap = new Bitmap(qrCreator.Length * BlockLength, qrCreator.Length * BlockLength);
            var g = Graphics.FromImage(bitmap);
            Color color;
            if (QRImages.Count > 0)
                color = QRImages[0][0].GetPixel(0, 0);
            else color = Color.Black;
            g.Clear(color);

            //====
            //var R = 0;
            //var G = 0;
            //var B = 0;
            //foreach (var block in qrBlockList.QRBlockCollection)
            //{
            //    foreach (var point in block)
            //    {
            //        g.FillRectangle(new SolidBrush(Color.FromArgb(R, G, B)), new Rectangle(point.X * BlockLength, point.Y * BlockLength, BlockLength, BlockLength));
            //        //g.FillRectangle(new SolidBrush(Color.FromArgb(R, G, B)), new Rectangle(point.X * BlockLength, point.Y * BlockLength, BlockLength, BlockLength));
            //    }
            //    R = (R + 10) > 255 ? 0 : R + 10;
            //    G = (G + 10) > 255 ? 0 : G + 10;
            //    B = (B + 10) > 255 ? 0 : B + 10;
            //}
            //====
            foreach (var block in qrBlockList.ThemeBlockCollection)
            {
                int Index = -1;
                if (AnimationThemeImages.Count > 0)
                {
                    var mode = rnd.Next(0, AnimationThemeImages.Count + ThemeImages.Count);
                    if (mode < AnimationThemeImages.Count) mode = 0;
                    else mode = 1;
                    switch (mode)
                    {
                        case 0:
                            Index = rnd.Next(0, AnimationThemeImages.Count);
                            foreach (var point in block)
                            {
                                //g.DrawImage(ForegroundImages[index], new Rectangle(point.X * BlockLength, point.Y * BlockLength, BlockLength, BlockLength), 0, 0, ForegroundImages[index].Width, ForegroundImages[index].Height, GraphicsUnit.Pixel, ImageAttr);
                                DrawImage(g, new Rectangle(point.X * BlockLength, point.Y * BlockLength, BlockLength, BlockLength), AnimationThemeImages[Index][AnimationThemeImages[Index].Count / 2]);
                            }
                            AnimationThemeBlocks.Add(block);
                            break;
                        case 1:
                            Index = rnd.Next(0, ThemeImages.Count);
                            foreach (var point in block)
                            {
                                //g.DrawImage(ForegroundImages[index], new Rectangle(point.X * BlockLength, point.Y * BlockLength, BlockLength, BlockLength), 0, 0, ForegroundImages[index].Width, ForegroundImages[index].Height, GraphicsUnit.Pixel, ImageAttr);
                                DrawImage(g, new Rectangle(point.X * BlockLength, point.Y * BlockLength, BlockLength, BlockLength), ThemeImages[Index][0]);
                            }
                            break;
                    }
                }
                else
                {
                    Index = rnd.Next(0, ThemeImages.Count);
                    foreach (var point in block)
                    {
                        //g.DrawImage(ForegroundImages[index], new Rectangle(point.X * BlockLength, point.Y * BlockLength, BlockLength, BlockLength), 0, 0, ForegroundImages[index].Width, ForegroundImages[index].Height, GraphicsUnit.Pixel, ImageAttr);
                        DrawImage(g, new Rectangle(point.X * BlockLength, point.Y * BlockLength, BlockLength, BlockLength), ThemeImages[Index][0]);
                    }
                }
            }
            g.Dispose();
            g = null;
            return DrawMargin(bitmap, Color.FromArgb(127, 127, 127));
        }

        public void SavePng()
        {
            var bitmap = DrawBitmap();
            var folderPath = args.OutputPath.Substring(0, args.OutputPath.LastIndexOf('/'));
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
            bitmap.Save(args.OutputPath, ImageFormat.Png);
            bitmap.Dispose();
        }


        public void SaveGif()
        {
            float progress = 0f;
            OnProgressChanged(progress);

            var bitmap = DrawBitmap();
            if (args.Mode == Mode.Both)
            {
                bitmap.Save(args.OutputPath + ".png", ImageFormat.Png);
                args.OutputPath += ".gif";
            }

            var Count = CreateFrameArray(bitmap, TimeSpan.FromSeconds(args.Time), 5);

            NGif.AnimatedGifEncoder encoder = new NGif.AnimatedGifEncoder();
            if (File.Exists(args.OutputPath)) File.Delete(args.OutputPath);
            var file = File.Create(args.OutputPath);
            encoder.Start(file);
            encoder.SetSize(bitmap.Width, bitmap.Height);
            encoder.SetDelay(100);
            encoder.SetRepeat(0);

            progress = 10.8f;
            OnProgressChanged(progress);

            for (int i = 0; i < Count; i++)
            {
                var tempBitmap = (Bitmap)Image.FromFile($"temp/CreateQr/{i}");
                encoder.AddFrame(tempBitmap);
                tempBitmap.Dispose();

                progress = (float)i / Count * 87f + 11f;
                OnProgressChanged(progress);
            }
            encoder.Finish();

            progress = 99;
            OnProgressChanged(progress);

            bitmap.Dispose();
            if (Directory.Exists("temp"))
                Directory.Delete("temp", true);

            progress = 100;
            OnProgressChanged(progress);
        }

        private int CreateFrameArray(Bitmap bitmap, TimeSpan time, int Step)
        {
            var blocks = GetAnimationBlock();
            var points = new Collection<BlockPoint[]>();
            var objs = new Collection<IList<Bitmap>>();
            var rnd = new Random();

            var timeout = TimeSpan.FromSeconds(0.1d);
            int Count = (int)(time.Ticks / timeout.Ticks);
            var bitmapArray = new Bitmap[Count];

            if (StaticMovableImages.Count > 0)
            {
                for (int iobjs = 0; iobjs < blocks.Count - 1; iobjs++)
                {
                    var objsindex = rnd.Next(0, MovableImages.Count);
                    objs.Add(MovableImages[objsindex]);
                }
                objs.Add(StaticMovableImages[0]);
            }
            else
            {
                for (int iobjs = 0; iobjs < blocks.Count; iobjs++)
                {
                    var objsindex = rnd.Next(0, MovableImages.Count);
                    objs.Add(MovableImages[objsindex]);
                }
            }
            for (int iFrame = 0; iFrame < bitmapArray.Length; iFrame += Step)
            {
                for (int iBlock = 0; iBlock < blocks.Count; iBlock++)
                {
                    BlockPoint prevPoint = null;
                    BlockPoint nextPoint = null;
                    if (points.Count <= iBlock)
                    {
                        if (resource.IsGravityEnable)
                        {
                            var select = blocks[iBlock].Where(x =>
                            {
                                if (!blocks[iBlock].Contains(x.BelowPoint))
                                {
                                    if (blocks[iBlock].Contains(x.LeftPoint))
                                        if (!blocks[iBlock].Contains(x.LeftPoint.BelowPoint)) return true;
                                    if (blocks[iBlock].Contains(x.RightPoint))
                                        if (!blocks[iBlock].Contains(x.RightPoint.BelowPoint)) return true;
                                }
                                return false;
                            }).ToList();
                            var selectCount = select.Count();
                            if (selectCount > 0)
                            {
                                var pointindex = rnd.Next(selectCount);
                                points.Add(new BlockPoint[2] { select[pointindex], select[pointindex] });
                            }
                            else
                            {
                                var select2 = blocks[iBlock].Where(x =>
                                {
                                    if (!blocks[iBlock].Contains(x.BelowPoint)) return true;
                                    return false;
                                }).ToList();
                                var selectCount2 = select2.Count();
                                if (selectCount2 > 0)
                                {
                                    var pointindex = rnd.Next(selectCount2);
                                    points.Add(new BlockPoint[2] { select2[pointindex], select2[pointindex] });
                                }
                                else
                                {
                                    var pointindex = rnd.Next(blocks[iBlock].Count);
                                    points.Add(new BlockPoint[2] { blocks[iBlock][pointindex], blocks[iBlock][pointindex] });
                                }
                            }

                            //var NotFound = true;
                            //var FindCount = 0;
                            //while (NotFound)
                            //{
                            //    var pointindex = rnd.Next(blocks[iBlock].Count - 1);
                            //    if (!blocks[iBlock].Contains(blocks[iBlock][pointindex].BelowPoint) &&
                            //        (blocks[iBlock].Contains(blocks[iBlock][pointindex].LeftPoint) ||
                            //        blocks[iBlock].Contains(blocks[iBlock][pointindex].RightPoint)))
                            //    {
                            //        points.Add(new BlockPoint[2] { blocks[iBlock][pointindex], blocks[iBlock][pointindex] });
                            //        NotFound = false;
                            //    }
                            //    FindCount++;
                            //    if (FindCount > 100)
                            //    {
                            //        pointindex = rnd.Next(blocks[iBlock].Count - 1);
                            //        points.Add(new BlockPoint[2] { blocks[iBlock][pointindex], blocks[iBlock][pointindex] });
                            //    }
                            //}
                        }
                        else
                        {
                            var pointindex = rnd.Next(blocks[iBlock].Count);
                            points.Add(new BlockPoint[2] { blocks[iBlock][pointindex], blocks[iBlock][pointindex] });
                        }
                    }
                    prevPoint = points[iBlock][1];
                    //while (nextPoint == null)
                    //{
                    //Arrow prevArrow = GetArrow(points[iBlock][1], points[iBlock][0]);
                    //Arrow nextArrow = (Arrow)(rnd.Next(0, 3));



                    nextPoint = GetNextPoint(blocks[iBlock], points[iBlock], resource.IsGravityEnable);

                    var resourceBlockCount = 0;

                    for (float progress = 0; progress < 1; progress += 1f / Step)
                    {
                        var tempBitmap = bitmapArray[(int)(iFrame + progress * Step)];
                        if (tempBitmap == null) tempBitmap = new Bitmap(bitmap);
                        DrawAnimationObject(tempBitmap, objs[iBlock][resourceBlockCount], prevPoint, nextPoint, progress);
                        //objs[iBlock][resourceBlockCount].Save("1/" + (int)(iFrame + progress * Step) + ".png", ImageFormat.Png);
                        bitmapArray[(int)(iFrame + progress * Step)] = tempBitmap;
                        if (++resourceBlockCount >= objs[iBlock].Count) resourceBlockCount = 0;
                    }
                    points[iBlock][0] = points[iBlock][1];
                    points[iBlock][1] = nextPoint;
                }

                if (AnimationThemeBlocks.Count > 0)
                {
                    foreach (var block in AnimationThemeBlocks)
                    {
                        var tempThemeIndex = 0;
                        var themeIndex = rnd.Next(AnimationThemeImages.Count);
                        for (float progress = 0; progress < 1; progress += 1f / Step)
                        {
                            var tempBitmap = bitmapArray[(int)(iFrame + progress * Step)];
                            var g = Graphics.FromImage(tempBitmap);
                            var tempThemeBitmap = AnimationThemeImages[themeIndex][tempThemeIndex];
                            foreach (var point in block)
                            {
                                DrawImage(g, new Rectangle((point.X + MarginLength) * BlockLength, (point.Y + MarginLength) * BlockLength, BlockLength, BlockLength), tempThemeBitmap);
                            }
                            g.Dispose();
                            if (++tempThemeIndex >= AnimationThemeImages[themeIndex].Count) tempThemeIndex = 0;
                        }
                    }
                }
                for (float progress = 0; progress < 1; progress += 1f / Step)
                {
                    var tempBitmap = bitmapArray[(int)(iFrame + progress * Step)];
                    if (!Directory.Exists("temp/CreateQr")) Directory.CreateDirectory("temp/CreateQr");
                    tempBitmap.Save($"temp/CreateQr/{(int)(iFrame + progress * Step)}", ImageFormat.Bmp);
                    tempBitmap.Dispose();
                    tempBitmap = null;
                }
                OnProgressChanged((float)iFrame / bitmapArray.Length * 10f);
            }
            return Count;
        }

        private BlockPoint GetNextPoint(Block block, BlockPoint[] prevPoint, bool IsGravityEnable)
        {
            var nextPoints = new Collection<BlockPoint>();
            var inLinePoints = new Collection<BlockPoint>();

            if (block.Contains(prevPoint[1].LeftPoint))
            {
                if (InLine(prevPoint[0], prevPoint[1], prevPoint[1].LeftPoint))
                    inLinePoints.Add(prevPoint[1].LeftPoint);
                else nextPoints.Add(prevPoint[1].LeftPoint);
            }

            if (block.Contains(prevPoint[1].AbovePoint) && !IsGravityEnable)
            {
                if (InLine(prevPoint[0], prevPoint[1], prevPoint[1].AbovePoint))
                    inLinePoints.Add(prevPoint[1].AbovePoint);
                else nextPoints.Add(prevPoint[1].AbovePoint);
            }

            if (block.Contains(prevPoint[1].RightPoint))
            {
                if (InLine(prevPoint[0], prevPoint[1], prevPoint[1].RightPoint))
                    inLinePoints.Add(prevPoint[1].RightPoint);
                else nextPoints.Add(prevPoint[1].RightPoint);
            }

            if (block.Contains(prevPoint[1].BelowPoint) && !IsGravityEnable)
            {
                if (InLine(prevPoint[0], prevPoint[1], prevPoint[1].BelowPoint))
                    inLinePoints.Add(prevPoint[1].BelowPoint);
                else nextPoints.Add(prevPoint[1].BelowPoint);
            }

            if (nextPoints.Count == 0)
            {
                foreach (var temppoint in inLinePoints)
                {
                    if (temppoint != prevPoint[0]) nextPoints.Add(temppoint);
                }
                if (nextPoints.Count == 0) nextPoints.Add(prevPoint[0]);
            }

            if (IsGravityEnable)
            {
                var results = new Collection<BlockPoint>();
                foreach (var temppoint in nextPoints)
                {
                    if (!block.Contains(temppoint.BelowPoint))
                        results.Add(temppoint);
                }
                nextPoints = results;
            }
            if (nextPoints.Count > 0)
            {
                var rnd = new Random();
                var nextPointIndex = rnd.Next(nextPoints.Count);
                return nextPoints[nextPointIndex];
            }
            else return prevPoint[0];
        }

        private Collection<Block> GetAnimationBlock()
        {
            var rnd = new Random();

            //var blockIndex = rnd.Next(0, qrBlockList.ForegroundBlockList.Count - 1);
            var minobjCount = qrCreator.Length / 6;
            var maxobjCount = qrCreator.Length / 3;
            var objCount = rnd.Next(minobjCount == 0 ? 1 : minobjCount, maxobjCount);
            var blocks = new Collection<Block>();
            int whileCount = 0;
            while (blocks.Count <= objCount && whileCount < 10000)
            {
                var tempIndex = rnd.Next(0, qrBlockList.QRBlockCollection.Count);
                bool NotResult = true;
                for (int i = 0; i < qrBlockList.QRBlockCollection[tempIndex].Count && NotResult; i++)
                {
                    for (int j = 0; j < qrCreator.ResultPointBlocks.Length && NotResult; j++)
                    {
                        if (qrCreator.ResultPointBlocks[j].Contains(qrBlockList.QRBlockCollection[tempIndex][i]))
                            NotResult = false;
                    }
                }
                if (NotResult)
                {
                    if (qrBlockList.QRBlockCollection[tempIndex].Count > 5 && !blocks.Contains(qrBlockList.QRBlockCollection[tempIndex]))
                    {
                        if (resource.IsGravityEnable)
                        {
                            var select = qrBlockList.QRBlockCollection[tempIndex].Select(x =>
                            {
                                if (!qrBlockList.QRBlockCollection[tempIndex].Contains(x.BelowPoint))
                                {
                                    if (qrBlockList.QRBlockCollection[tempIndex].Contains(x.LeftPoint))
                                        if (!qrBlockList.QRBlockCollection[tempIndex].Contains(x.LeftPoint.BelowPoint)) return x;
                                    if (qrBlockList.QRBlockCollection[tempIndex].Contains(x.RightPoint))
                                        if (!qrBlockList.QRBlockCollection[tempIndex].Contains(x.RightPoint.BelowPoint)) return x;
                                }
                                return null;
                            });
                            if (select.Count() != 0) blocks.Add(qrBlockList.QRBlockCollection[tempIndex]);
                        }
                        else
                        {
                            blocks.Add(qrBlockList.QRBlockCollection[tempIndex]);
                        }
                    }
                }
                whileCount++;
            }
            return blocks;
        }

        private Arrow GetArrow(BlockPoint prevPoint, BlockPoint nextPoint)
        {
            Arrow arrow = Arrow.Null;
            if (prevPoint.X == nextPoint.X)
            {
                if (nextPoint.Y > prevPoint.Y) arrow = Arrow.Down;
                else if (nextPoint.Y < prevPoint.Y) arrow = Arrow.Up;
            }
            else if (prevPoint.Y == nextPoint.Y)
            {
                if (nextPoint.X > prevPoint.X) arrow = Arrow.Right;
                else if (nextPoint.X < prevPoint.X) arrow = Arrow.Left;
            }
            return arrow;
        }

        private bool InLine(Arrow prevArrow, Arrow nextArrow)
        {
            if (prevArrow == nextArrow) return false;
            var tempPrevArrow = Arrow.Null;
            var tempNextArrow = Arrow.Null;
            switch (prevArrow)
            {
                case Arrow.Left:
                case Arrow.Right:
                    tempPrevArrow = Arrow.Left;
                    break;
                case Arrow.Up:
                case Arrow.Down:
                    tempPrevArrow = Arrow.Up;
                    break;
            }
            switch (nextArrow)
            {
                case Arrow.Left:
                case Arrow.Right:
                    tempNextArrow = Arrow.Left;
                    break;
                case Arrow.Up:
                case Arrow.Down:
                    tempNextArrow = Arrow.Up;
                    break;
            }
            if (tempPrevArrow == tempNextArrow) return true;
            else return false;
        }

        private bool InLine(BlockPoint prevPoint, BlockPoint midPoint, BlockPoint nextPoint)
        {
            return InLine(GetArrow(prevPoint, midPoint), GetArrow(midPoint, nextPoint));
        }

        private void DrawAnimationObject(Bitmap bitmap, Bitmap resource, BlockPoint prevPoint, BlockPoint nextPoint, float progress)
        {
            Arrow arrow = GetArrow(prevPoint, nextPoint);
            Bitmap tempRes = new Bitmap(resource.Width, resource.Height);
            var g = Graphics.FromImage(tempRes);
            g.DrawImage(resource, new Point(0, 0));
            g.Dispose();
            int offset = (int)(BlockLength * progress);
            Rectangle rect = new Rectangle(0, 0, resource.Width, resource.Height);
            if (arrow == Arrow.Null)
            {
                rect = new Rectangle((prevPoint.X + MarginLength) * BlockLength, (prevPoint.Y + MarginLength) * BlockLength, BlockLength, BlockLength);
            }
            if (arrow == Arrow.Up)
            {
                rect = new Rectangle((prevPoint.X + MarginLength) * BlockLength, (prevPoint.Y + MarginLength) * BlockLength - offset, BlockLength, BlockLength);
            }
            else
            {
                //var g = Graphics.FromImage(tempRes);
                float x = 0;
                float y = 0;
                switch (arrow)
                {
                    case Arrow.Left:
                        if (this.resource.IsRotateEnable)
                            tempRes.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        y = resource.Height;
                        rect = new Rectangle((prevPoint.X + MarginLength) * BlockLength - offset, (prevPoint.Y + MarginLength) * BlockLength, BlockLength, BlockLength);
                        break;
                    case Arrow.Right:
                        if (this.resource.IsRotateEnable)
                            tempRes.RotateFlip(RotateFlipType.Rotate90FlipNone);
                        else tempRes.RotateFlip(RotateFlipType.RotateNoneFlipX);
                        x = resource.Width;
                        rect = new Rectangle((prevPoint.X + MarginLength) * BlockLength + offset, (prevPoint.Y + MarginLength) * BlockLength, BlockLength, BlockLength);
                        break;
                    case Arrow.Down:
                        if (this.resource.IsRotateEnable)
                            tempRes.RotateFlip(RotateFlipType.Rotate180FlipNone);
                        x = resource.Width;
                        y = resource.Height;
                        rect = new Rectangle((prevPoint.X + MarginLength) * BlockLength, (prevPoint.Y + MarginLength) * BlockLength + offset, BlockLength, BlockLength);
                        break;
                }
                //if (this.resource.IsRotateEnable)
                //{
                //    g.TranslateTransform(x, y);
                //    g.RotateTransform(a);
                //}
                //else
                //{
                //    if(arrow == Arrow.Right)
                //    {
                //        tempRes.RotateFlip(RotateFlipType.RotateNoneFlipX);
                //    }
                //}
                //g.DrawImage(resource, new Rectangle(0, 0, resource.Width, resource.Height));
                //g.Dispose();
            }
            var g1 = Graphics.FromImage(bitmap);
            DrawImage(g1, rect, tempRes);
            g1.Dispose();
        }

        private void DrawImage(Graphics g, Rectangle destRect, Bitmap source)
        {
            var PixelLength = BlockLength / (float)source.Width;
            for (int x = 0; x < source.Width; x++)
            {
                for (int y = 0; y < source.Height; y++)
                {
                    g.FillRectangle(new SolidBrush(source.GetPixel(x, y)), x * PixelLength + destRect.X, y * PixelLength + destRect.Y, PixelLength, PixelLength);
                    //g.FillRectangle(new SolidBrush(source.GetPixel(x, y)), new Rectangle(x * PixelLength + destRect.X, y * PixelLength + destRect.Y, PixelLength, PixelLength));
                }
            }
        }

        private Bitmap DrawMargin(Bitmap source, Bitmap resource)
        {
            var bitmap = new Bitmap(source.Width + (BlockLength * MarginLength * 2), source.Height + (BlockLength * MarginLength * 2));
            var g = Graphics.FromImage(bitmap);
            for (int x = 0; x < qrCreator.Length + 2; x++)
            {
                for (int y = 0; y < qrCreator.Length + 2; y++)
                {
                    if (x < MarginLength || y < MarginLength || x > qrCreator.Length + (MarginLength - 1) || y > qrCreator.Length + (MarginLength - 1))
                    {
                        DrawImage(g, new Rectangle(x * BlockLength, y * BlockLength, BlockLength, BlockLength), resource);
                    }
                }
            }
            g.DrawImage(source, new Rectangle(BlockLength, BlockLength, qrCreator.Length * BlockLength, qrCreator.Length * BlockLength));
            g.Dispose();
            source.Dispose();
            return bitmap;
        }

        private Bitmap DrawMargin(Bitmap source, Color color)
        {
            var bitmap = new Bitmap(source.Width + (BlockLength * MarginLength * 2), source.Height + (BlockLength * MarginLength * 2));
            var g = Graphics.FromImage(bitmap);
            for (int x = 0; x < qrCreator.Length + 2; x++)
            {
                for (int y = 0; y < qrCreator.Length + 2; y++)
                {
                    if (x < MarginLength || y < MarginLength || x > qrCreator.Length + (MarginLength - 1) || y > qrCreator.Length + (MarginLength - 1))
                    {
                        g.FillRectangle(new SolidBrush(color), new Rectangle(x * BlockLength, y * BlockLength, BlockLength, BlockLength));
                    }
                }
            }
            g.DrawImage(source, new Rectangle(BlockLength, BlockLength, qrCreator.Length * BlockLength, qrCreator.Length * BlockLength));
            g.Dispose();
            source.Dispose();
            return bitmap;
        }

        private void LoadResource()
        {
            config = ConfigModel.GetConfig("config.xml");
            resource = config.GetTheme(args.Theme);
            if (resource == null)
            {
                ConsoleHelper.WriteLine(ConsoleHelper.Format.Error, "Resource");
                return;
            }
            qrBlockList = new QrBlockList(BoolMask, 25);
            ResourceBitmap = (Bitmap)Image.FromFile(resource.Path);
            var map = new ColorMap() { OldColor = Color.Pink, NewColor = Color.Transparent };
            ThemeImages = CreateImageBlockList(ResourceBlockMode.Theme, map);
            AnimationThemeImages = CreateImageBlockList(ResourceBlockMode.AnimationTheme, map);
            QRImages = CreateImageBlockList(ResourceBlockMode.QR);
            MovableImages = CreateImageBlockList(ResourceBlockMode.Movable, map);
            StaticMovableImages = CreateImageBlockList(ResourceBlockMode.StaticMovable, map);
            AnimationThemeBlocks = new Collection<Block>();
        }

        private byte[] GetPixelBuffer(Bitmap bitmap)
        {
            var buffer = new byte[bitmap.Width * bitmap.Height];
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            Marshal.Copy(bitmapData.Scan0, buffer, 0, buffer.Length);
            bitmap.UnlockBits(bitmapData);
            return buffer;
        }

        private IList<IList<Bitmap>> CreateImageBlockList(ResourceBlockMode Mode, ColorMap map = null)
        {
            var bitmapslist = new List<IList<Bitmap>>();
            var blockModels = new List<ResourceBlockModel>();
            foreach (var blockModel in config.GetTheme(args.Theme).Block)
            {
                if (blockModel.Mode == Mode)
                {
                    bitmapslist.Add(CutImageBlock(blockModel, map));
                }
            }
            return bitmapslist;
        }

        private IList<Bitmap> CutImageBlock(ResourceBlockModel blockModel, ColorMap map = null)
        {
            var Images = new List<Bitmap>();
            var IsImage = true;
            var x = blockModel.X;
            var y = blockModel.Y;
            var blockLength = blockModel.Height;
            while (IsImage)
            {
                if (x + blockLength > blockModel.X + blockModel.Width || y + blockLength > blockModel.Y + blockModel.Height) IsImage = false;
                else
                {
                    var bitmap = new Bitmap(blockLength, blockLength);
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.Clear(Color.Transparent);
                        ImageAttributes imageAttr = new ImageAttributes();
                        if (map != null)
                        {
                            ColorMap[] remapTable = { map };
                            imageAttr.SetRemapTable(remapTable, ColorAdjustType.Bitmap);
                        }
                        g.DrawImage(ResourceBitmap, new Rectangle(0, 0, blockLength, blockLength), x, y, blockLength, blockLength, GraphicsUnit.Pixel, imageAttr);
                        var buffer = GetPixelBuffer(bitmap);
                        var NotNull = true;
                        for (int i = 0; i < buffer.Length - 2; i += 3)
                        {
                            if (buffer[i] != 0 || buffer[i + 1] != 0 || buffer[i + 2] != 0)
                            {
                                NotNull = true;
                            }
                        }
                        if (NotNull)
                        {
                            x += blockLength;
                            Images.Add(bitmap);
                        }
                        else
                        {
                            bitmap.Dispose();
                            bitmap = null;
                            IsImage = false;
                        }
                    }
                }
            }
            return Images;
        }

        //private IList<Bitmap> CutImagesLine(Bitmap source, int OffsetY, int Width, int Height, ColorMap map = null)
        //{
        //    var Images = new List<Bitmap>();
        //    var IsImage = true;
        //    int x = 0, y = OffsetY;
        //    while (IsImage)
        //    {
        //        if (x + Width > source.Width || y + Height > source.Height) IsImage = false;
        //        else
        //        {
        //            var bitmap = new Bitmap(Width, Height);
        //            using (Graphics g = Graphics.FromImage(bitmap))
        //            {
        //                g.Clear(Color.Transparent);
        //                ImageAttributes imageAttr = new ImageAttributes();
        //                if (map != null)
        //                {
        //                    ColorMap[] remapTable = { map };
        //                    imageAttr.SetRemapTable(remapTable, ColorAdjustType.Bitmap);
        //                }
        //                g.DrawImage(source, new Rectangle(0, 0, Width, Height), x, y, Width, Height, GraphicsUnit.Pixel, imageAttr);
        //                var buffer = GetPixelBuffer(bitmap);
        //                bool NotNull = false;
        //                for (int i = 0; i < buffer.Length - 2; i += 3)
        //                {
        //                    if (buffer[i] != 0 || buffer[i + 1] != 0 || buffer[i + 2] != 0)
        //                    {
        //                        NotNull = true;
        //                    }
        //                }
        //                if (NotNull)
        //                {
        //                    x += Width;
        //                    Images.Add(bitmap);
        //                }
        //                else
        //                {
        //                    bitmap.Dispose();
        //                    bitmap = null;
        //                    IsImage = false;
        //                }
        //            }
        //        }
        //    }
        //    return Images;
        //}

        enum Arrow
        {
            Null = -1,
            Left = 0,
            Up = 1,
            Right = 2,
            Down = 3,
        }
    }
}
