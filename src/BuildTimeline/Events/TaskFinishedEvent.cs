namespace BuildTimeline
{
    public class TaskFinishedEvent : Event
    {
        // path to the file that contains the project data
        public string ProjectFile { get; set; }

        // whether the task built successfully
        public bool Succeeded { get; set; }

        // path to the file that contains the task data
        public string TaskFile { get; set; }

        // name of the task started with this event
        public string TaskName { get; set; }
    }
}
