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
            //            public LockMultiple(params object[] toLock)
            //            {
            //                lockedObjects = (object[])toLock.Clone();
            //                int waitFor = -1;
            //                int failedAttempts = 0;
            //                bool success = false;
            //                do
            //                {
            //                    success = true;
            //                    if (waitFor >= 0) Monitor.Enter(toLock[waitFor]); // wait for the object, which was blocked in the previous attempt
            //                    for (int i = 0; i < toLock.Length; i++)
            //                    {
            //                        if (i == waitFor) continue; // already locked
            //                        bool locked = false;
            //                        Monitor.TryEnter(toLock[i], ref locked);
            //                        if (!locked)
            //                        {   // object i cannot immediately been locked, unlock all other objects and wait for object i
            //                            for (int j = 0; j < i; j++)
            //                            {
            //                                Monitor.Exit(toLock[j]);
            //                            }
            //                            if (waitFor >= 0) Monitor.Exit(toLock[waitFor]);
            //                            waitFor = i;
            //                            success = false;
            //                            if (failedAttempts > 0)
            //                            {
            //                                Random rnd = new Random();
            //                                Thread.Sleep(rnd.Next(failedAttempts * 2)); // 
            //                            }
            //                            ++failedAttempts;
            //#if DEBUG
            //                            lock (lmfa)
            //                            {
            //                                maxFailedAttempts = Math.Max(maxFailedAttempts, failedAttempts);
            //                            }
            //#endif
            //                            break;
            //                        }
            //                    }
            //                } while (!success);
            //            }
            public LockMultiple(params object[] toLock)
            {   // very simple aproach, assuming the objects are sorted.
                // There is following deadlock scenarion: object A needs exclusive access to object 1, 2 and 3 and object B needs exclusive access to object 2, 1 and 4
                // then A locks 1 and B locks 2, then A waits for 2 and B waits for 1 to get unlocked.
                // But when the objects are sorted, such a deadlock cannot happen (whatever sorting mechanism is used, as long as it is the same for both A and B)
                if (toLock != null)
                {
                    lockedObjects = (object[])toLock.Clone();
                    for (int i = 0; i < toLock.Length; i++)
                    {
                        Monitor.Enter(toLock[i]);
                    }
                }
            }
            public void Dispose()
            {
                if (lockedObjects != null)
                {
                    for (int i = 0; i < lockedObjects.Length; i++)
                    {
                        Monitor.Exit(lockedObjects[i]);
                    }
                }
            }
        }
        static public IDisposable LockMultipleObjects(params object[] toLock)
        {
            return new LockMultiple(toLock);
        }
    }
}
