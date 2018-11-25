using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace AACMassEncoder
{
    class SequentialWorker : Worker
    {
        public SequentialWorker(Stopwatch elapsedTime, int timeOutInMinutes, string stopperFile) 
            : base(elapsedTime, timeOutInMinutes, stopperFile)
        {
        }

        public override void ExecuteActions()
        {
            foreach (var action in Actions)
            {
                CheckElapsedTimeAndStop();
                action.Invoke();
            }
        }
    }
}
