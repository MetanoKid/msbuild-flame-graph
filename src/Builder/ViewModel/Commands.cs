using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        public ICommand OnSelectCreateTimelineFromEventsFile { get; private set; }

        private BuilderViewModel m_viewModel;

        public Commands(BuilderViewModel viewModel)
        {
            m_viewModel = viewModel;

            OnSelectSolution = new DelegateCommand<object>(_ => OpenSolution());
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

        private void CreateTimeline()
        {
            OpenFileDialog openEventsFileDialog = new OpenFileDialog();
            openEventsFileDialog.Filter = "Events dump file|*.json";
            
            if (openEventsFileDialog.ShowDialog() == true)
            {
                // load data from file
                string eventsJSON = File.ReadAllText(openEventsFileDialog.FileName);
                Model.BuildData data = JsonConvert.DeserializeObject<Model.BuildData>(eventsJSON, new JsonSerializerSettings()
                {
                    TypeNameHandling = TypeNameHandling.Auto
                });
                
                SaveFileDialog saveTimelineFileDialog = new SaveFileDialog();
                saveTimelineFileDialog.Filter = "JSON file (*.json)|*.json";
                saveTimelineFileDialog.FileName = $"{data.Target} - {data.Configuration} - {data.Platform} - {Path.GetFileNameWithoutExtension(data.SolutionPath)} - Trace";

                if(saveTimelineFileDialog.ShowDialog() == true)
                {
                    // build a hierarchical timeline of the events
                    Model.BuildTimeline.TimelineBuilder builder = new Model.BuildTimeline.TimelineBuilder(data);
                    Model.BuildTimeline.Timeline timeline = builder.Build();

                    // dump it to file
                    Model.ChromeTrace trace = Model.ChromeTracingSerializer.BuildTrace(timeline);
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
