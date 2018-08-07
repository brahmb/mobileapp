namespace Toggl.Multivac
{
    public static class HashCode
    {
        public static int From<T1, T2, T3, T4>(T1 itemA, T2 itemB, T3 itemC, T4 itemD)
        {
            unchecked
            {
                return (itemA.GetHashCode() * 397) ^ From(itemB, itemC, itemD);
            }
        }

        public static int From<T1, T2, T3>(T1 itemA, T2 itemB, T3 itemC)
        {
            unchecked
            {
                return (itemA.GetHashCode() * 397) ^ From(itemB, itemC);
            }
        }

        public static int From<T1, T2>(T1 itemA, T2 itemB)
        {
            unchecked
            {
                return (itemA.GetHashCode() * 397) ^ itemB.GetHashCode();
            }
        }
    }
}
