namespace Model
{
    public class BuildConfiguration
    {
        public string SolutionPath { get; set; }
        public string Project { get; set; }
        public string Configuration { get; set; }
        public string Platform { get; set; }
        public string Target { get; set; }
        public int MaxParallelProjects { get; set; }
        public int MaxParallelCLTasksPerProject { get; set; }
        public bool UseBtPlusFlag { get; set; }
        public bool UseTimePlusFlag { get; set; }
        public bool UseD1ReportTimeFlag { get; set; }
    }
}
