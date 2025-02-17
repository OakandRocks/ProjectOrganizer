using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using Ookii.Dialogs.Wpf;

namespace ProjectOrganizer
{
    // File Type Mapping Class (New)
    public static class FileTypeMapper
    {
        private static readonly Dictionary<string, string> FileTypeToFolderMap = new()
        {
            // Documents
            { "doc", "Documents" },
            { "docx", "Documents" },
            { "pdf", "Documents" },
            { "txt", "Documents" },
            { "rtf", "Documents" },
            
            // Images
            { "jpg", "Images" },
            { "jpeg", "Images" },
            { "png", "Images" },
            { "gif", "Images" },
            { "bmp", "Images" },
            { "tiff", "Images" },
            
            // VMDK
            { "vmdk", "VMDK" },
            { "vdi", "VMDK" },
            { "vhd", "VMDK" },
            { "vhdx", "VMDK" },
            
            // Adobe Projects
            { "psd", "Adobe Projects" },
            { "ai", "Adobe Projects" },
            { "indd", "Adobe Projects" },
            { "prproj", "Adobe Projects" },
            { "aep", "Adobe Projects" }
        };

        public static string GetTargetFolder(string fileExtension)
        {
            fileExtension = fileExtension.ToLower().TrimStart('.');
            return FileTypeToFolderMap.TryGetValue(fileExtension, out string folder)
                ? folder
                : "Other";
        }
    }

    // Project Extensions Class (New)
    public static class ProjectExtensions
    {
        public static string GetTargetPath(this MainWindow.Project project, string fileExtension, string mode)
        {
            string folderName = FileTypeMapper.GetTargetFolder(fileExtension);
            var activePaths = project.GetActivePaths(mode);

            return folderName switch
            {
                "Documents" => activePaths.DocumentsPath,
                "Images" => activePaths.ImagesPath,
                "VMDK" => activePaths.VMDKPath,
                "Adobe Projects" => activePaths.AdobeProjectsPath,
                "Custom Folders" => activePaths.CustomFoldersPath,
                _ => activePaths.OtherPath
            };
        }
    }

    public partial class MainWindow : Window
    {
        // Data Structures
        public class Project
        {
            public string Name { get; set; }
            public StandardPaths StandardPaths { get; set; } = new StandardPaths();
            public CustomPaths CustomPaths { get; set; } = new CustomPaths();

            public Project()
            {
                StandardPaths = new StandardPaths();
                CustomPaths = new CustomPaths();
            }

            public ProjectPaths GetActivePaths(string mode)
            {
                if (mode == "Custom" && CustomPaths.HasCustomPaths())
                {
                    return new ProjectPaths
                    {
                        DocumentsPath = CustomPaths.DocumentsPath ?? StandardPaths.DocumentsPath,
                        ImagesPath = CustomPaths.ImagesPath ?? StandardPaths.ImagesPath,
                        VMDKPath = CustomPaths.VMDKPath ?? StandardPaths.VMDKPath,
                        AdobeProjectsPath = CustomPaths.AdobeProjectsPath ?? StandardPaths.AdobeProjectsPath,
                        CustomFoldersPath = CustomPaths.CustomFoldersPath ?? StandardPaths.CustomFoldersPath,
                        OtherPath = CustomPaths.OtherPath ?? StandardPaths.OtherPath
                    };
                }
                return new ProjectPaths
                {
                    DocumentsPath = StandardPaths.DocumentsPath,
                    ImagesPath = StandardPaths.ImagesPath,
                    VMDKPath = StandardPaths.VMDKPath,
                    AdobeProjectsPath = StandardPaths.AdobeProjectsPath,
                    CustomFoldersPath = StandardPaths.CustomFoldersPath,
                    OtherPath = StandardPaths.OtherPath
                };
            }
        }

        public class StandardPaths
        {
            public string DocumentsPath { get; set; }
            public string ImagesPath { get; set; }
            public string VMDKPath { get; set; }
            public string AdobeProjectsPath { get; set; }
            public string CustomFoldersPath { get; set; }
            public string OtherPath { get; set; }

