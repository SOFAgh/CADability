using System.Collections;
using System.Reflection;

namespace CADability.UserInterface
{
    public class CursorTable
    {
        public static string GetCursor(string Name)
        {
            return Name.Replace(".cur", "");

        }
    }
}
