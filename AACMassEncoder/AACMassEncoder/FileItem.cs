using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using TagLib;

namespace AACMassEncoder
{
    public enum FileType
    {
        Flac,
        Mp3,
        M4a,
        Jpg,
        Other
    }

    public class FileItem
    {
        private const string CoverFileName = "cover.jpg";
        private readonly string InputPath;
        private readonly string OutputPath;
        private readonly string EncoderPath;
        private const string AacExt = ".m4a";

        public FileItem(FileInfo file, string inputPath, string outputPath, string encoderPath)
        {
            SourceFile = file;
            OutputPath = outputPath;
            InputPath = inputPath;
            EncoderPath = encoderPath;
        }

        public FileInfo SourceFile { get; private set; }
        
        public FileType Type
        {
            get
            {
                if (SourceFile.Extension == ".flc" || SourceFile.Extension == ".flac")
                {
                    return FileType.Flac;
                }

                if (SourceFile.Extension == ".mp3")
                {
                    return FileType.Mp3;
                }

                if (SourceFile.Extension == ".jpg" || SourceFile.Extension == ".jpeg")
                {
                    return FileType.Jpg;
                }

                if (SourceFile.Extension == ".m4a")
                {
                    return FileType.M4a;
                }

                return FileType.Other;
            }
        }

        public string AacOutputFilenameWithPath
        {
            get
            {
                var outFile = SourceFile.FullName.Replace(InputPath, OutputPath);
                return outFile.Replace(SourceFile.Extension, AacExt);
            }
        }

        public string OutputDirectory
        {
            get
            {
                var outDirectory = SourceFile.DirectoryName?.Replace(InputPath, OutputPath);
                if (outDirectory != null && !outDirectory.EndsWith("\\"))
                {
                    outDirectory += "\\";
                }

                return outDirectory;
            }
        }

        public string OutputFileNameWithPath => SourceFile.FullName.Replace(InputPath, OutputPath);

        public void HandleFile()
        {
            if (!System.IO.File.Exists(SourceFile.FullName))
            {
                Console.WriteLine("Source file '" + SourceFile.FullName + "' is missing!");
                return;
            }

            switch (Type)
            {
                case FileType.Flac:
                    Encode();
                    break;
                case FileType.Jpg:
                    {
                        ResizeAndCopyJpeg(500);
                    }
                    break;
                case FileType.Mp3:
                case FileType.M4a:
                    CopyLossyFileAndAddTag();
                    break;
                case FileType.Other:
                    CopyFileToOutput();
                    break;
            }
        }

        private void CopyLossyFileAndAddTag()
        {
            bool fileAlreadyExists = System.IO.File.Exists(OutputFileNameWithPath);

            if (!fileAlreadyExists)
            {
                CopyFileToOutput();
            }

            var fileAndPathToCoverJpg = OutputDirectory + CoverFileName;

            //add artwork
            if (!fileAlreadyExists && System.IO.File.Exists(fileAndPathToCoverJpg))
            {
                Console.WriteLine("Add album art '" + fileAndPathToCoverJpg + "' to file '" + OutputFileNameWithPath + "'");
                var file = TagLib.File.Create(OutputFileNameWithPath); 
                IPicture albumArt = new Picture(fileAndPathToCoverJpg);
                file.Tag.Pictures = new[] { albumArt };
                file.Save();

                CopyFileAttributesTo(new FileInfo(OutputFileNameWithPath));
            }
        }

        private void CopyFileToOutput()
        {
            Console.WriteLine("Copy file '" + SourceFile.FullName + "'");

            try
            {
                if (!System.IO.File.Exists(OutputFileNameWithPath))
                {
                    var directoryInfo = (new FileInfo(OutputFileNameWithPath.Replace(SourceFile.Name, ""))).Directory;
                    directoryInfo?.Create();
                    var file = SourceFile.CopyTo(OutputFileNameWithPath);
                    CopyFileAttributesTo(file);
                }
                else
                {
                    CopyFileAttributesTo(new FileInfo(OutputFileNameWithPath));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void Encode()
        {

            if (System.IO.File.Exists(AacOutputFilenameWithPath))
            {
                CopyFileAttributesTo(new FileInfo(AacOutputFilenameWithPath));
                return;
            }

            try
            {
                if (System.IO.File.Exists(OutputDirectory + CoverFileName))
                {

                }

                // Use ProcessStartInfo class
                var startInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    FileName = EncoderPath,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    Arguments = "\"" + SourceFile.FullName + "\" -o \"" + AacOutputFilenameWithPath + "\""
                };

                var jpegCoverFilePath = OutputDirectory + CoverFileName;
                if (System.IO.File.Exists(jpegCoverFilePath))
                {
                    startInfo.Arguments = "\"" + SourceFile.FullName + "\" -o \"" + AacOutputFilenameWithPath + "\""
                        + " --artwork \"" + jpegCoverFilePath + "\"";
                }

                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                Console.WriteLine(startInfo.FileName + " " + startInfo.Arguments);
                using (var exeProcess = Process.Start(startInfo))
                {
                    exeProcess?.WaitForExit();

                    if (exeProcess != null && exeProcess.ExitCode != 0)
                    {
                        Console.WriteLine("*** Error ***");
                        Console.WriteLine(SourceFile.FullName);
                    }
                    else
                    {
                        CopyFileAttributesTo(new FileInfo(AacOutputFilenameWithPath));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private void CopyFileAttributesTo(FileInfo file)
        {
            try
            {
                file.CreationTime = SourceFile.CreationTime;
                file.LastAccessTime = SourceFile.LastAccessTime;
                file.LastWriteTime = SourceFile.LastWriteTime;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void ResizeAndCopyJpeg(int size)
        {
            var sourceFile = SourceFile.FullName;
            var destFile = OutputDirectory + SourceFile.Name;
            ;
            if (!System.IO.File.Exists(destFile))
            {
                Console.WriteLine("Copy and Resize image '" + SourceFile.FullName +"'");
                using (var image = new Bitmap(Image.FromFile(sourceFile)))
                {
                    int quality = 80;
                    int width, height;
                    if (image.Width > image.Height)
                    {
                        width = size;
                        height = Convert.ToInt32(image.Height * size / (double) image.Width);
                    }
                    else
                    {
                        width = Convert.ToInt32(image.Width * size / (double) image.Height);
                        height = size;
                    }

                    var resized = new Bitmap(width, height);
                    using (var graphics = Graphics.FromImage(resized))
                    {
                        graphics.CompositingQuality = CompositingQuality.HighSpeed;
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.CompositingMode = CompositingMode.SourceCopy;
                        graphics.DrawImage(image, 0, 0, width, height);

                        var directoryInfo = (new FileInfo(OutputDirectory)).Directory;
                        directoryInfo?.Create();
                        using (var output = System.IO.File.Open(destFile, FileMode.Create))
                        {
                            var encoderParameters = new EncoderParameters(1)
                            {
                                Param = {[0] = new EncoderParameter(Encoder.Quality, quality)}
                            };
                            var codec = ImageCodecInfo.GetImageDecoders()
                                .FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);
                            if (codec != null) resized.Save(output, codec, encoderParameters);
                        }
                    }
                }
            }

            if (System.IO.File.Exists(destFile))
            {
                CopyFileAttributesTo(new FileInfo(destFile));
            }
        }
    }
}