            public void InitializeStandardPaths(string projectRootPath)
            {
                DocumentsPath = Path.Combine(projectRootPath, "Documents");
                ImagesPath = Path.Combine(projectRootPath, "Images");
                VMDKPath = Path.Combine(projectRootPath, "VMDK");
                AdobeProjectsPath = Path.Combine(projectRootPath, "Adobe Projects");
                CustomFoldersPath = Path.Combine(projectRootPath, "Custom Folders");
                OtherPath = Path.Combine(projectRootPath, "Other");
            }
        }

        public class CustomPaths
        {
            public string DocumentsPath { get; set; }
            public string ImagesPath { get; set; }
            public string VMDKPath { get; set; }
            public string AdobeProjectsPath { get; set; }
            public string CustomFoldersPath { get; set; }
            public string OtherPath { get; set; }

            public bool HasCustomPaths()
            {
                return !string.IsNullOrEmpty(DocumentsPath) ||
                       !string.IsNullOrEmpty(ImagesPath) ||
                       !string.IsNullOrEmpty(VMDKPath) ||
                       !string.IsNullOrEmpty(AdobeProjectsPath) ||
                       !string.IsNullOrEmpty(CustomFoldersPath) ||
                       !string.IsNullOrEmpty(OtherPath);
            }
        }

        public class ProjectPaths
        {
            public string DocumentsPath { get; set; }
            public string ImagesPath { get; set; }
            public string VMDKPath { get; set; }
            public string AdobeProjectsPath { get; set; }
            public string CustomFoldersPath { get; set; }
            public string OtherPath { get; set; }
        }

        public class FileCopyEntry
        {
            public string Filename { get; set; }
            public string Filetype { get; set; }
            public string Status { get; set; }
        }

        public class AppConfig
        {
            public string GlobalProjectPath { get; set; } = "";
            public string Mode { get; set; } = "Custom";
            public string ActiveProject { get; set; } = "";
            public Dictionary<string, Project> Projects { get; set; } = new Dictionary<string, Project>();
        }

        // Collections
        public ObservableCollection<FileCopyEntry> Files { get; set; } = new ObservableCollection<FileCopyEntry>();

        // Configuration
        internal AppConfig _config = new AppConfig();
        private const string ConfigFile = "config.json";

        public MainWindow()
        {
            InitializeComponent();
            LogListView.ItemsSource = Files;
            PopulateProjectsComboBox();
            GlobalPathTextBox.Text = _config.GlobalProjectPath;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadConfig();
            PopulateProjectsComboBox();
        }

        private void OpenSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsDialog = new SettingsWindow(this);
            settingsDialog.Owner = this;
            settingsDialog.ShowDialog();
        }

        private void AddProjectButton_Click(object sender, RoutedEventArgs e)
        {
            AddProjectWindow addProjectWindow = new AddProjectWindow(this);
            addProjectWindow.Owner = this;
            addProjectWindow.ShowDialog();
        }

        private void ManageProjectButton_Click(object sender, RoutedEventArgs e)
        {
            //Call manage project window
        }

        internal void LoadConfig()
        {
            if (File.Exists(ConfigFile))
            {
                try
                {
                    string json = File.ReadAllText(ConfigFile);
                    _config = JsonConvert.DeserializeObject<AppConfig>(json) ?? new AppConfig();
                    PopulateProjectsComboBox();
                    GlobalPathTextBox.Text = _config.GlobalProjectPath;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading config: {ex.Message}, using default.");
                    _config = new AppConfig();
                }
            }
            else
            {
                _config = new AppConfig();
                PopulateProjectsComboBox();
                GlobalPathTextBox.Text = _config.GlobalProjectPath;
            }
        }

