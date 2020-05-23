using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using Accord.Imaging.Filters;
using AImage = Accord.Imaging.Image;

namespace MuledumpDataMaker.Imaging
{
    public class BitmapSheet
    {
        public Dictionary<ushort, Bitmap> Bitmaps { get; private set; }

        public static BitmapSheet FromImage(string path, int scale, int pieceScale, int pieceSize, bool saveSheet, bool savePieces = false, bool animated = false)
        {
            var bs = new BitmapSheet { Bitmaps = new Dictionary<ushort, Bitmap>() };

            var bitmap = AImage.FromFile(path);
            var totalBitmap = new Bitmap(bitmap.Width * scale, bitmap.Height * scale);

            var index = 0;
            for (var y = 0; y < bitmap.Height; y += pieceSize)
                for (var x = 0; x < bitmap.Width; x += pieceSize)
                {
                    var crop = new Crop(new Rectangle(x, y, pieceSize, pieceSize));
                    var piece = crop.Apply(bitmap);

                    var resizeW = System.Math.Min(32, pieceSize * 4);
                    var resizeH = System.Math.Min(32, pieceSize * 4);
                    var outW = System.Math.Min(40, pieceSize * 5);
                    var outH = System.Math.Min(40, pieceSize * 5);

                    var resize = new ResizeNearestNeighbor(resizeW, resizeH);
                    piece = resize.Apply(piece);

                    var outBitmap = new Bitmap(outW, outH);
                    outBitmap.Add(piece);

                    outBitmap = outBitmap.OutlineGlow(0, 0, .8, 1.4, 10);
                    if (savePieces)
                        outBitmap.Save($"{path} - 0x{index:x2}.png", ImageFormat.Png);

                    totalBitmap.Add(outBitmap, x * 5, y * 5);
                    bs.Bitmaps.Add((ushort)index, outBitmap);
                    index++;
                }
            if (saveSheet)
                totalBitmap.SavePng($"{path} - {(int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds}.png");

            return bs;
        }
    }
}