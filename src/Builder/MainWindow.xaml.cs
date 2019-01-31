using Microsoft.Win32;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

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
            m_viewModel.SolutionCompiler = new Model.SolutionCompiler(m_viewModel.Solution, OnBuildMessage);
            m_viewModel.BuildMessages = new System.Collections.ObjectModel.ObservableCollection<Model.BuildMessage>();
        }

        private void OnBuildMessage(Model.BuildMessage message)
        {
            App.Current.Dispatcher.InvokeAsync(() => {
                m_viewModel.BuildMessages.Add(message);
            });
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
            Task.Run((Action) m_viewModel.SolutionCompiler.Start);
        }
    }
}
