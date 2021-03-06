﻿namespace BuildTimeline
{
    public class TaskStartedEvent : Event
    {
        // path to the file that contains the project data
        public string ProjectFile { get; set; }

        // path to the file that contains the task data
        public string TaskFile { get; set; }

        // name of the task started with this event
        public string TaskName { get; set; }
    }
}
