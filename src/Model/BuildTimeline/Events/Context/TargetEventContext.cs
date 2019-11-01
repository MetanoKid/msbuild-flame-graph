namespace Model.BuildTimeline
{
    public class TargetEventContext : EventContext
    {
        // the ID of the project where this context lives in
        public int ProjectId { get; set; }

        // the ID of the target where this context lives in
        public int TargetId { get; set; }
        
        public override bool Equals(object obj)
        {
            TargetEventContext other = obj as TargetEventContext;
            return other != null &&
                   base.Equals(obj) &&
                   ProjectId == other.ProjectId &&
                   TargetId == other.TargetId;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
