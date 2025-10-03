using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace ComputerInfo
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            }
            catch
            {
                MessageBox.Show("Unable to open link in browser.",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
            e.Handled = true;
        }
    }
}
