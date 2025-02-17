using System;
using System.Windows;
using Ookii.Dialogs.Wpf;

namespace ProjectOrganizer
{
    public partial class SettingsWindow : Window
    {
        private MainWindow _mainWindow;

        public SettingsWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;

            //Load existing value, if existant.
            GlobalProjectPathTextBox.Text = _mainWindow._config.GlobalProjectPath; //Import values
            if (_mainWindow._config.Mode == "Standard")
            {
                StandardModeRadioButton.IsChecked = true; //Set value for radiobox
            }
        }

        private void BrowseGlobalPath_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog(this) == true)
            {
                GlobalProjectPathTextBox.Text = dialog.SelectedPath;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            //Assign
            _mainWindow._config.GlobalProjectPath = GlobalProjectPathTextBox.Text;
            _mainWindow._config.Mode = CustomModeRadioButton.IsChecked == true ? "Custom" : "Standard";

            _mainWindow.LoadConfig(); //Refresh UI

            //Save settings, and close the dialog
            _mainWindow.SaveConfig();

            this.Close(); //Close window after change
        }

        private void ModeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            //Assign
            if (this.IsLoaded)
            {
                _mainWindow._config.GlobalProjectPath = GlobalProjectPathTextBox.Text;
                _mainWindow._config.Mode = CustomModeRadioButton.IsChecked == true ? "Custom" : "Standard";

                _mainWindow.LoadConfig(); //Refresh UI
            }

        }
    }
}