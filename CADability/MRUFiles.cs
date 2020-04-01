using System;
using System.Collections;
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
                return;
                //if (path == null) return;
                //Microsoft.Win32.RegistryKey mru = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("Software\\SOFA\\CADability");
                //if (mru == null) return;
                //object list = mru.GetValue("MRUFileList");
                //if (list == null || !(list is string))
                //{
                //    mru.SetValue("MRUFileList", path);
                //}
                //else
                //{
                //    string[] lastFiles = (list as string).Split('\n');
                //    ArrayList al = new ArrayList();
                //    for (int i = 0; i < lastFiles.Length; ++i)
                //    {
                //        if (!lastFiles[i].StartsWith(path) && lastFiles[i].Length > 0) al.Add(lastFiles[i]);
                //    }
                //    if (al.Count > 10) al.RemoveAt(0);
                //    al.Add(path + ";" + filetype);
                //    StringBuilder b = new StringBuilder();
                //    for (int i = 0; i < al.Count; ++i)
                //    {
                //        b.Append(al[i] as string);
                //        if (i < al.Count - 1) b.Append("\n"); // das letzte kein "\n"
                //    }
                //    try
                //    {
                //        mru.SetValue("MRUFileList", b.ToString());
                //    }
                //    catch (UnauthorizedAccessException) { }
                //    mru.Close();
                //}
            }
            catch { }
        }
        public static string[] GetMRUFiles()
        {

            try
            {
                return new string[] { };
                //Microsoft.Win32.RegistryKey mru = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\SOFA\\CADability");
                //if (mru == null)
                //{
                //    return new string[] { };
                //}
                //object list = mru.GetValue("MRUFileList");
                //mru.Close();
                //if (list == null || !(list is string))
                //{
                //    return new string[] { };
                //}
                //else
                //{
                //    return (list as string).Split('\n');
                //}
            }
            catch
            {
                return new string[] { };
            }
        }
        // die folgenden werden nicht verwendet, stattdessen eine statische in der Instanz
        //public static void SetMRUFileType(int fileType)
        //{
        //    try
        //    {
        //        Microsoft.Win32.RegistryKey mru = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("Software\\SOFA\\CADability");
        //        if (mru == null) return;
        //        mru.SetValue("MRUFileType", fileType);
        //    }
        //    catch { }
        //}
        //public static int GetMRUFileType()
        //{
        //    try
        //    {
        //        Microsoft.Win32.RegistryKey mru = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("Software\\SOFA\\CADability");
        //        if (mru == null) return 1;
        //        object fileType = mru.GetValue("MRUFileType");
        //        if (fileType != null) return (int)fileType;
        //        return 1;
        //    }
        //    catch
        //    {
        //        return 1;
        //    }
        //}
        /// <summary>
        /// Replaces entries in the given menu with the menuid "MenuId.File.Mru.FileN"
        /// with the most recently used file N or removes this item
        /// </summary>
        /// <param name="toModify"></param>
        //public static void ModifyMenu(Menu toModify)
        //{
        //    if (toModify.IsParent)
        //    {
        //        Menu[] items = new MenuItemWithID[toModify.MenuItems.Count];
        //        toModify.MenuItems.CopyTo(items, 0);
        //        for (int i = 0; i < items.Length; ++i)
        //        {
        //            ModifyMenu(items[i]);
        //        }
        //    }
        //    else
        //    {
        //        MenuItemWithID mid = toModify as MenuItemWithID;

        //        if (mid != null && mid.ID.StartsWith("MenuId.File.Mru.File"))
        //        {
        //            string filenr = mid.ID.Substring(20);
        //            try
        //            {
        //                int n = int.Parse(filenr);
        //                string[] files = GetMRUFiles();
        //                if (n < files.Length && n > 0)
        //                {
        //                    string[] parts = files[files.Length - n].Split(';');
        //                    if (parts.Length > 1)
        //                        mid.Text = parts[0];
        //                }
        //                else
        //                {
        //                    toModify.MenuItems.Remove(mid);
        //                }
        //            }
        //            catch (FormatException) { }
        //            catch (OverflowException) { }
        //        }
        //    }
        //}

    }
}
