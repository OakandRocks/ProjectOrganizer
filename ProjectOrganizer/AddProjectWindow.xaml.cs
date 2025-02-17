using System;
using System.IO;
using System.Windows;
using Ookii.Dialogs.Wpf;
using static ProjectOrganizer.MainWindow;

namespace ProjectOrganizer
{
    public partial class AddProjectWindow : Window
    {
        private MainWindow _mainWindow;
        private TextBox[] _customPathTextBoxes;

        public AddProjectWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;

            // Store custom path textboxes for easy access
            _customPathTextBoxes = new TextBox[]
            {
            DocumentsPathTextBox,
            ImagesPathTextBox,
            VMDKPathTextBox,
            AdobeProjectsPathTextBox,
            CustomFoldersPathTextBox,
            OtherPathTextBox
            };

            // Set UI state based on current mode
            UpdateUIBasedOnMode();
        }

        private void UpdateUIBasedOnMode()
        {
            bool isCustomMode = _mainWindow._config.Mode == "Custom";

            // Show/hide custom path inputs based on mode
            foreach (var textBox in _customPathTextBoxes)
            {
                textBox.IsEnabled = isCustomMode;
                textBox.Visibility = isCustomMode ? Visibility.Visible : Visibility.Collapsed;
            }

            // Show/hide browse buttons
            foreach (var button in CustomPathsPanel.Children.OfType<Button>())
            {
                button.IsEnabled = isCustomMode;
                button.Visibility = isCustomMode ? Visibility.Visible : Visibility.Collapsed;
            }

            // Show/hide labels
            foreach (var label in CustomPathsPanel.Children.OfType<Label>())
            {
                label.Visibility = isCustomMode ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string projectName = ProjectNameTextBox.Text.Trim();

            // Validation
            if (string.IsNullOrEmpty(projectName))
            {
                MessageBox.Show("Project Name is required!", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_mainWindow._config.Projects.ContainsKey(projectName))
            {
                MessageBox.Show("A project with this name already exists!", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_mainWindow._config.GlobalProjectPath))
            {
                MessageBox.Show("Global Project Path is not set!", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Create project root directory
                string projectRootPath = Path.Combine(_mainWindow._config.GlobalProjectPath, projectName);
                Directory.CreateDirectory(projectRootPath);

                // Create new project
                var newProject = new Project { Name = projectName };

                // Initialize and create standard paths
                newProject.StandardPaths.InitializeStandardPaths(projectRootPath);

                // Create standard directories
                Directory.CreateDirectory(newProject.StandardPaths.DocumentsPath);
                Directory.CreateDirectory(newProject.StandardPaths.ImagesPath);
                Directory.CreateDirectory(newProject.StandardPaths.VMDKPath);
                Directory.CreateDirectory(newProject.StandardPaths.AdobeProjectsPath);
                Directory.CreateDirectory(newProject.StandardPaths.CustomFoldersPath);
                Directory.CreateDirectory(newProject.StandardPaths.OtherPath);

                // If in Custom mode, save custom paths
                if (_mainWindow._config.Mode == "Custom")
                {
                    newProject.CustomPaths = new CustomPaths
                    {
                        DocumentsPath = DocumentsPathTextBox.Text,
                        ImagesPath = ImagesPathTextBox.Text,
                        VMDKPath = VMDKPathTextBox.Text,
                        AdobeProjectsPath = AdobeProjectsPathTextBox.Text,
                        CustomFoldersPath = CustomFoldersPathTextBox.Text,
                        OtherPath = OtherPathTextBox.Text
                    };
                }

                // Add project to config
                _mainWindow._config.Projects.Add(projectName, newProject);
                _mainWindow._config.ActiveProject = projectName;

                // Save config and refresh UI
                _mainWindow.SaveConfig();
                _mainWindow.PopulateProjectsComboBox();

                MessageBox.Show("Project created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating project: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BrowseCustomPath(TextBox targetTextBox)
        {
            var dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog(this) == true)
            {
                targetTextBox.Text = dialog.SelectedPath;
            }
        }

        private void BrowseDocumentsPath_Click(object sender, RoutedEventArgs e)
            => BrowseCustomPath(DocumentsPathTextBox);

        private void BrowseImagesPath_Click(object sender, RoutedEventArgs e)
            => BrowseCustomPath(ImagesPathTextBox);

        private void BrowseVMDKPath_Click(object sender, RoutedEventArgs e)
            => BrowseCustomPath(VMDKPathTextBox);

        private void BrowseAdobeProjectsPath_Click(object sender, RoutedEventArgs e)
            => BrowseCustomPath(AdobeProjectsPathTextBox);

        private void BrowseCustomFoldersPath_Click(object sender, RoutedEventArgs e)
            => BrowseCustomPath(CustomFoldersPathTextBox);

        private void BrowseOtherPath_Click(object sender, RoutedEventArgs e)
            => BrowseCustomPath(OtherPathTextBox);
    }
}