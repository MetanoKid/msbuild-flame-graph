namespace Model.BuildTimeline
{
    public class TargetFinishedEvent : Event
    {
        // path to the file that contains the project data
        public string ProjectFile { get; set; }

        // whether the target built successfully
        public bool Succeeded { get; set; }

        // path to the file that contains the target data
        public string TargetFile { get; set; }

        // name of the target started with this event
        public string TargetName { get; set; }
    }
}
