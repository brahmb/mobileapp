namespace Toggl.Foundation.MvvmCross.Collections
{
    public struct SectionedIndex
    {
        public int Section { get; }
        public int Row { get; }

        public SectionedIndex(int section, int row)
        {
            Section = section;
            Row = row;
        }

        public override string ToString()
        {
            return $"{Section}-{Row}";
        }
    }
}
