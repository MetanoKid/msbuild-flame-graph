namespace Model.BuildTimeline
{
    public class ProjectEventContext : EventContext
    {
        // the ID of the project where this context lives in
        public int ProjectId { get; set; }

        public override bool Equals(object obj)
        {
            ProjectEventContext other = obj as ProjectEventContext;
            return other != null &&
                   base.Equals(obj) &&
                   ProjectId == other.ProjectId;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
