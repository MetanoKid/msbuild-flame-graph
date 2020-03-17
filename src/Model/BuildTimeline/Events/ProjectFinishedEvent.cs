namespace Model.BuildTimeline
{
    public class ProjectFinishedEvent : Event
    {
        // path to the file that contains the project data
        public string ProjectFile { get; set; }

        // whether the project built successfully
        public bool Succeeded { get; set; }
    }
}
