
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

        const int MaxActions = 32;

        private static int TimeOutInMinutes = -1;
        private static Stopwatch ElapsedTime = new Stopwatch();

        static void Main(string[] args)
        {
            //handle arguments
            if (args.Length < 2)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("AACMassEncoder.exe <input path> <output path>");
                Console.WriteLine("-t<time out in minutes");

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
                Console.WriteLine("Create a file '" + StopperFilePath + "' to stop execution");
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

            Console.WriteLine("Start time: " + DateTime.Now);
            ElapsedTime.Start();
            Console.WriteLine("Create a file '" + StopperFilePath + "' to stop execution");

            //get all files
            var files = Directory.GetFiles(InputPath, "*", SearchOption.AllDirectories);
            IList<FileItem> allFiles = files.Select(file => 
                new FileItem(new FileInfo(file), InputPath, OutpuPath, QaacFileWithPath)).ToList();

            //first copy jpegs for artwork images
            foreach (var workFile in allFiles)
            {
                if (workFile.Type == FileType.Jpg)
                {
                    workFile.HandleFile();
                    CheckElapsedTimeAndStop();
                }
            }

            //do the rest
            var actions = new List<Action>();
            foreach (var workFile in allFiles)
            {
                if (workFile.Type != FileType.Jpg)
                {
                    actions.Add(workFile.HandleFile);
                }
            }

            var parallelWorker = new ParallelWorker(ElapsedTime, TimeOutInMinutes, StopperFilePath, MaxActions, 4);
            parallelWorker.AddActions(actions);
            parallelWorker.ExecuteActions();

            Console.WriteLine("End time: " + DateTime.Now);
        }

        private static string GetExecutingDirectoryName()
        {
            var location = new Uri(Assembly.GetEntryAssembly().GetName().CodeBase);
            return new FileInfo(location.AbsolutePath).Directory.FullName + "\\";
        }

        private static void CheckElapsedTimeAndStop()
        {
            var elapsedMinutes = ElapsedTime.Elapsed.Minutes;

            if (TimeOutInMinutes > 0 && elapsedMinutes > TimeOutInMinutes)
            {
                Console.WriteLine("Elapsed time " + elapsedMinutes + " min ... time exceeded ... Exit!");
                Environment.Exit(0);
            }
            else
            {
                Console.WriteLine("Elapsed time " + elapsedMinutes + "/" + TimeOutInMinutes + " min.");
            }
        }
    }
}
