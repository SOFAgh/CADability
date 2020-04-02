using System;
using System.Collections.Generic;
using System.Text;

namespace CADability.UserInterface
{
    /// <summary>
    /// 
    /// </summary>

    public class MRUFiles
    {
        public MRUFiles()
        {
            // 
            // TODO: Add constructor logic here
            //
        }
        public static void AddPath(string path, string filetype)
        {
            try
            {
                if (path == null) return;
                string mru = Settings.GlobalSettings.GetStringValue("MRUFileList", "");
                string[] lastFiles = mru.Split('\n');
                List<string> entries = new List<string>();
                for (int i = 0; i < lastFiles.Length; ++i)
                {
                    if (!lastFiles[i].StartsWith(path) && lastFiles[i].Length > 0) entries.Add(lastFiles[i]);
                }
                if (entries.Count > 10) entries.RemoveAt(0);
                entries.Add(path + ";" + filetype);
                StringBuilder b = new StringBuilder();
                for (int i = 0; i < entries.Count; ++i)
                {
                    b.Append(entries[i] as string);
                    if (i < entries.Count - 1) b.Append("\n"); // das letzte kein "\n"
                }
                mru = b.ToString();
                Settings.GlobalSettings.SetValue("MRUFileList", mru);
            }
            catch { }
        }
        public static string[] GetMRUFiles()
        {

            string mru = Settings.GlobalSettings.GetStringValue("MRUFileList", "");
            return mru.Split('\n');
        }
    }
}
