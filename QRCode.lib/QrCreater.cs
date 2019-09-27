using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using ZXing;
using ZXing.Common;
using ZXing.CoreCompat.System.Drawing;
using ZXing.QrCode;
using ZXing.QrCode.Internal;
using ZXing.Rendering;

namespace QRCode.lib
{
    public enum ErrorCorrectionLevel
    {
        L = 7, M = 15, Q = 25, H = 30
    }
    public class QrCreator
    {
        public QrCreator(ArgsModel args, ErrorCorrectionLevel level = ErrorCorrectionLevel.H, int Width = 500, int Height = 500)
        {
            switch (args.LoadMode)
            {
                case LoadMode.Text:
                    CreateFromContent(args.Content, level, Width, Height);
                    break;
                case LoadMode.Image:
                    CreateFromImageFile(args.Content, level, Width, Height);
                    break;
            }
        }

        private BitMatrix bitMatrix;
        private BitMatrix bitMatrixNoMargin;
        private Bitmap bitmap;
        private PixelData pixelData;
        private bool[,] boolMask;
        private int unitLength;
        private int length;
        private ResultPoint[] resultPoints;
        private Block[] resultPointBlocks;
        private bool isCreated;

        public static PixelData ToPixelData(string content, ErrorCorrectionLevel level = ErrorCorrectionLevel.H, int Width = 500, int Height = 500)
        {
            BarcodeWriterPixelData writer = new BarcodeWriterPixelData();
            ZXing.QrCode.Internal.ErrorCorrectionLevel errorCorrectionLevel;
            switch (level)
            {
                case ErrorCorrectionLevel.L:
                    errorCorrectionLevel = ZXing.QrCode.Internal.ErrorCorrectionLevel.L;
                    break;
                case ErrorCorrectionLevel.M:
                    errorCorrectionLevel = ZXing.QrCode.Internal.ErrorCorrectionLevel.M;
                    break;
                case ErrorCorrectionLevel.Q:
                    errorCorrectionLevel = ZXing.QrCode.Internal.ErrorCorrectionLevel.Q;
                    break;
                case ErrorCorrectionLevel.H:
                default:
                    errorCorrectionLevel = ZXing.QrCode.Internal.ErrorCorrectionLevel.H;
                    break;
            }
            writer.Format = BarcodeFormat.QR_CODE;
            writer.Options = new QrCodeEncodingOptions
            {
                CharacterSet = "UTF-8",
                Width = Width,
                Height = Height,
                ErrorCorrection = errorCorrectionLevel,
                Margin = 0,
            };
            var pixel = writer.Write(content);
            return pixel;
        }

