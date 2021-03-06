﻿
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AACMassEncoder
{
    class Program
    {
        private static string InputPath = @"c:\Input\";
        private static string OutpuPath = @"c:\Output\";
        private static string RelativeQaacPath = @"\qaac\qaac64.exe";
        private static string QaacFileWithPath = @"c:\temp\qaac64.exe";
        private static string StopperFilePath = "AACMassEncoder.stop";

        private static IEnumerable<string> IgnorePatterns = new List<string>();
#if DEBUG
        const int MaxActions = 2;
#else
        const int MaxActions = 64;
#endif

        private static int TimeOutInMinutes = -1;
        private static Stopwatch ElapsedTime = new Stopwatch();

        static void Main(string[] args)
        {
            //handle arguments
            if (args.Length < 2)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("AACMassEncoder.exe <input path> <output path>");
                Console.WriteLine("-t<time out in minutes>");
                Console.WriteLine("-t<ignor1/ignor2_no_blank_allowed/ignore_3>");

                return;
            }

            InputPath = args[0];
            OutpuPath = args[1];

            if (!InputPath.EndsWith("\\"))
            {
                InputPath += "\\";
                if (!Directory.Exists(InputPath))
                {
                    Console.WriteLine("Input directory '" + InputPath + "' doesn't exist.");
                    return;
                }
            }

            if (!OutpuPath.EndsWith("\\"))
            {
                OutpuPath += "\\";
                if (!Directory.Exists(OutpuPath))
                {
                    Console.WriteLine("Output directory '" + OutpuPath + "' doesn't exist.");
                    return;
                }

                StopperFilePath = OutpuPath + StopperFilePath;
            }

            //get absolute path to qaac
            QaacFileWithPath = GetExecutingDirectoryName() + RelativeQaacPath;

            if (!File.Exists(QaacFileWithPath))
            {
                Console.WriteLine("Decoder '" + QaacFileWithPath + "' doesn't exist");
                return;
            }

            foreach (var argument in args)
            {
                if (argument.StartsWith("-t") && argument.Length > 2)
                {
                    var result = Convert.ToInt32(argument.Replace("-t", String.Empty));
                    if (result > 0)
                    {
                        TimeOutInMinutes = result;
                        Console.WriteLine("Time out set to " + TimeOutInMinutes + " minutes.");
                    }
                }
            }

            foreach (var argument in args)
            {
                if (argument.StartsWith("-i") && argument.Length > 2)
                {
                    var result = argument.Replace("-i", String.Empty);
                    if (result.Length > 0)
                    {
                        IgnorePatterns = result.Split(new char[] {'/'}, StringSplitOptions.RemoveEmptyEntries).ToList();
                        foreach (var ignorePattern in IgnorePatterns)
                        {
                            Console.WriteLine("Ignore pattern: " + ignorePattern);
                        }
                    }
                }
            }

            Console.WriteLine("Start time: " + DateTime.Now);
            ElapsedTime.Start();
            Console.WriteLine("Create a file '" + StopperFilePath + "' to stop execution");

            //get all files and filter out the ignored ones
            var files = Directory.EnumerateFiles(InputPath, "*", SearchOption.AllDirectories).Where(file => !FileNameContainsIgnorePattern(file)).ToList();
            
            IList<FileItem> allFiles = files.Select(file => 
                new FileItem(new FileInfo(file), InputPath, OutpuPath, QaacFileWithPath)).ToList();

            Console.WriteLine(allFiles.Count + " files have to be processed!");
            foreach (var fileItem in allFiles)
            {
                Console.WriteLine(fileItem.SourceFile.FullName);
            }

            //first copy jpegs for artwork images
            var jpegWorker = new SequentialWorker(ElapsedTime, TimeOutInMinutes, StopperFilePath)
            {
                Actions = (from workFile in allFiles where workFile.Type == FileType.Jpg select (Action)workFile.HandleFile).ToList()
            };
            jpegWorker.ExecuteActions();

            //do the flac to m4a
            var flacWorker = new ParallelWorker(ElapsedTime, TimeOutInMinutes, StopperFilePath, MaxActions, 4)
            {
                Actions = (from workFile in allFiles where workFile.Type == FileType.Flac select (Action)workFile.HandleFile).ToList()
            };
            flacWorker.ExecuteActions();

            //do the mp3/m4a copy and artwork
            var mp3Worker = new SequentialWorker(ElapsedTime, TimeOutInMinutes, StopperFilePath)
            {
                Actions = (from workFile in allFiles where workFile.Type == FileType.Mp3 || workFile.Type == FileType.M4a select (Action)workFile.HandleFile).ToList()

            };
            mp3Worker.ExecuteActions();

            //do the rest
            var restWorker = new SequentialWorker(ElapsedTime, TimeOutInMinutes, StopperFilePath)
            {
                Actions = (from workFile in allFiles where workFile.Type == FileType.Other select (Action)workFile.HandleFile).ToList()

            };
            restWorker.ExecuteActions();

            Console.WriteLine("End time: " + DateTime.Now);
        }

        private static bool FileNameContainsIgnorePattern(string file)
        {
            return IgnorePatterns.Any(ignorePattern => file.Contains(ignorePattern));
        }

        private static string GetExecutingDirectoryName()
        {
            var location = new Uri(Assembly.GetEntryAssembly().GetName().CodeBase);
            return new FileInfo(location.AbsolutePath).Directory.FullName + "\\";
        }
    }
}
