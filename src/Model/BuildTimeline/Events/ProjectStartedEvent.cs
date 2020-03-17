namespace Model.BuildTimeline
{
    public class ProjectStartedEvent : Event
    {
        // context of the parent project that started this project, if any
        public ProjectEventContext ParentEventContext { get; set; }

        // path to the file that contains the project data
        public string ProjectFile { get; set; }

        // the ID of the project, used within event contexts
        public int ProjectId { get; set; }
    }
}