        public static Bitmap ToBitmap(PixelData pixelData)
        {
            var bitmap = new Bitmap(pixelData.Width, pixelData.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            var bitmapdata = bitmap.LockBits(new Rectangle(0, 0, pixelData.Width, pixelData.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            try
            {
                Marshal.Copy(pixelData.Pixels, 0, bitmapdata.Scan0, pixelData.Pixels.Length);
            }
            finally
            {
                bitmap.UnlockBits(bitmapdata);
            }
            return bitmap;
        }

        public static Bitmap ToBitmap(BitMatrix bitMatrix, int Margin = 0)
        {
            PixelDataRenderer renderer = new PixelDataRenderer();
            var options = new EncodingOptions()
            {
                Margin = Margin
            };
            var pixelData = renderer.Render(bitMatrix, BarcodeFormat.QR_CODE, null, options);
            return ToBitmap(pixelData);
        }

        public static BitMatrix RemoveMargin(BitMatrix bitMatrix)
        {
            var marginLeftBitCount = 0;
            var marginRightBitCount = 0;
            var marginTopBitCount = 0;
            var marginBottomBitCount = 0;
            //top
            var isnull = true;
            for (int y = 0; y < bitMatrix.Height / 2 && isnull; y++)
            {
                for (int x = 0; x < bitMatrix.Width / 2 && isnull; x++)
                {
                    if (bitMatrix[x, y])
                    {
                        marginTopBitCount = y;
                        isnull = false;
                    }
                }
            }
            //left
            isnull = true;
            for (int x = 0; x < bitMatrix.Width / 2 && isnull; x++)
            {
                for (int y = 0; y < bitMatrix.Height / 2 && isnull; y++)
                {
                    if (bitMatrix[x, y])
                    {
                        marginLeftBitCount = x;
                        isnull = false;
                    }
                }
            }
            //right
            isnull = true;
            for (int x = bitMatrix.Width - 1; x > bitMatrix.Width / 2 && x < bitMatrix.Width && isnull; x--)
            {
                for (int y = 0; y < bitMatrix.Height / 2 && isnull; y++)
                {
                    if (bitMatrix[x, y])
                    {
                        marginRightBitCount = bitMatrix.Width - 1 - x;
                        isnull = false;
                    }
                }
            }
            //bottom
            isnull = true;
            for (int y = bitMatrix.Height - 1; y > bitMatrix.Height / 2 && y < bitMatrix.Height && isnull; y--)
            {
                for (int x = 0; x < bitMatrix.Width / 2 && isnull; x++)
                {
                    if (bitMatrix[x, y])
                    {
                        marginBottomBitCount = bitMatrix.Height - 1 - y;
                        isnull = false;
                    }
                }
            }

            var marginLeft = marginLeftBitCount / 4;
            var marginRight = marginRightBitCount / 4;
            var marginTop = marginTopBitCount / 4;
            var marginBottom = marginBottomBitCount / 4;

            bool[][] sourceArr = new bool[(bitMatrix.Width - marginLeftBitCount - marginRightBitCount)][];

            for (int x = 0; x < (bitMatrix.Width - marginLeftBitCount - marginRightBitCount); x++)
            {
                sourceArr[x] = new bool[(bitMatrix.Height - marginTopBitCount - marginBottomBitCount)];
                for (int y = 0; y < (bitMatrix.Height - marginTopBitCount - marginBottomBitCount); y++)
                {
                    sourceArr[x][y] = bitMatrix[marginLeftBitCount + x, marginTopBitCount + y];
                }
            }

            return BitMatrix.parse(sourceArr);
        }

        public BitMatrix GetBitMatrix(bool IsMarginEnable)
        {
            if (IsMarginEnable) return bitMatrix;
            else return bitMatrixNoMargin;
        }

        public bool[,] BoolMask { get => boolMask; }

        public int Length { get => length; }

        public int UnitLength { get => unitLength; }

        public Block[] ResultPointBlocks { get => resultPointBlocks; }
        public bool IsCreated { get => isCreated; set => isCreated = value; }

        private void Create()
        {
            var source = new BitmapLuminanceSource(bitmap);
            var hybridBinarizer = new HybridBinarizer(source);
            bitMatrix = hybridBinarizer.BlackMatrix;
            bitMatrixNoMargin = RemoveMargin(bitMatrix);
            CreateBoolMask(bitMatrix);
            CreatetResultPointBlock();
        }

        private void CreateFromContent(string content, ErrorCorrectionLevel level = ErrorCorrectionLevel.H, int Width = 500, int Height = 500)
        {
            pixelData = ToPixelData(content, level, Width, Height);
            bitmap = ToBitmap(pixelData);
            Create();
            isCreated = true;
        }

        private void CreateFromBitmap(Bitmap bitmap, ErrorCorrectionLevel level = ErrorCorrectionLevel.H, int Width = 500, int Height = 500)
        {
            var reader = new ZXing.BarcodeReader();
            var result = reader.Decode(bitmap);

            if (result != null)
            {
                CreateFromContent(result.Text, level, Width, Height);
            }
            else
            {
                ConsoleHelper.WriteLine(ConsoleHelper.Format.Error, "Connot read this image");
                isCreated = false;
            }
        }

        private void CreateFromImageFile(string Path, ErrorCorrectionLevel level = ErrorCorrectionLevel.H, int Width = 500, int Height = 500)
        {
            var bitmap = (Bitmap)Image.FromFile(Path);
            CreateFromBitmap(bitmap);
            bitmap.Dispose();
            bitmap = null;
        }

        private void CreatetResultPointBlock()
        {
            var LeftTopBlock = new Block();
            var LeftBottomBlock = new Block();
            var RightTopBlock = new Block();
            var LittleRightBottomBlock = new Block();
            for (int x = 0; x < 7; x++)
            {
                for (int y = 0; y < 7; y++)
                {
                    LeftTopBlock.Add(new BlockPoint(x, y));
                }
            }

            for (int x = 0; x < 7; x++)
            {
                for (int y = length - 1; y > length - 8; y--)
                {
                    LeftBottomBlock.Add(new BlockPoint(x, y));
                }
            }

            for (int x = length - 1; x > length - 8; x--)
            {
                for (int y = 0; y < 7; y++)
                {
                    RightTopBlock.Add(new BlockPoint(x, y));
                }
            }

            ResultPoint littlepoint = null;
            for (int i = 0; i < resultPoints.Length; i++)
            {
                if (resultPoints[i].X == resultPoints[i].Y)
                {
                    if (littlepoint != null && littlepoint.X > resultPoints[i].X) break;
                    else littlepoint = resultPoints[i];
                }
            }
            BlockPoint CenterPoint = new BlockPoint((int)littlepoint.X / unitLength, (int)littlepoint.X / unitLength);
            for (int x = CenterPoint.X - 1; x <= CenterPoint.X + 1; x++)
            {
                for (int y = CenterPoint.Y - 1; y <= CenterPoint.Y + 1; y++)
                {
                    LittleRightBottomBlock.Add(new BlockPoint(x, y));
                }
            }
            resultPointBlocks = new Block[] { LeftTopBlock, LeftBottomBlock, RightTopBlock, LittleRightBottomBlock };
        }

        public void CreateBoolMask(BitMatrix bitMatrix)
        {
            var bitMatrix2 = RemoveMargin(bitMatrix);
            var reader = new ZXing.BarcodeReader();
            var bitmap = ToBitmap(bitMatrix2, 50);
            var result = reader.Decode(bitmap);
            resultPoints = result.ResultPoints;
            ResultPoint point = null;
            for (int i = 0; i < resultPoints.Length - 1 && point == null; i++)
            {
                var list = new List<float>() { resultPoints[i].X, resultPoints[i].Y };
                if (list.Contains(resultPoints[i + 1].X) || list.Contains(resultPoints[i + 1].Y)) point = resultPoints[i];
            }

            int x = (int)point.X;
            int y = (int)point.Y;

            var TempLength = 0;
            if (bitMatrix2[x, y])
            {
                int tempx = x, tempy = y;
                while (bitMatrix2[tempx--, tempy--]) TempLength++;
                tempx = x + 1; tempy = y + 1;
                while (bitMatrix2[tempx++, tempy++]) TempLength++;
            }
            unitLength = TempLength / 3;
            length = Math.Max(bitMatrix2.Width / unitLength, bitMatrix2.Height / unitLength);
            boolMask = new bool[length, length];
            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < length; j++)
                {
                    BoolMask[i, j] = bitMatrix2[i * unitLength, j * unitLength];
                }
            }
        }
    }
}
