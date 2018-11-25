using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace AACMassEncoder
{
    public abstract class Worker
    {
        protected string StopperFile;
        protected Stopwatch ElapsedTime;
        protected int TimeOutInMinutes;

        protected Worker(Stopwatch elapsedTime, int timeOutInMinutes, string stopperFile)
        {
            ElapsedTime = elapsedTime;
            TimeOutInMinutes = timeOutInMinutes;
            StopperFile = stopperFile;
        }

        public IList<Action> Actions { get; set; }

        protected void CheckElapsedTimeAndStop()
        {
            //check for file and stop
            if (File.Exists(StopperFile))
            {
                Console.WriteLine("Stopper file found: '" + StopperFile + "'");
                Console.WriteLine("Execution stopped!");
                File.Delete(StopperFile);
                Environment.Exit(0);
            }

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

        public abstract void ExecuteActions();
    }
}
