namespace CADability
{
    class Hlp
    {
        public static void Swap<T>(ref T first, ref T second)
        {
            T tmp = first;
            first = second;
            second = tmp;
        }
    }
}