        internal void SaveConfig()
        {
            try
            {
                _config.GlobalProjectPath = GlobalPathTextBox.Text;
                string json = JsonConvert.SerializeObject(_config, Formatting.Indented);
                File.WriteAllText(ConfigFile, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving config: {ex.Message}");
            }
        }

        private void ProjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProjectComboBox.SelectedItem != null)
            {
                _config.ActiveProject = ProjectComboBox.SelectedItem.ToString();
                SaveConfig();
            }
        }

        private void BrowseGlobalPathButton_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog(this) == true)
            {
                GlobalPathTextBox.Text = dialog.SelectedPath;
                SaveConfig();
            }
        }

        private void ListBox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
        }

        private void ListBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string file in files)
                {
                    ProcessFile(file);
                }
            }
        }

        private void LogListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (Clipboard.ContainsFileDropList())
                {
                    System.Collections.Specialized.StringCollection files = Clipboard.GetFileDropList();
                    if (files != null)
                    {
                        foreach (string filePath in files)
                        {
                            ProcessFile(filePath);
                        }
                    }
                }
            }
        }

        // Updated ProcessFile method
        private void ProcessFile(string filePath)
        {
            if (string.IsNullOrEmpty(_config.ActiveProject))
            {
                MessageBox.Show("Please select a project first.", "No Active Project", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!_config.Projects.TryGetValue(_config.ActiveProject, out var activeProject))
            {
                MessageBox.Show("Active project not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string filename = Path.GetFileName(filePath);
            string fileExtension = Path.GetExtension(filePath);

            var logEntry = new FileCopyEntry
            {
                Filename = filename,
                Filetype = fileExtension.TrimStart('.'),
                Status = "Processing"
            };
            Files.Add(logEntry);
            RefreshLog();

            try
            {
                string targetFolderPath = activeProject.GetTargetPath(fileExtension, _config.Mode);
                string targetFilePath = Path.Combine(targetFolderPath, filename);

                Directory.CreateDirectory(targetFolderPath);
                targetFilePath = GetUniqueFilePath(targetFilePath);
                File.Copy(filePath, targetFilePath);

                logEntry.Status = "Complete";
                RefreshLog();
            }
            catch (Exception ex)
            {
                logEntry.Status = $"Failed: {ex.Message}";
                RefreshLog();
            }
        }

        // New method for handling file name conflicts
        private string GetUniqueFilePath(string filePath)
        {
            if (!File.Exists(filePath)) return filePath;

            string directory = Path.GetDirectoryName(filePath);
            string filename = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);
            int counter = 1;

            string newFilePath;
            do
            {
                newFilePath = Path.Combine(directory, $"{filename} ({counter}){extension}");
                counter++;
            } while (File.Exists(newFilePath));

            return newFilePath;
        }

        // Updated PopulateProjectsComboBox method
        public void PopulateProjectsComboBox()
        {
            if (string.IsNullOrEmpty(_config.GlobalProjectPath) || !Directory.Exists(_config.GlobalProjectPath))
            {
                ProjectComboBox.ItemsSource = null;
                return;
            }

            var directories = Directory.GetDirectories(_config.GlobalProjectPath)
                                     .Select(Path.GetFileName)
                                     .Where(dir => dir != null)
                                     .ToList();

            foreach (var dir in directories)
            {
                if (!_config.Projects.ContainsKey(dir))
                {
                    var project = new Project { Name = dir };
                    project.StandardPaths.InitializeStandardPaths(Path.Combine(_config.GlobalProjectPath, dir));
                    _config.Projects[dir] = project;
                }
            }

            var nonExistentProjects = _config.Projects.Keys
                                            .Where(project => !directories.Contains(project))
                                            .ToList();
            foreach (var project in nonExistentProjects)
            {
                _config.Projects.Remove(project);
            }

            ProjectComboBox.ItemsSource = directories;
            ProjectComboBox.SelectedItem = _config.ActiveProject;
            SaveConfig();
        }

        public void RefreshLog()
        {
            CollectionViewSource.GetDefaultView(Files).Refresh();
        }
    }
}