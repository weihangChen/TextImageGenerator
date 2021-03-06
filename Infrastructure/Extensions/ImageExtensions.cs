﻿using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace Infrastructure.Extensions
{

    public static class ImageExtensions
    {
        public static void FromBytesToFile(this byte[] bytes, string path)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                Image i = Image.FromStream(ms);
                i.Save(path);
                //always recycle bitmap manually
                i.Dispose();
            }
        }

        public static Image FromFileToImage(this string path)
        {
            return FromFileToBytes(path).FromBytesToImage();
        }

        public static byte[] FromFileToBytes(this string path)
        {
            return File.ReadAllBytes(path);
        }



        public static byte[] ImageToByte(this Image img)
        {
            ImageConverter converter = new ImageConverter();
            return (byte[])converter.ConvertTo(img, typeof(byte[]));
        }

        public static Image ByteToImage(this byte[] bytes)
        {
            return Image.FromStream(new MemoryStream(bytes));
        }


        public static Bitmap Rezie(this Bitmap image, int width, int height)
        {
            Image<Bgr, byte> img = new Image<Bgr, byte>(image);
            Image<Bgr, byte> cpimg = img.Resize(width, height, Inter.Linear);
            return cpimg.Bitmap;
        }

        /// <summary>
        /// kowning only size of one dimension, scale the other dimension by comparing with origianl image
        /// </summary>
        /// <param name="image">to be resized image</param>
        /// <param name="size">known new size</param>
        /// <param name="XOrY">known size is at X-axis or Y-axis</param>
        /// <returns></returns>
        public static Bitmap RezieWithOneKnowAxis(this Bitmap image, int size, Axis axis)
        {
            int width = 0;
            int height = 0;
            if (axis == Axis.X)
            {
                width = size;
                height = Convert.ToInt32(image.Height * (width / image.Width));
            }
            else if (axis == Axis.Y)
            {
                height = size;
                width = Convert.ToInt32(image.Width * (height / image.Height));
            }

            return Rezie(image, width, height);
        }



        public static Image FromBytesToImage(this byte[] bytes)
        {
            Image i;
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                i = Image.FromStream(ms);
            }
            return i;
        }

        public static Image CropImage(this Image img, Rectangle cropArea)
        {
            Bitmap bmpImage = new Bitmap(img);
            return bmpImage.Clone(cropArea, bmpImage.PixelFormat);
        }


        public static void DrawContoursOnImage(this Image img, List<Rectangle> recs)
        {
            Font font = new Font("Times New Roman", 13.0f);
            Brush bgBrush = new SolidBrush(Color.Goldenrod);

            using (Graphics g = Graphics.FromImage(img))
            {
                using (Pen pen = new Pen(ColorTranslator.FromHtml("#ff6bb5"), 1))
                {

                    recs.ForEach(rec =>
                    {
                        Point p = new Point((rec.Left + rec.Right) / 2, rec.Top);
                        g.DrawRectangle(pen, rec);
                        //g.DrawString(imageData.Text, font, bgBrush, new PointF(p.X + 1 - font.Height / 3, p.Y + 1 - font.Height));
                        //g.DrawString(imageData.label.ToString(), font, Brushes.Black, 5, 5);
                    });
                }
            }
        }

        public static double[] ConvertImageToOneDimensionArray(Bitmap bmp)
        {
            int width = bmp.Width;
            int height = bmp.Height;
            double[] features = new double[width * height];
            for (int i = 0; i < height; i++)
                for (int j = 0; j < width; j++)
                {
                    var pixel = bmp.GetPixel(j, i);
                    features[i * width + j] = (pixel.R == 255) ? 0 : 1;
                }
            return features;
        }

        //0 stands for white - R 255
        //1 stands for black - R 0
        public static int[][] ConvertImageToTwoDimensionArray(this Bitmap bmp)
        {
            int width = bmp.Width;
            int height = bmp.Height;
            int[][] features = new int[width][];
            for (int i = 0; i < width; i++)
            {
                features[i] = new int[height];
                for (int j = 0; j < height; j++)
                {
                    Color pixel = bmp.GetPixel(i, j);
                    features[i][j] = pixel.ColorToByte();
                }
            }
            return features;
        }

        public static int[][] ConvertImageToTwoDimensionArray1(this Bitmap bmp)
        {
            int width = bmp.Width;
            int height = bmp.Height;
            int[][] features = new int[height][];
            for (int i = 0; i < height; i++)
            {
                features[i] = new int[width];
                for (int j = 0; j < width; j++)
                {
                    Color pixel = bmp.GetPixel(j, i);
                    features[i][j] = pixel.ColorToByte();
                }
            }
            return features;
        }

        public static Bitmap ConvertTwoDimensionArrayToImage(this int[][] features)
        {

            int width = features.Length;
            int height = features[0].Length;
            var bmp = new Bitmap(width, height);
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    var feature = features[i][j];
                    Color color = Color.FromArgb(feature, feature, feature);
                    bmp.SetPixel(i, j, color);
                    //if (feature == 1)
                    //{
                    //    bmp.SetPixel(i, j, Color.Black);
                    //}
                    //else
                    //{
                    //    bmp.SetPixel(i, j, Color.White);
                    //}
                }
            }
            return bmp;
        }

        /// <summary>
        /// peel white pixels as offset from the char image, usually offset at Y-axis are larger
        /// therefore peel more at Y. by removing these offset we are going to test if it inproves the classification rates in ML pipeline
        /// 
        /// note1: better way of doing it can be looking for the perfect offset by measuring all images first 
        /// note2: som offset-top and offset-bottom are uneven, it is possible to auto adjust that, but really don't know if it is going to help
        /// 
        /// 
        /// </summary>
        /// <param name="features"></param>
        /// <param name="offsetX"></param>
        /// <param name="offsetY"></param>
        /// <returns></returns>
        public static int[][] PeelOffset(this int[][] features, int offsetXLeft, int offsetXRight, int offsetYTop, int offsetYBottom)
        {
            int width = features.Length;
            int width_new = width - (offsetXLeft + offsetXRight);
            int height = features[0].Length;
            int height_new = height - (offsetYTop + offsetYBottom);

            int[][] features_new = new int[width_new][];
            for (int i = 0; i < width; i++)
            {
                //x-axis peel
                var isPeelArea_Width = false;
                if (i < offsetXLeft || i + offsetXRight >= width)
                    isPeelArea_Width = true;


                if (!isPeelArea_Width)
                    features_new[i - offsetXLeft] = new int[height_new];

                for (int j = 0; j < height; j++)
                {
                    var isPeelArea_Height = false;
                    //there is no need to calculate since one axix as peel area is enough
                    if (!isPeelArea_Width)
                    {
                        //y-axix peel
                        if (j < offsetYTop || j + offsetYBottom >= height)
                            isPeelArea_Height = true;
                    }
                    var feature = features[i][j];
                    var isPeelArea = (isPeelArea_Width || isPeelArea_Height);
                    //if the peel area contains dark pixel, should not peel
                    if (isPeelArea && feature == 1)
                        throw new Exception("peeled area contains dark pixel, modify offset");
                    //assign data to the new features if it is not peel area
                    if (!isPeelArea)
                    {
                        features_new[i - offsetXLeft][j - offsetYTop] = feature;
                    }
                }
            }
            return features_new;
        }

        public static List<int> CalculateOffsets(this Bitmap img)
        {
            var result = new List<int>();
            var width = img.Width;
            var height = img.Height;

            dynamic offsetIndexes = img.CalculateOffsetIndexes();
            result.Add(offsetIndexes.xFirstDarkPixelIndex);
            result.Add(width - offsetIndexes.xLastDarkPixelIndex - 1);
            result.Add(offsetIndexes.yFirstDarkPixelIndex);
            result.Add(height - offsetIndexes.yLastDarkPixelIndex - 1);
            return result;
        }

        public static List<int> CalculateOffsetDiffs(this Bitmap img)
        {
            var result = new List<int>();
            var width = img.Width;
            var height = img.Height;

            dynamic offsetIndexes = img.CalculateOffsetIndexes();
            //X
            var diffX = offsetIndexes.xFirstDarkPixelIndex - (width - offsetIndexes.xLastDarkPixelIndex - 1);
            if (diffX == 0)
            {
                result.Add(0);
                result.Add(0);
            }
            else if (diffX > 0)
            {
                result.Add(Math.Abs(diffX));
                result.Add(0);
            }
            else if (diffX < 0)
            {
                result.Add(0);
                result.Add(Math.Abs(diffX));

            }
            //Y
            var diffY = offsetIndexes.yFirstDarkPixelIndex - (height - offsetIndexes.yLastDarkPixelIndex - 1);
            if (diffY == 0)
            {
                result.Add(0);
                result.Add(0);
            }
            else if (diffY > 0)
            {
                result.Add(Math.Abs(diffY));
                result.Add(0);
            }
            else if (diffY < 0)
            {
                result.Add(0);
                result.Add(Math.Abs(diffY));

            }
            return result;
        }


        private static dynamic CalculateOffsetIndexes(this Bitmap img)
        {
            //variables
            var xFirstDarkPixelIndex = 0;
            var xLastDarkPixelIndex = 0;
            var yFirstDarkPixelIndex = 0;
            var yLastDarkPixelIndex = 0;


            //process matrix by column
            var featuresByColumn = img.ConvertImageToTwoDimensionArray();

            for (int i = 0; i < featuresByColumn.Length; i++)
            {
                var columnData = featuresByColumn[i].ToList().Select((pixel, index) => new { pixel, index });

                var columnFirstDarkPixel = columnData.FirstOrDefault(x => x.pixel != (int)MyColor.WHITE);
                if (columnFirstDarkPixel != null)
                {
                    var columnFirstDarkPixelIndex = columnFirstDarkPixel.index;
                    if (yFirstDarkPixelIndex == 0 || columnFirstDarkPixelIndex < yFirstDarkPixelIndex)
                        yFirstDarkPixelIndex = columnFirstDarkPixelIndex;
                }


                var columnLastDarkPixel = columnData.LastOrDefault(x => x.pixel != (int)MyColor.WHITE);
                if (columnLastDarkPixel != null)
                {
                    var columnLastDarkPixelIndex = columnLastDarkPixel.index;
                    if (yLastDarkPixelIndex == 0 || columnLastDarkPixelIndex > yLastDarkPixelIndex)
                        yLastDarkPixelIndex = columnLastDarkPixelIndex;
                }

            }

            //process matrix by row
            var featuresByRow = img.ConvertImageToTwoDimensionArray1();
            for (int i = 0; i < featuresByRow.Length; i++)
            {
                var rowData = featuresByRow[i].ToList().Select((pixel, index) => new { pixel, index });

                var rowFirstDarkPixel = rowData.FirstOrDefault(x => x.pixel != (int)MyColor.WHITE);
                if (rowFirstDarkPixel != null)
                {
                    var rowFirstDarkPixelIndex = rowFirstDarkPixel.index;
                    if (xFirstDarkPixelIndex == 0 || rowFirstDarkPixelIndex < xFirstDarkPixelIndex)
                        xFirstDarkPixelIndex = rowFirstDarkPixelIndex;
                }

                var rowLastDarkPixel = rowData.LastOrDefault(x => x.pixel != (int)MyColor.WHITE);
                if (rowLastDarkPixel != null)
                {
                    var rowLastDarkPixelIndex = rowLastDarkPixel.index;
                    if (xLastDarkPixelIndex == 0 || rowLastDarkPixelIndex > xLastDarkPixelIndex)
                        xLastDarkPixelIndex = rowLastDarkPixelIndex;
                }

            }

            return new
            {
                xFirstDarkPixelIndex = xFirstDarkPixelIndex,
                xLastDarkPixelIndex = xLastDarkPixelIndex,
                yFirstDarkPixelIndex = yFirstDarkPixelIndex,
                yLastDarkPixelIndex = yLastDarkPixelIndex

            };


        }

        public static Bitmap CropFromBitmap(this Bitmap source, Rectangle rec, GraphicsUnit graphicsUnit = GraphicsUnit.Pixel)
        {
            if (rec.Width == 0 || rec.Height == 0)
                throw new ArgumentException("the to be crop rec width or height is 0");
            Bitmap bmp = null;
            try
            {
                bmp = new Bitmap(rec.Width, rec.Height);
                var graphics = Graphics.FromImage(bmp);
                //graphics.SetupGraphic();
                graphics.DrawImage(source, 0, 0, rec, graphicsUnit);
            }
            catch (Exception e)
            {
                throw e;
            }


            return bmp;
        }

        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(this Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.SetupGraphic();
                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        public static void SetupGraphic(this Graphics graphics)
        {
            //sourcecopy is kind of important to get higher predication rate
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        }
        public static Bitmap CropImageToEdge(this Bitmap bmp, string text = "")
        {

            var imageRawData = new Dictionary<int, List<double>>();
            Rectangle edgeRetangle = new Rectangle();

            try
            {
                int width = bmp.Width;
                int height = bmp.Height;

                //read in bitmpa data row by row into imageRawData
                var leftMostIndex = 0;
                var rightMostIndex = 0;

                for (int i = 0; i < height; i++)
                {
                    var rowRawData = new List<double>();
                    for (int j = 0; j < width; j++)
                    {
                        var pixel = bmp.GetPixel(j, i);
                        var tmp = (pixel.R == 255) ? 0 : 1;

                        if (tmp == 1)
                        {
                            if (leftMostIndex > j || leftMostIndex == 0)
                                leftMostIndex = j;

                            if (rightMostIndex < j || rightMostIndex == 0)
                                rightMostIndex = j;
                        }
                        rowRawData.Add(tmp);
                    }
                    imageRawData.Add(i, rowRawData);
                }

                //find the rectangle
                var darkRows = imageRawData.Where(x => x.Value.Contains(1));

                if (darkRows.Count() == 0)
                {
                    throw new Exception("stop the porgram since no dark color row is found");
                }


                var firstDarkRowIndex = darkRows.First().Key;
                var lastDarkRowIndex = darkRows.Last().Key;

                //rightMostIndex - leftMostIndex
                var letterWidth = rightMostIndex - leftMostIndex + 1;
                var letterHeight = lastDarkRowIndex - firstDarkRowIndex + 1;
                //the right index and left index is same, set the rectangle width to 1
                letterWidth = letterWidth == 0 ? 1 : letterWidth;
                letterHeight = letterHeight == 0 ? 1 : letterHeight;
                //modify the leftMostIndex and firstDarkRowIndex
                leftMostIndex = leftMostIndex > 1 ? leftMostIndex : leftMostIndex - 1;
                firstDarkRowIndex = firstDarkRowIndex > 1 ? firstDarkRowIndex : firstDarkRowIndex - 1;


                edgeRetangle = new Rectangle(leftMostIndex, firstDarkRowIndex, letterWidth, letterHeight);
            }
            catch (Exception e)
            {
                throw e;
            }
            var newBmp = bmp.CropFromBitmap(edgeRetangle);
            return newBmp;

        }


        /// <summary>
        /// convert pixel data + height + width into bitmap
        /// </summary>
        /// <param name="widthOrigin"></param>
        /// <param name="heightOrigin"></param>
        /// <param name="pixels"></param>
        /// <param name="mag">magnify</param>
        /// <returns></returns>
        public static Bitmap GetBitmap(this byte[][] pixels, int widthOrigin, int heightOrigin, int mag)
        {
            // create a C# Bitmap suitable for display in a PictureBox control
            int width = widthOrigin * mag;
            int height = heightOrigin * mag;
            Bitmap result = new Bitmap(width, height);
            Graphics gr = Graphics.FromImage(result);
            for (int i = 0; i < heightOrigin; ++i)
            {
                for (int j = 0; j < widthOrigin; ++j)
                {
                    //int pixelColor = 255 - pixels[i][j]; // white background, black digits
                    //int pixelColor = dImage.pixels[i][j]; // black background, white digits
                    //Color c = Color.FromArgb(pixelColor, pixelColor, pixelColor); // gray scale
                    //Color c = Color.FromArgb(pixelColor, 0, 0); // red scale
                    SolidBrush sb = new SolidBrush(pixels[i][j].ByteToColor());
                    gr.FillRectangle(sb, j * mag, i * mag, mag, mag); // fills bitmap via Graphics object
                }
            }
            return result;
        }

        public static Color ByteToColor(this byte b)
        {
            // white background, black digits
            int pixelColor = 255 - b;
            return Color.FromArgb(pixelColor, pixelColor, pixelColor);
        }

        public static byte ColorToByte(this Color color)
        {
            var pixelValue = Convert.ToInt32((color.R + color.G + color.B) / 3);
            return (byte)pixelValue;
        }


        public static List<byte> GetPixelsForOneImage(this Bitmap img, int xmax, int ymax)
        {
            var pixels = new List<byte>();
            //var imgResized = (Bitmap)img.ResizeImage(size, size);

            //vertical pixel iteration first
            //for (int x = 0; x < size; x++)
            //{
            //    for (int y = 0; y < size; y++)
            //    {
            //        Color pixel = imgResized.GetPixel(x, y);
            //        var pixelValue = Convert.ToInt32((pixel.R + pixel.G + pixel.B) / 3);
            //        pixels.Add(pixelValue);
            //    }
            //}

            //horizontal pixel iteration first
            for (int y = 0; y < ymax; y++)
            {
                for (int x = 0; x < xmax; x++)
                {
                    Color pixel = img.GetPixel(x, y);
                    //var pixelValue = Convert.ToInt32((pixel.R + pixel.G + pixel.B) / 3);
                    pixels.Add(pixel.ColorToByte());
                }
            }

            return pixels;
        }
    }
}
