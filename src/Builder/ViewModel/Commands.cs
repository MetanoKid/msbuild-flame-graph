using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public ICommand OnSelectEventsConvertToTimeline { get; private set; }

        private BuilderViewModel m_viewModel;

        public Commands(BuilderViewModel viewModel)
        {
            m_viewModel = viewModel;

            OnSelectSolution = new DelegateCommand<object>(_ => OpenSolution());
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
    }
}
