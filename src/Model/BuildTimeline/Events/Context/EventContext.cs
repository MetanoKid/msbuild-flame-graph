namespace Model.BuildTimeline
{
    public class EventContext
    {
        // the ID of this context
        public int ContextId { get; set; }

        // the ID of the node where this context lives in
        public int NodeId { get; set; }

        public override bool Equals(object obj)
        {
            EventContext other = obj as EventContext;
            return other != null &&
                   ContextId == other.ContextId &&
                   NodeId == other.NodeId;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
