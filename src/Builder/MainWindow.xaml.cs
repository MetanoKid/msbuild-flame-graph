using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Builder
{
    public partial class MainWindow : Window
    {
        private static readonly Regex s_RegexIntegersOnly = new Regex(@"^\d+$");

        private BuilderViewModel m_viewModel;

        public MainWindow()
        {
            InitializeComponent();

            m_viewModel = new BuilderViewModel();
            m_viewModel.BuildMessages.CollectionChanged += ScrollBuildMessageToBottom;

            DataContext = m_viewModel;
        }

        public void OnBuildMessage(MSBuildWrapper.BuildMessage message)
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

        private void NumericOnlyTextInputEvent(object sender, TextCompositionEventArgs e)
        {
            bool isNumber = s_RegexIntegersOnly.IsMatch(e.Text);
            e.Handled = !isNumber;
        }
    }
}
