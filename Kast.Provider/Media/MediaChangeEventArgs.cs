namespace Kast.Provider.Media
{
    public class MediaChangeEventArgs : EventArgs
    {
        public enum EventType
        {
            Added,
            Removed,
            StatusChanged,
            MetadataChanged,
            MediaInfoChanged,
            CompanionChanged,
        }

        public readonly EventType Event;

        public MediaChangeEventArgs(EventType eventType)
        {
            Event = eventType;
        }
    }
}
