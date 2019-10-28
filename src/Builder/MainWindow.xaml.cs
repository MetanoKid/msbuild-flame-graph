using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace Builder
{
    public partial class MainWindow : Window
    {
        private BuilderViewModel m_viewModel;

        public MainWindow()
        {
            InitializeComponent();

            m_viewModel = new BuilderViewModel();
            DataContext = m_viewModel;
        }

        private void LoadSolution(string path)
        {
            try
            {
                m_viewModel.Solution = Model.SolutionLoader.From(path);
                m_viewModel.SolutionCompiler = new Model.SolutionCompiler();
                m_viewModel.BuildMessages = new System.Collections.ObjectModel.ObservableCollection<Model.BuildMessage>();
                m_viewModel.BuildMessages.CollectionChanged += ScrollBuildMessageToBottom;
            } catch(ArgumentException ex) {
                MessageBox.Show(ex.Message, "Invalid file");
            }
        }

        private void OnBuildMessage(Model.BuildMessage message)
        {
            App.Current.Dispatcher.InvokeAsync(() => {
                m_viewModel.BuildMessages.Add(message);
            });
        }

        private void ScrollBuildMessageToBottom(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (VisualTreeHelper.GetChildrenCount(BuildMessageList) > 0)
            {
                Decorator border = VisualTreeHelper.GetChild(BuildMessageList, 0) as Decorator;
                ScrollViewer scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
                scrollViewer.ScrollToBottom();
            }
        }

        private void OnClickBrowseSolution(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Visual Studio solution|*.sln";

            if (dialog.ShowDialog() == true)
            {
                LoadSolution(dialog.FileName);
            }
        }

        private void OnClickBuildSolutionSaveEvents(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "JSON file (*.json)|*.json";
            dialog.FileName = $"{m_viewModel.BuildTarget} - {m_viewModel.BuildConfiguration} - {m_viewModel.BuildPlatform} - {Path.GetFileNameWithoutExtension(m_viewModel.Solution.Path)} - Events";
            if (dialog.ShowDialog() == true)
            {
                m_viewModel.BuildMessages.Clear();

                List<Model.CompilationDataExtractor> dataExtractors = new List<Model.CompilationDataExtractor>();
                
                // extractor that redirects messages to UI
                dataExtractors.Add(new Model.CallbackPerMessageDataExtractor(OnBuildMessage));

                // extractor that accumulates all raw messages then converts them into a custom representation
                Model.CustomEventFormatExtractor eventsExtractor = new Model.CustomEventFormatExtractor();
                dataExtractors.Add(eventsExtractor);

                // asynchronously build the solution
                Task.Run(() => {
                    m_viewModel.SolutionCompiler.Start(m_viewModel.Solution,
                                                                  m_viewModel.BuildConfiguration,
                                                                  m_viewModel.BuildPlatform,
                                                                  m_viewModel.BuildTarget,
                                                                  Environment.ProcessorCount,
                                                                  Environment.ProcessorCount,
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

        private void OnClickBuildSolutionSaveTimeline(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "JSON file (*.json)|*.json";
            dialog.FileName = $"{m_viewModel.BuildTarget} - {m_viewModel.BuildConfiguration} - {m_viewModel.BuildPlatform} - {Path.GetFileNameWithoutExtension(m_viewModel.Solution.Path)} - Trace";
            if (dialog.ShowDialog() == true)
            {
                m_viewModel.BuildMessages.Clear();

                List<Model.CompilationDataExtractor> dataExtractors = new List<Model.CompilationDataExtractor>();

                // extractor that redirects messages to UI
                dataExtractors.Add(new Model.CallbackPerMessageDataExtractor(OnBuildMessage));

                // extractor that accumulates all raw messages then converts them into a custom representation
                Model.CustomEventFormatExtractor eventsExtractor = new Model.CustomEventFormatExtractor();
                dataExtractors.Add(eventsExtractor);

                // asynchronously build the solution
                Task.Run(() => {
                    m_viewModel.SolutionCompiler.Start(m_viewModel.Solution,
                                                       m_viewModel.BuildConfiguration,
                                                       m_viewModel.BuildPlatform,
                                                       m_viewModel.BuildTarget,
                                                       Environment.ProcessorCount,
                                                       Environment.ProcessorCount,
                                                       dataExtractors);

                    Debug.Assert(eventsExtractor.IsFinished);

                    // build a hierarchical timeline of the events
                    Model.BuildTimeline.TimelineBuilder builder = new Model.BuildTimeline.TimelineBuilder(eventsExtractor.BuildData);
                    Model.BuildTimeline.Timeline timeline = builder.Build();

                    // dump it to file
                    Model.ChromeTrace trace = Model.ChromeTracingSerializer.BuildTrace(timeline);
                    string json = JsonConvert.SerializeObject(trace,
                        Formatting.Indented,
                        new JsonSerializerSettings()
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        });

                    File.WriteAllText(dialog.FileName, json);
                });
            }
        }
    }
}
