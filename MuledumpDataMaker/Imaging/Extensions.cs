using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Accord.Imaging.Filters;
using NImage = System.Drawing.Image;

namespace MuledumpDataMaker.Imaging
{
    /// <summary>
    ///
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="source"></param>
        /// <param name="name"></param>
        public static void SavePng(this NImage source, string name)
        {
            if (!name.EndsWith(".png"))
                name += ".png";
            var file = new FileInfo(name);
            if (!file.Directory.Exists)
                file.Directory.Create();
            using (var stream = file.OpenWrite())
                source.Save(stream, ImageFormat.Png);

            //source.Save(file.FullName, ImageFormat.Png);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="source"></param>
        /// <param name="bitmap"></param>
        public static void Add(this NImage source, NImage bitmap)
        {
            var graphics = Graphics.FromImage(source);
            graphics.CompositingMode = CompositingMode.SourceOver;

            const int margin = 4;
            var x = source.Width - bitmap.Width - margin;
            var y = source.Height - bitmap.Height - margin;
            graphics.DrawImage(bitmap, new Point(x, y));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="source"></param>
        /// <param name="bitmap"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public static void Add(this NImage source, NImage bitmap, int x, int y)
        {
            var graphics = Graphics.FromImage(source);
            graphics.CompositingMode = CompositingMode.SourceOver;
            graphics.DrawImage(bitmap, new Point(x, y));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="outlineColor"></param>
        /// <param name="glowColor"></param>
        /// <param name="outlineAlpha"></param>
        /// <param name="outlineSigma"></param>
        /// <param name="outlineSize"></param>
        /// <returns></returns>
        public static Bitmap OutlineGlow(this Bitmap bitmap, int outlineColor, int glowColor, double outlineAlpha = 1, double outlineSigma = 1, int outlineSize = 15)
        {
            var copy = (Bitmap)bitmap.Clone();

            if (outlineColor == -1)
            {
                if (glowColor == -1)
                    return bitmap;

                return applyOutlineGlow(copy,
                    Color.FromArgb((glowColor >> 16) & 0xFF, (glowColor >> 8) & 0xFF, (glowColor >> 0) & 0xFF), outlineAlpha, outlineSigma,
                    outlineSize);
            }
            Point[] neighbourPoints =
            {
                new Point(-1, -1),
                new Point(0, -1),
                new Point(1, -1),
                new Point(1, 0),
                new Point(1, 1),
                new Point(0, 1),
                new Point(-1, 1),
                new Point(-1, 0)
            };

            for (var x = 0; x < bitmap.Width; x++)
            {
                for (var y = 0; y < bitmap.Height; y++)
                {
                    var transparentFound = false;
                    var hasSomethingAround = false;

                    if (bitmap.GetPixel(x, y).A != 0)
                        continue;

                    foreach (var neighbourPoint in neighbourPoints)
                    {
                        var dx = x + neighbourPoint.X;
                        var dy = y + neighbourPoint.Y;

                        if (dx < 0 || dy < 0 || dx >= bitmap.Width || dy >= bitmap.Height)
                            continue;

                        var alpha = bitmap.GetPixel(dx, dy).A;

                        if (alpha == 0)
                            transparentFound = true;
                        if (alpha != 0)
                            hasSomethingAround = true;
                    }

                    var color = Color.FromArgb((outlineColor >> 16) & 0xFF, (outlineColor >> 8) & 0xFF, (outlineColor >> 0) & 0xFF);

                    if (transparentFound && hasSomethingAround)
                        copy.SetPixel(x, y, color);
                }
            }

            return applyOutlineGlow(copy, Color.FromArgb((glowColor >> 16) & 0xFF, (glowColor >> 8) & 0xFF, (glowColor >> 0) & 0xFF), outlineAlpha, outlineSigma, outlineSize);
        }

        private static Bitmap applyOutlineGlow(this Bitmap bitmap, Color shadowColor, double alpha = 1, double sigma = 1, int size = 15)
        {
            if (bitmap == null)
                return null;
            var copy = (Bitmap)bitmap.Clone();
            for (var x = 0; x < copy.Width; x++)
            {
                for (var y = 0; y < copy.Height; y++)
                {
                    var pixel = copy.GetPixel(x, y);
                    copy.SetPixel(x, y, Color.FromArgb(pixel.A, shadowColor));
                }
            }

            var shadow = copy;
            var blur = new GaussianBlur(sigma, size);
            blur.ApplyInPlace(shadow);

            for (var x = 0; x < bitmap.Width; x++)
            {
                for (var y = 0; y < bitmap.Height; y++)
                {
                    var originalPixel = bitmap.GetPixel(x, y);
                    if (originalPixel.A != 0)
                        shadow.SetPixel(x, y, originalPixel);
                    else
                    {
                        var shadowPixel = shadow.GetPixel(x, y);
                        if (shadowPixel.A != 0)
                            shadow.SetPixel(x, y, Color.FromArgb((int)(shadowPixel.A * alpha), shadowPixel));
                    }
                }
            }

            return shadow;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static bool IsEmpty(this Bitmap image)
        {
            var data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadOnly, image.PixelFormat);
            var bytes = new byte[data.Height * data.Stride];
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
            image.UnlockBits(data);
            return bytes.All(x => x == 0);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="action"></param>
        /// <param name="predicate"></param>
        public static void ForEachPixel(this Bitmap bitmap, Action<int, int> action, Func<Color, bool> predicate)
        {
            for (var y = 0; y < bitmap.Height; y++)
                for (var x = 0; x < bitmap.Height; x++)
                {
                    if (!predicate.Invoke(bitmap.GetPixel(x, y))) continue;
                    action.Invoke(x, y);
                }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="action"></param>
        public static void ForEach(this Bitmap bitmap, Action<int, int> action)
        {
            for (var y = 0; y < bitmap.Height; y++)
                for (var x = 0; x < bitmap.Height; x++)
                    action.Invoke(x, y);
        }

        private static void ForEach(this Bitmap bitmap, Action<int, int> action, Action<int, int> other)
        {
        }
    }
}