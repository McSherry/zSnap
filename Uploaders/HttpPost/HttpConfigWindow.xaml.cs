using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace zSnap.Uploaders.HttpPost
{
    /// <summary>
    /// Interaction logic for HttpConfigWindow.xaml
    /// </summary>
    public partial class HttpConfigWindow : Window
    {
        public HttpConfigWindow()
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            InitializeComponent();
            UseAuthCheckBox.IsChecked = Configuration.UseBasicAuth;
            AuthGroupBox.IsEnabled = Configuration.UseBasicAuth;
            UserTBox.Text = Configuration.Username;
            PasswordTBox.Text = Configuration.Password;
            EndpointTBox.Text = Configuration.Destination;
            UseRegexCheckBox.IsChecked = Configuration.UseRegex;
            RegexTBox.IsEnabled = Configuration.UseRegex;
            RegexTBox.Text = Configuration.Regex;
        }

        public void AuthCheckedHandler(object sender, RoutedEventArgs e)
        {
            AuthGroupBox.IsEnabled = true;
        }

        private void AuthUncheckedHandler(object sender, RoutedEventArgs e)
        {
            AuthGroupBox.IsEnabled = false;
        }

        public void RegexCheckedHandler(object sender, RoutedEventArgs e)
        {
            RegexTBox.IsEnabled = true;
        }

        private void RegexUncheckedHandler(object sender, RoutedEventArgs e)
        {
            RegexTBox.IsEnabled = false;
        }

        private void SaveClickedHandler(object sender, RoutedEventArgs e)
        {
            Configuration.Username = UserTBox.Text;
            Configuration.Password = PasswordTBox.Text;
            Configuration.UseBasicAuth = UseAuthCheckBox.IsChecked ?? false;
            Configuration.Destination = EndpointTBox.Text;
            Configuration.UseRegex = UseRegexCheckBox.IsChecked ?? false;
            Configuration.Regex = RegexTBox.Text;
            Close();
        }

        private void CancelClickedHandler(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ResetClickedHandler(object sender, RoutedEventArgs e)
        {
            UserTBox.Text = PasswordTBox.Text = EndpointTBox.Text = RegexTBox.Text = "";
            UseAuthCheckBox.IsChecked = true;
            UseRegexCheckBox.IsChecked = false;
        }
    }
}
