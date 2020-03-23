using Microsoft.Win32;
using MSBuildWrapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Builder
{
    // https://stackoverflow.com/a/33088947/1257656
    public class DelegateCommand<T> : ICommand
    {
        private Action<T> m_delegateMethod;
        public event EventHandler CanExecuteChanged;

        public DelegateCommand(Action<T> delegateMethod)
        {
            m_delegateMethod = delegateMethod;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            Debug.Assert(m_delegateMethod != null);
            m_delegateMethod((T) parameter);
        }
    }

    public class Commands
    {
        public ICommand OnSelectSolution { get; private set; }
        public ICommand OnSelectBuildSolution { get; private set; }
        public ICommand OnSelectCreateTimelineFromEventsFile { get; private set; }

        private BuilderViewModel m_viewModel;

        public Commands(BuilderViewModel viewModel)
        {
            m_viewModel = viewModel;

            OnSelectSolution = new DelegateCommand<object>(_ => OpenSolution());
            OnSelectBuildSolution = new DelegateCommand<MainWindow>(BuildSolution);
            OnSelectCreateTimelineFromEventsFile = new DelegateCommand<object>(_ => CreateTimeline());
        }

        private void OpenSolution()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Visual Studio solution|*.sln";

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    m_viewModel.LoadSolution(dialog.FileName);
                }
                catch (ArgumentException ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private string BuildEventsFileName(BuilderViewModel viewModel)
        {
            StringBuilder builder = new StringBuilder();

            builder.Append("Events - ");
            builder.Append($"{Path.GetFileNameWithoutExtension(viewModel.Solution.Path)} - ");

            if(viewModel.CurrentBuildConfiguration.Project != SolutionCompiler.s_CompileFullSolution)
            {
                builder.Append($"{viewModel.CurrentBuildConfiguration.Project} - ");
            }

            builder.Append($"{viewModel.CurrentBuildConfiguration.Target} - ");
            builder.Append($"{viewModel.CurrentBuildConfiguration.ConfigurationPlatform.Configuration} - ");
            builder.Append($"{viewModel.CurrentBuildConfiguration.ConfigurationPlatform.Platform}");

            return builder.ToString();
        }

        private string BuildTraceFileName(BuildTimeline.BuildData data)
        {
            StringBuilder builder = new StringBuilder();
            
            builder.Append("Trace - ");
            builder.Append($"{Path.GetFileNameWithoutExtension(data.SolutionPath)} - ");

            if (data.Project != null)
            {
                builder.Append($"{data.Project} - ");
                // MSBuild requires it in the form "Project:Target"
                builder.Append($"{data.Target.Split(':')[1]} - ");
            }
            else
            {
                builder.Append($"{data.Target} - ");
            }

            builder.Append($"{data.Configuration} - ");
            builder.Append($"{data.Platform}");

            return builder.ToString();
        }

        private void BuildSolution(MainWindow window)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "JSON file (*.json)|*.json";
            dialog.FileName = BuildEventsFileName(m_viewModel);
            if (dialog.ShowDialog() == true)
            {
                m_viewModel.BuildMessages.Clear();

                List<CompilationDataExtractor> dataExtractors = new List<CompilationDataExtractor>();

                // extractor that redirects messages to UI
                dataExtractors.Add(new CallbackPerMessageDataExtractor(window.OnBuildMessage));

                // extractor that accumulates all raw messages then converts them into a custom representation
                CustomEventFormatExtractor eventsExtractor = new CustomEventFormatExtractor();
                dataExtractors.Add(eventsExtractor);

                // asynchronously build the solution
                Task.Run(() => {
                    m_viewModel.SolutionCompiler.Start(m_viewModel.Solution,
                                                       m_viewModel.CurrentBuildConfiguration,
                                                       dataExtractors);

                    Debug.Assert(eventsExtractor.IsFinished);

                    // dump custom data to the requested JSON file
                    string json = JsonConvert.SerializeObject(eventsExtractor.BuildData,
                        Formatting.Indented,
                        new JsonSerializerSettings()
                        {
                            NullValueHandling = NullValueHandling.Ignore,
                            TypeNameHandling = TypeNameHandling.Auto,
                        });

                    File.WriteAllText(dialog.FileName, json);
                });
            }
        }

        private void CreateTimeline()
        {
            OpenFileDialog openEventsFileDialog = new OpenFileDialog();
            openEventsFileDialog.Filter = "Events dump file|*.json";
            
            if (openEventsFileDialog.ShowDialog() == true)
            {
                // load data from file
                string eventsJSON = File.ReadAllText(openEventsFileDialog.FileName);
                BuildTimeline.BuildData data = JsonConvert.DeserializeObject<BuildTimeline.BuildData>(eventsJSON, new JsonSerializerSettings()
                {
                    TypeNameHandling = TypeNameHandling.Auto
                });
                
                SaveFileDialog saveTimelineFileDialog = new SaveFileDialog();
                saveTimelineFileDialog.Filter = "JSON file (*.json)|*.json";
                saveTimelineFileDialog.FileName = BuildTraceFileName(data);

                if(saveTimelineFileDialog.ShowDialog() == true)
                {
                    // build a hierarchical timeline of the events
                    BuildTimeline.TimelineBuilder builder = new BuildTimeline.TimelineBuilder(data);

                    // include some post-processing
                    BuildTimeline.TimelineEntryPostProcessor.Processor postProcessors = null;

                    if(data.UseBtPlusFlag)
                    {
                        postProcessors += BuildTimeline.TimelineEntryPostProcessor.TaskCLSingleThread;
                        postProcessors += BuildTimeline.TimelineEntryPostProcessor.TaskCLMultiThread;
                    }

                    if(data.UseTimePlusFlag)
                    {
                        postProcessors += BuildTimeline.TimelineEntryPostProcessor.TaskLink;
                    }

                    // build a hierarchical timeline of the events
                    BuildTimeline.Timeline timeline = builder.Build(postProcessors);

                    // dump it to file
                    TimelineSerializer.ChromeTrace trace = TimelineSerializer.ChromeTracingSerializer.BuildTrace(timeline);
                    string json = JsonConvert.SerializeObject(trace,
                        Formatting.Indented,
                        new JsonSerializerSettings()
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        });

                    File.WriteAllText(saveTimelineFileDialog.FileName, json);

                    MessageBox.Show("Timeline built and saved successfully", "File saved");
                }
            }
        }
    }
}
