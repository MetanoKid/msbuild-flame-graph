using Microsoft.Win32;
using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Diagnostics;
using System.IO;

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
            m_viewModel.Solution = new Model.Solution(path);
            m_viewModel.SolutionCompiler = new Model.SolutionCompiler(OnBuildMessage);
            m_viewModel.BuildMessages = new System.Collections.ObjectModel.ObservableCollection<Model.BuildMessage>();
            m_viewModel.BuildMessages.CollectionChanged += ScrollBuildMessageToBottom;
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

        private void OnClickBuildSolution(object sender, RoutedEventArgs e)
        {
            m_viewModel.BuildMessages.Clear();
            Task.Run(() => m_viewModel.SolutionCompiler.Start(m_viewModel.Solution, "Debug", "x64", "Rebuild"));
        }

        private void OnClickSaveBuildTimeline(object sender, RoutedEventArgs e)
        {
            // ensure compilation is completed before allowing clicking on this button!

            Model.BuildTimelineBuilder builder = new Model.BuildTimelineBuilder(m_viewModel.SolutionCompiler.CurrentCompilation);
            Model.BuildTimeline timeline = builder.Process();
            Debug.Assert(timeline.IsCompleted);

            string text = Model.ChromeTracingSerializer.Serialize(timeline);
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "JSON files (*.json)|*.json";

            if(dialog.ShowDialog() == true)
            {
                File.WriteAllText(dialog.FileName, text);
            }
        }
    }
}
