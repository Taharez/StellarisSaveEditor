using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Storage;
using System.IO.Compression;
using Windows.UI.Xaml.Media.Imaging;
using StellarisSaveEditor.Models;
using StellarisSaveEditor.Helpers;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.Xaml.Shapes;
using System.Collections.Generic;
using StellarisSaveEditor.Enums;
using Windows.ApplicationModel.Resources;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace StellarisSaveEditor
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private GameState GameState { get; set; }

        private const double MarkedSystemRadius = 10;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private void MarkSystemFlags_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateMarkedSystems();
        }

        private void HighlightStartingSystem_Checked(object sender, RoutedEventArgs e)
        {
            UpdateStartingSystemHighlight();
        }

        private void HighlightStartingSystem_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateStartingSystemHighlight();
        }

        private void ContentPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ContentPivot.SelectedItem == MapPivot && GameState != null)
            {
                FilterPanel.Visibility = Visibility.Visible;
            }
            else
            {
                FilterPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void GameStateRawSections_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = GameStateRawSections.SelectedItem as ListViewItem;
            var section = selectedItem != null ? selectedItem.DataContext as GameStateRawSection : null;
            UpdateGameStateRawSectionChildList(section);
        }

        private void GameStateRawSectionChildList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = GameStateRawSectionChildList.SelectedItem as ListViewItem;
            var section = selectedItem != null ? selectedItem.DataContext as GameStateRawSection : null;
            UpdateGameStateRawSectionDetails(section);
        }

        private async void SelectFile_Clicked(object sender, RoutedEventArgs e)
        {
            SelectFile.IsEnabled = false;

            var res = ResourceLoader.GetForCurrentView();

            var saveFile = await LoadSaveFile();
            if (saveFile != null)
            {
                UnloadGameState();

                LoadingIndicatorRing.IsActive = true;
                LoadingIndicatorLabel.Text = res.GetString("LoadingSaveFileLabel");
                LoadingIndicatorPanel.Visibility = Visibility.Visible;

                var openedFileLabel = res.GetString("OpenedFileLabel");
                FileNameLabel.Text = string.Format(openedFileLabel, saveFile.Name);

                var gamestateFile = await GetLocalGameStateCopy(saveFile);
                if (gamestateFile != null)
                {
                    var gamestateText = await FileIO.ReadLinesAsync(gamestateFile);

                    GameState = GameStateParser.ParseGamestate(gamestateText.ToList());

                    VersionLabel.Text = GameState.Version;
                    SaveNameLabel.Text = GameState.Name;

                    UpdateFilters();

                    UpdateCanvasMapImage();

                    FilterPanel.Visibility = Visibility.Visible;

                    UpdateGameStateRawView();

                    LoadingIndicatorPanel.Visibility = Visibility.Collapsed;
                    LoadingIndicatorRing.IsActive = false;
                }
            }
            else
            {
                var operationCanceledLabel = res.GetString("OperationCanceledLabel");
                FileNameLabel.Text = operationCanceledLabel;
            }
            SelectFile.IsEnabled = true;
        }

        private async Task<StorageFile> LoadSaveFile()
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".sav");

            StorageFile saveFile = await picker.PickSingleFileAsync();
            return saveFile;
        }

        private async Task<StorageFile> GetLocalGameStateCopy(StorageFile saveFile)
        {
            var localFolder = ApplicationData.Current.LocalFolder;

            // Clean up any old savefile copy
            var oldLocalSaveFile = await localFolder.TryGetItemAsync(saveFile.Name);
            if (oldLocalSaveFile != null)
                await oldLocalSaveFile.DeleteAsync();

            // Copy selected file to local folder (to avoid risk of modification and requiring extra permissions)
            await saveFile.CopyAsync(localFolder);

            // Clean up any old extracted files
            var oldGamestateFile = await localFolder.TryGetItemAsync("gamestate");
            if (oldGamestateFile != null)
                await oldGamestateFile.DeleteAsync();

            var oldMetaFile = await localFolder.TryGetItemAsync("meta");
            if (oldMetaFile != null)
                await oldMetaFile.DeleteAsync();

            // Extract local save file copy to local folder
            var localSaveFile = await localFolder.TryGetItemAsync(saveFile.Name);
            if (localSaveFile != null)
                await Task.Run(() => ZipFile.ExtractToDirectory(localSaveFile.Path, localFolder.Path));

            // Load and parse extracted gamestate file
            var gamestateFile = await localFolder.TryGetItemAsync("gamestate") as StorageFile;
            return gamestateFile;
        }

        private void UpdateFilters()
        {
            MarkSystemFlags.Items.Clear();
            var systemFlags = Enum.GetNames(typeof(GalacticObjectFlag));
            var presentSystemFlags = GameState.GalacticObjects.SelectMany(o => o.GalacticObjectFlags != null ? o.GalacticObjectFlags : new List<Enums.GalacticObjectFlag>()).Distinct().Select(f => f.ToString()).ToList();
            foreach (var systemFlag in systemFlags)
            {
                MarkSystemFlags.Items.Add(new ListBoxItem
                {
                    Content = systemFlag,
                    IsEnabled = presentSystemFlags.Contains(systemFlag)
                });
            }
        }

        private void UpdateCanvasMapImage()
        {
            // Clear any old elements in map canvas
            MapCanvas.Children.Clear();

            // Galactic objects
            var svg = GalacticObjectsRenderer.RenderAsSvg(GameState, MapCanvas.ActualWidth, MapCanvas.ActualHeight);

            var path = new Windows.UI.Xaml.Shapes.Path
            {
                Data = SvgXamlHelper.PathMarkupToGeometry(svg),
                Fill = new SolidColorBrush(Colors.Blue),
                Stretch = Stretch.Fill,
                Width = MapCanvas.ActualWidth,
                Height = MapCanvas.ActualHeight,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            MapCanvas.Children.Add(path);
            Canvas.SetLeft(path, 0);
            Canvas.SetTop(path, 0);

            // Hyper lanes
            var hyperLaneBrush = Resources["ApplicationForegroundThemeBrush"] as Brush;
            var hyperLaneLines = GalacticObjectsRenderer.RenderHyperLanesAsLineList(GameState, MapCanvas.ActualWidth, MapCanvas.ActualHeight);
            foreach(var hyperLaneLine in hyperLaneLines)
            {
                var line = new Line { Stroke = hyperLaneBrush, X1 = hyperLaneLine.Item1.X, Y1 = hyperLaneLine.Item1.Y, X2 = hyperLaneLine.Item2.X, Y2 = hyperLaneLine.Item2.Y };
                MapCanvas.Children.Add(line);
                Canvas.SetLeft(line, 0);
                Canvas.SetTop(line, 0);
            }

            UpdateStartingSystemHighlight();
        }

        private void UpdateStartingSystemHighlight()
        {
            if (StartingSystemsCanvas == null)
                return;

            StartingSystemsCanvas.Children.Clear();
            
            // Player system
            if (HighlightStartingSystem.IsChecked == true)
            {
                var playerSystemCoordinate = GalacticObjectsRenderer.GetPlayerSystemCoordinates(GameState, MapCanvas.ActualWidth, MapCanvas.ActualHeight);
                var playerSystemBrush = new SolidColorBrush(Colors.OrangeRed);
                var playerSystemShape = new Ellipse
                {
                    Stroke = playerSystemBrush,
                    StrokeThickness = 2,
                    Width = 2 * MarkedSystemRadius,
                    Height = 2 * MarkedSystemRadius
                };

                StartingSystemsCanvas.Children.Add(playerSystemShape);
                Canvas.SetLeft(playerSystemShape, playerSystemCoordinate.X - MarkedSystemRadius);
                Canvas.SetTop(playerSystemShape, playerSystemCoordinate.Y - MarkedSystemRadius);
                Canvas.SetZIndex(playerSystemShape, 1000);
            }
        }

        private void UpdateMarkedSystems()
        {
            MarkedSystemsCanvas.Children.Clear();

            if (MarkSystemFlags.SelectedItem != null)
            {
                var markedFlags = MarkSystemFlags.SelectedItems.Select(i => (i as ListBoxItem).Content as string);
                var markedSystemCoordinates = GalacticObjectsRenderer.GetMarkedSystemCoordinates(GameState, MapCanvas.ActualWidth, MapCanvas.ActualHeight, markedFlags);
                var markedSystemBrush = new SolidColorBrush(Colors.DarkTurquoise);
                foreach (var markedSystemCoordinate in markedSystemCoordinates)
                {
                    var markedSystemShape = new Ellipse
                    {
                        Stroke = markedSystemBrush,
                        StrokeThickness = 2,
                        Width = 2 * MarkedSystemRadius,
                        Height = 2 * MarkedSystemRadius
                    };

                    MarkedSystemsCanvas.Children.Add(markedSystemShape);
                    Canvas.SetLeft(markedSystemShape, markedSystemCoordinate.X - MarkedSystemRadius);
                    Canvas.SetTop(markedSystemShape, markedSystemCoordinate.Y - MarkedSystemRadius);
                    Canvas.SetZIndex(markedSystemShape, 1000);
                }
            }
        }

        private void UpdateGameStateRawView()
        {
            GameStateRawAttributes.Items.Clear();
            GameStateRawSections.Items.Clear();

            foreach (var attribute in GameState.GameStateRaw.RootSection.Attributes)
            {
                GameStateRawAttributes.Items.Add(new ListViewItem()
                {
                    Content = (string.IsNullOrEmpty(attribute.Name) ? "" : attribute.Name + ": ") + attribute.Value,
                    DataContext = attribute
                });
            }
            foreach (var rawSection in GameState.GameStateRaw.RootSection.Sections)
            {
                var section = new ListViewItem()
                {
                    Content = string.IsNullOrEmpty(rawSection.Name) ? "*" : rawSection.Name,
                    DataContext = rawSection
                };
                GameStateRawSections.Items.Add(section);
            }
        }

        private void UpdateGameStateRawSectionChildList(GameStateRawSection selectedSection)
        {
            GameStateRawSectionChildList.Items.Clear();

            if (selectedSection == null)
                return;

            foreach (var childSection in selectedSection.Sections)
            {
                var section = new ListViewItem()
                {
                    Content = string.IsNullOrEmpty(childSection.Name) ? "*" : childSection.Name,
                    DataContext = childSection
                };
                GameStateRawSectionChildList.Items.Add(section);
            }

            foreach (var attribute in selectedSection.Attributes)
            {
                var section = new ListViewItem()
                {
                    Content = (string.IsNullOrEmpty(attribute.Name) ? "" : attribute.Name + ": ") + attribute.Value,
                    DataContext = attribute
                };
                GameStateRawSectionChildList.Items.Add(section);
            }
        }

        private void UpdateGameStateRawSectionDetails(GameStateRawSection selectedSection)
        {
            GameStateRawSectionDetails.RootNodes.Clear();

            if (selectedSection == null)
                return;

            var rootNode = new TreeViewNode { IsExpanded = true };
            GameStateRawSectionDetails.RootNodes.Add(rootNode);

            PopulateGameStateRawSectionDetails(rootNode, selectedSection);
        }

        private void PopulateGameStateRawSectionDetails(TreeViewNode node, GameStateRawSection section)
        {
            foreach (var childSection in section.Sections)
            {
                var childNode = new TreeViewNode { Content = string.IsNullOrEmpty(childSection.Name) ? "*" : childSection.Name };
                node.Children.Add(childNode);
                PopulateGameStateRawSectionDetails(childNode, childSection);
            }

            foreach(var attribute in section.Attributes)
            {
                node.Children.Add(new TreeViewNode { Content = (string.IsNullOrEmpty(attribute.Name) ? "" : attribute.Name + ": ") + attribute.Value });
            }
        }

        private void UnloadGameState()
        {
            FileNameLabel.Text = "";
            VersionLabel.Text = "";
            SaveNameLabel.Text = "";
            FilterPanel.Visibility = Visibility.Collapsed;
            MapCanvas.Children.Clear();
            StartingSystemsCanvas.Children.Clear();
            MarkSystemFlags.Items.Clear();
            MarkedSystemsCanvas.Children.Clear();
            GameStateRawAttributes.Items.Clear();
            GameStateRawSections.Items.Clear();
            GameStateRawSectionChildList.Items.Clear();
            GameStateRawSectionDetails.RootNodes.Clear();
            GameState = null;
        }
    }
}
