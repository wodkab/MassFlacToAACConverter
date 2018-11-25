using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace AACMassEncoder
{
    public abstract class Worker
    {
        protected string StopperFile;
        protected Stopwatch ElapsedTime;
        protected int TimeOutInMinutes;
        private IList<double> TimePerItems;

        protected Worker(Stopwatch elapsedTime, int timeOutInMinutes, string stopperFile)
        {
            ElapsedTime = elapsedTime;
            TimeOutInMinutes = timeOutInMinutes;
            StopperFile = stopperFile;
            TimePerItems = new List<double>();
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

        protected void CalculateRestTime(int workedItemCount, TimeSpan neededTimeSpan, Action lastExecutedAction)
        {
            if(neededTimeSpan.Seconds >0 )
            {
                double timePerItem = (double)neededTimeSpan.Seconds / workedItemCount;
                TimePerItems.Add(timePerItem);

                if (timePerItem > 0.0)
                {
                    double restCount = Actions.Count - Actions.IndexOf(lastExecutedAction);
                    Console.WriteLine("Remaining items: " + restCount);
                    Console.WriteLine("Time needed per item: " + TimePerItems.Average() + " sec.");
                    Console.WriteLine("Remaining  time: " + TimeSpan.FromSeconds(TimePerItems.Average() * restCount).ToString(@"dd\.hh\:mm\:ss") + " days.");
                }
            }
        }

        public abstract void ExecuteActions();
    }
}
