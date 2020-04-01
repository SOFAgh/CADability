using System;
using System.Threading;

namespace CADability
{
    public class ParallelHelper
    {
#if DEBUG
        private static string lmfa = ""; // to have an object to lock access to maxFailedAttempts
        static int maxFailedAttempts = 0;
#endif
        class LockMultiple : IDisposable
        {
            object[] lockedObjects;
            public LockMultiple(params object[] toLock)
            {
                lockedObjects = (object[])toLock.Clone();
                int waitFor = -1;
                int failedAttempts = 0;
                bool success = false;
                do
                {
                    success = true;
                    if (waitFor >= 0) Monitor.Enter(toLock[waitFor]); // wait for the object, which was blocked in the previous attempt
                    for (int i = 0; i < toLock.Length; i++)
                    {
                        if (i == waitFor) continue; // already locked
                        bool locked = false;
                        Monitor.TryEnter(toLock[i], ref locked);
                        if (!locked)
                        {   // object i cannot immediately been locked, unlock all other objects and wait for object i
                            for (int j = 0; j < i; j++)
                            {
                                Monitor.Exit(toLock[j]);
                            }
                            if (waitFor >= 0) Monitor.Exit(toLock[waitFor]);
                            waitFor = i;
                            success = false;
                            if (failedAttempts > 0)
                            {
                                Random rnd = new Random();
                                Thread.Sleep(rnd.Next(failedAttempts * 2)); // 
                            }
                            ++failedAttempts;
#if DEBUG
                            lock (lmfa)
                            {
                                maxFailedAttempts = Math.Max(maxFailedAttempts, failedAttempts);
                            }
#endif
                            break;
                        }
                    }
                } while (!success);
            }
            public void Dispose()
            {
                for (int i = 0; i < lockedObjects.Length; i++)
                {
                    Monitor.Exit(lockedObjects[i]);
                }
            }
        }
        static public IDisposable LockMultipleObjects(params object[] toLock)
        {
            return new LockMultiple(toLock);
        }
    }
}
