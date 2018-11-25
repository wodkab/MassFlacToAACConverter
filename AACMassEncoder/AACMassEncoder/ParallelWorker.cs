using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace AACMassEncoder
{
    class ParallelWorker : Worker
    {
        private readonly int MaxActions;
        private readonly int MaxThreads;

        public ParallelWorker(Stopwatch elapsedTime, int timeOutInMinutes, string stopperFilePath, int maxActionsPerSpawnBlock, int maxParallelProcesses) 
            : base(elapsedTime, timeOutInMinutes, stopperFilePath)
        {
            MaxThreads = maxParallelProcesses;
            Debug.Assert(maxActionsPerSpawnBlock < 64);
            MaxActions = Math.Min(maxActionsPerSpawnBlock, 64);
        }

        public override void ExecuteActions()
        {
            CheckElapsedTimeAndStop();

            var subActions = new List<Action>();

            foreach (var action in Actions)
            {
                if (subActions.Count < MaxActions)
                {
                    subActions.Add(action);
                }
                else
                {
                    SpawnAndWait(subActions);
                    subActions.Clear();
                    subActions.Add(action);
                }
            }

            if (subActions?.Count > 0)
            {
                SpawnAndWait(subActions);
            }
        }

        private void SpawnAndWait(IEnumerable<Action> actions)
        {
            CheckElapsedTimeAndStop();

            ThreadPool.SetMaxThreads(MaxThreads, MaxThreads);

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
    }
}
