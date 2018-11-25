using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace AACMassEncoder
{
    public abstract class Worker
    {
        protected IList<Action> Actions;
        protected string StopperFile;
        protected Stopwatch ElapsedTime;
        protected int TimeOutInMinutes;

        protected Worker(Stopwatch elapsedTime, int timeOutInMinutes, string stopperFile)
        {
            ElapsedTime = elapsedTime;
            TimeOutInMinutes = timeOutInMinutes;
            StopperFile = stopperFile;

            Actions = new List<Action>();
        }

        protected void CheckElapsedTimeAndStop()
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

        public abstract void AddActions(IList<Action> actions);
        public abstract void ExecuteActions();
    }
}
