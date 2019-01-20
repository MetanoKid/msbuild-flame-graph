using Microsoft.Win32;
using System;
using System.Windows;

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

        private void OnClickBrowseSolution(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Visual Studio solution|*.sln";

            if (dialog.ShowDialog() == true)
            {
                // TODO: requires INotifyPropertyChanged implementation
                m_viewModel.Solution = new Model.Solution(dialog.FileName);
            }
        }
    }
}
