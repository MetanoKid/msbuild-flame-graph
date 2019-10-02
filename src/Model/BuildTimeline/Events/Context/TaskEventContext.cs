namespace Model.BuildTimeline
{
    public class TaskEventContext : EventContext
    {
        // the ID of the project where this context lives in
        public int ProjectId { get; set; }

        // the ID of the target where this context lives in
        public int TargetId { get; set; }

        // the ID of the task where this context lives in
        public int TaskId { get; set; }

        public override bool Equals(object obj)
        {
            TaskEventContext other = obj as TaskEventContext;
            return other != null &&
                   base.Equals(obj) &&
                   ProjectId == other.ProjectId &&
                   TargetId == other.TargetId &&
                   TaskId == other.TaskId;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
