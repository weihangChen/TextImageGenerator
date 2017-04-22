﻿using Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using Infrastructure.Extensions;
using System.Text;
using System.Threading.Tasks;

namespace ImageSynthesizer
{
    public class MNISTUnpack
    {
        static string _MNISTDir = ConfigurationManager.AppSettings["MNISTDest"];

        public static void UnpackByteFileToImages()
        {
            //create 10 directories under the specific root dir
            DirGenerator.CreateDirs(StringResources.Digits, _MNISTDir);
            //create stream and reader for label and image byte file reading
            var labelStream = new FileStream("../../Data/train-labels-idx1-ubyte", FileMode.Open, FileAccess.Read);
            var labelReader = new BinaryReader(labelStream);
            var imageStream = new FileStream("../../Data/train-labels-idx1-ubyte", FileMode.Open, FileAccess.Read);
            var imageReader = new BinaryReader(imageStream);
            //verify label file
            var labelFileLength = labelStream.Length;
            var lmagic = labelReader.ReadInt32BigEndian();
            var lnum = labelReader.ReadInt32BigEndian();
            if (labelFileLength != 60008 || lmagic != 2059 || lnum != 60000)
            {
                throw new ArgumentException("label byte file corrupted");
            }
            //verify image file
            int magic = imageReader.ReadInt32BigEndian();
            int numImages = imageReader.ReadInt32BigEndian();
            int numRows = imageReader.ReadInt32BigEndian();
            int numCols = imageReader.ReadInt32BigEndian();
            if (magic != 0 || numImages != 60000 || numRows != 28 || numCols != 28)
                throw new ArgumentException("image byte file corrupted");
            //read and save images to hard disk
            var imageDatas = new List<ImageData>();
            try
            {
                while (true)
                {
                    var imageData = imageReader.ReadAsImage(28, 28);
                    int label = labelReader.ReadByte();
                    imageData.Label = label.ToString();
                    imageDatas.Add(imageData);


                }
            }
            catch (EndOfStreamException)
            {

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            labelReader.Close();
            imageReader.Close();

            foreach (var imageData in imageDatas)
            {
                var labelDir = Path.Combine(_MNISTDir, imageData.Label);
                var imgPath = $"{labelDir}\\{Guid.NewGuid().ToString()}.jpg";
                imageData.bitmap.Save(imgPath);
            }

        }
    }
}
