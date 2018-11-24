
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace AACMassEncoder
{
    class Program
    {
        private static string InputPath = @"c:\Input\";
        private static string OutpuPath = @"c:\Output\";
        private static string RelQaacPath = @"\qaac\qaac64.exe";
        private static string QaacFileWithPath = @"c:\temp\qaac64.exe";
        private static string StopperFile = "AACMassEncoder.stop";

        static void Main(string[] args)
        {
            //handle arguments
            if (args.Length != 2)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("AACMassEncoder.exe <input path> <output path>");
                Console.WriteLine("Create a file '" + OutpuPath + StopperFile + "' to stop execution");

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
            }

            //get absolute path to qaac
            QaacFileWithPath = GetExecutingDirectoryName() + RelQaacPath;

            if (!File.Exists(QaacFileWithPath))
            {
                Console.WriteLine("Decoder '" + QaacFileWithPath + "' doesn't exist");
                return;
            }

            Console.WriteLine("Start time: " + DateTime.Now);
            Console.WriteLine("Create a file '" + OutpuPath + StopperFile + "' to stop execution");

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

            Execute(actions);

            Console.WriteLine("Start time: " + DateTime.Now);
        }

        private static string GetExecutingDirectoryName()
        {
            var location = new Uri(Assembly.GetEntryAssembly().GetName().CodeBase);
            return new FileInfo(location.AbsolutePath).Directory.FullName + "\\";
        }

        #region DoTheWork

        private static void Execute(List<Action> actions)
        {
            const int maxActions = 32;
            var stopperFile = OutpuPath + StopperFile;

            //check for file and stop
            if (File.Exists(stopperFile))
            {
                Console.WriteLine("Stopper file found: '" + stopperFile + "'");
                Console.WriteLine("Execution stopped!");
                File.Delete(stopperFile);
                return;
            }

            var subActions = new List<Action>();

            foreach (var action in actions)
            {
                if (subActions.Count < maxActions)
                {
                    subActions.Add(action);
                }
                else
                {
                    SpawnAndWait(subActions);
                    subActions.Clear();

                    //check for file and stop
                    if (File.Exists(stopperFile))
                    {
                        Console.WriteLine("Stopper file found: '" + stopperFile + "'");
                        Console.WriteLine("Execution stopped!");
                        File.Delete(stopperFile);
                        break;
                    }

                    subActions.Add(action);
                }
            }

            if (subActions?.Count > 0)
            {
                SpawnAndWait(subActions);
            }
        }

        public static void SpawnAndWait(IEnumerable<Action> actions)
        {
            ThreadPool.SetMaxThreads(4, 4);

            var list = actions.ToList();
            var handles = new ManualResetEvent[list.Count];
            for (var i = 0; i < list.Count; i++)
            {
                handles[i] = new ManualResetEvent(false);
                var currentAction = list[i];
                var currentHandle = handles[i];
                Action wrappedAction = () => { try { currentAction(); } finally { currentHandle.Set(); } };
                ThreadPool.QueueUserWorkItem(x => wrappedAction());
            }

            WaitHandle.WaitAll(handles);
        }

        #endregion
    }
}
