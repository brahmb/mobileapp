namespace Toggl.Foundation.MvvmCross.Collections
{
    public struct CollectionChange
    {
        public SectionedIndex Index;
        public SectionedIndex? OldIndex;
        public CollectionChangeType Type;

        public override string ToString()
        {
            var oldString = OldIndex.HasValue ? $", ({OldIndex.Value})" : "";
            return $"{Type}: {Index} {OldIndex}";
        }
    }

    public enum CollectionChangeType
    {
        AddRow,
        RemoveRow,
        UpdateRow,
        MoveRow,
        AddSection,
        RemoveSection,
        Reload
    }
}
