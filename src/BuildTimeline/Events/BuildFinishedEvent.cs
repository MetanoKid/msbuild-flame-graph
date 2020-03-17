namespace BuildTimeline
{
    public class BuildFinishedEvent : Event
    {
        // whether the build finished successfully
        public bool Succeeded { get; set; }
    }
}
