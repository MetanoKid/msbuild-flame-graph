namespace BuildTimeline
{
    public class TargetStartedEvent : Event
    {
        // name of the target that caused this one to build, if any
        public string ParentTarget { get; set; }

        // path to the file that contains the project data
        public string ProjectFile { get; set; }

        // path to the file that contains the target data
        public string TargetFile { get; set; }
        
        // name of the target started with this event
        public string TargetName { get; set; }
    }
}
