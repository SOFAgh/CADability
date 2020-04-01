using System;
using System.Collections.Generic;

namespace CADability
{
    internal class PerformanceTick : IDisposable
    {
        private string Name;
        private bool IsRunning;
        System.Diagnostics.Stopwatch stopWatch;
        public PerformanceTick(string Name)
        {
            this.Name = Name;
            IsRunning = true;
            stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
        }
        #region IDisposable Members

        public void Dispose()
        {
            if (!IsRunning) return;
            IsRunning = false;
            stopWatch.Stop();
            TimeSpan dt = stopWatch.Elapsed;
            if (PerformanceTimer.AllTimers.ContainsKey(Name))
            {
                PerformanceTimer.AllTimers[Name] = PerformanceTimer.AllTimers[Name].Add(dt);
            }
            else
            {
                PerformanceTimer.AllTimers[Name] = dt;
            }
        }

        #endregion
    }

    /// <summary>
    /// Summary description for PerformanceTimer.
    /// </summary>

    public class PerformanceTimer
    {
        static public Dictionary<string, TimeSpan> AllTimers;
        static PerformanceTimer()
        {
            AllTimers = new Dictionary<string, TimeSpan>();
        }
        static public void Print()
        {
            foreach (KeyValuePair<string, TimeSpan> de in AllTimers)
            {
                string Category = de.Key.ToString();
                System.Diagnostics.Trace.WriteLine(Category + ": " + de.Value.ToString());
            }
        }
    }
}
