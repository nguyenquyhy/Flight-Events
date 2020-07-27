using System.Windows;

namespace FlightEvents.Client.Dialogs
{
    /// <summary>
    /// Interaction logic for TeleportDialog.xaml
    /// </summary>
    public partial class TeleportDialog : Window
    {
        public TeleportDialog()
        {
            InitializeComponent();
        }

        private void ButtonTeleport_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
