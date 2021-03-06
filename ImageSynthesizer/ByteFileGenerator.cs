﻿using Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using Infrastructure.Extensions;

namespace ImageSynthesizer
{
    public class ByteFileGenerator
    {
        /// <summary>
        /// normalize size of images, put all images and its identity into two byte files as for MNIST
        /// one file with image data
        /// second file with image label data
        /// 
        /// shuffle will make sure that the imagedata are organized in random order, so machine learning training don't need to shuffle it 
        /// </summary>
        public static void GenerateByteFile(string fontDataDirDest, int size, LabelConfig labelConfig)
        {
            var imageData = ConfigurationManager.AppSettings["ImageData"];
            var imageLabel = ConfigurationManager.AppSettings["ImageLabel"];
            File.Delete(imageData);
            File.Delete(imageLabel);


            Dictionary<string, List<ImageData>> alldata = new Dictionary<string, List<ImageData>>();



            foreach (var folder in Directory.GetDirectories(fontDataDirDest))
            {
                var folderName = folder.Split('\\');
                var ld = labelConfig.LabelDatas.Single(x => x.LabelAsFolderName.Equals(folderName[folderName.Length - 1]));
                var imageDatasPerChar = new List<ImageData>();

                foreach (var file in Directory.GetFiles(folder))
                {
                    imageDatasPerChar.Add(new ImageData { Path = file, Label = ld.Label, LabelAsInt = ld.LabelAsInt });
                }
                //shuffle each dataset for each char
                imageDatasPerChar.Shuffle();
                alldata[ld.Label] = imageDatasPerChar;
            }

            //split 50% of all char data as training data and 50% as test data, both test / train collection holds a equal distribution for all chars
            var testDatas = new List<ImageData>();
            var trainDatas = new List<ImageData>();

            foreach (KeyValuePair<string, List<ImageData>> entry in alldata)
            {
                var data = entry.Value.OrderBy(x => x.Label).ToList();
                var count = data.Count;
                var offset = count / 2;
                var trainD = data.Take(offset).ToList();
                trainDatas.AddRange(trainD);
                var testD = data.Skip(offset).Take(count - offset).ToList();
                testDatas.AddRange(testD);

            }
            //merge test and train dataset
            var imageDatas = new List<ImageData>();
            trainDatas.Shuffle();
            testDatas.Shuffle();

            imageDatas.AddRange(trainDatas);
            imageDatas.AddRange(testDatas);

            //write data to file
            var labelWriter = new BinaryWriter(new FileStream(imageLabel, FileMode.CreateNew));
            var imageDataWriter = new BinaryWriter(new FileStream(imageData, FileMode.CreateNew));
            var total = Directory.GetDirectories(fontDataDirDest).Sum(folder => Directory.GetFiles(folder).Count());
            WriteDataToFile(imageDatas, labelWriter, imageDataWriter, total, size);

            //data integrity check

            var labels = imageDatas.Select(x => x.LabelAsInt).ToList();
            VerifyLabelByteFile(imageLabel, labels);
            VerifyImageDataByteFile(imageData, imageDatas.Count(), size);

        }


        public static void WriteDataToFile(List<ImageData> imageDatas, BinaryWriter labelWriter, BinaryWriter imageDataWriter, int total, int size)
        {

            //label byte file 
            labelWriter.WriteInt32BigEndian(2049);
            labelWriter.WriteInt32BigEndian(total);
            foreach (var data in imageDatas)
            {
                labelWriter.Write((byte)data.LabelAsInt);
            }
            labelWriter.Flush();
            labelWriter.Close();

            //image byte file
            imageDataWriter.WriteInt32BigEndian(2051);
            imageDataWriter.WriteInt32BigEndian(total);
            imageDataWriter.WriteInt32BigEndian(size);
            imageDataWriter.WriteInt32BigEndian(size);
            foreach (var data in imageDatas)
            {
                var img = Image.FromFile(data.Path) as Bitmap;
                var bytes = img.GetPixelsForOneImage(size, size).ToArray();
                imageDataWriter.Write(bytes);
            }
            imageDataWriter.Flush();
            imageDataWriter.Close();
        }


        private static void VerifyLabelByteFile(string imageLabel, List<int> labels)
        {
            var info = new FileInfo(imageLabel);
            //4 bytes for magic number, 4 bytes as label count
            if (info.Length != labels.Count + 8)
                throw new ArgumentException("total byte count not match");

            var reader = new BinaryReader(new FileStream(imageLabel, FileMode.Open));

            var magic = reader.ReadInt32BigEndian();
            var lcount = reader.ReadInt32BigEndian();
            if (labels.Count != lcount)
                throw new Exception("total incorrect");
            int count = 0;
            while (true)
            {

                var label = (int)(reader.ReadByte());
                if (label != labels[count])
                    throw new ArgumentException("saved byte data not match the content");

                count++;
                if (count == labels.Count)
                    break;
            }
        }
        private static void VerifyImageDataByteFile(string imageData, int count, int size)
        {
            var info1 = new FileInfo(imageData);
            //16 bytes, 4 bytes for magic number, 4 bytes for image count, 4 bytes for X, 4 bytes for Y
            if (info1.Length != count * size * size + 16)
                throw new ArgumentException("total byte count not match");


        }
    }
}
