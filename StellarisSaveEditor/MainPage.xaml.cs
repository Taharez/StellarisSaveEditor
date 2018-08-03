using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO.Compression;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using StellarisSaveEditor.Models;
using StellarisSaveEditor.Models.Enums;
using StellarisSaveEditor.Helpers;
using StellarisSaveEditor.Parser;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace StellarisSaveEditor
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage
    {
        private GameState GameState { get; set; }

        private const double MarkedSystemRadius = 10;

        private readonly DispatcherTimer _resizeTimer;

        public MainPage()
        {
            InitializeComponent();
            _resizeTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 500) };
            _resizeTimer.Tick += ResizeTimerTick;
        }

        void ResizeTimerTick(object sender, object e)
        {
            _resizeTimer.Stop();

            if (GameState != null)
            {
                UnloadMap();
                UpdateMap();
                UpdateHyperLanes();
                UpdateSystemHighlight();
                UpdateMarkedSystems();
            }
        }

        private void Map_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _resizeTimer.Stop();
            _resizeTimer.Start();
        }

        private void HightLightSystemName_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSystemHighlight();
        }

        private void HighlightStartingSystem_Checked(object sender, RoutedEventArgs e)
        {
            UpdateSystemHighlight();
        }

        private void HighlightStartingSystem_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateSystemHighlight();
        }

        private void MarkSystemFlags_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateMarkedSystems();
        }

        private void ShowHyperLanes_Checked(object sender, RoutedEventArgs e)
        {
            UpdateHyperLanes();
        }

        private void ShowHyperLanes_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateHyperLanes();
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
            var section = GameStateRawSections.SelectedItem is ListViewItem selectedItem ? selectedItem.DataContext as GameStateRawSection : null;
            UpdateGameStateRawSectionChildList(section);
        }

        private void GameStateRawSectionChildList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var section = GameStateRawSectionChildList.SelectedItem is ListViewItem selectedItem ? selectedItem.DataContext as GameStateRawSection : null;
            UpdateGameStateRawSectionDetails(section);
        }

        private async void SelectFile_Clicked(object sender, RoutedEventArgs e)
        {
            SelectFile.IsEnabled = false;

            var res = ResourceLoader.GetForCurrentView();

            using (var logger = new UwpLogger())
            {

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

                        var parser = new GameStateParser(logger);
                        GameState = parser.ParseGamestate(gamestateText.ToList());

                        VersionLabel.Text = GameState.Version;
                        SaveNameLabel.Text = GameState.Name;

                        UpdateFilters();

                        UpdateMap();

                        UpdateHyperLanes();

                        UpdateSystemHighlight();

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
                await logger.SaveAsync();
            }
        }

        private async Task<StorageFile> LoadSaveFile()
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker
            {
                ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail,
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary
            };
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
            if (MarkSystemFlags?.Items == null)
                return;

            MarkSystemFlags.Items.Clear();
            var systemFlags = Enum.GetNames(typeof(GalacticObjectFlag)).ToList();
            var presentSystemFlags = GameState.GalacticObjects.SelectMany(o => o.GalacticObjectFlags ?? new List<string>()).Distinct().ToList();
            systemFlags = systemFlags.Union(presentSystemFlags).ToList(); // Make sure we use all flags in file, even if they are unknown (not in enum)
            foreach (var systemFlag in systemFlags)
            {
                MarkSystemFlags.Items.Add(new ListBoxItem
                {
                    Content = systemFlag,
                    IsEnabled = presentSystemFlags.Contains(systemFlag)
                });
            }
        }

        private void UpdateMap()
        {
            // Clear any old elements in map element
            SystemMap.Children.Clear();

            // Galactic objects
            var svg = GalacticObjectsRenderer.RenderAsSvg(GameState, SystemMap.ActualWidth, SystemMap.ActualHeight);

            var path = new Path
            {
                Data = SvgXamlHelper.PathMarkupToGeometry(svg),
                Fill = new SolidColorBrush(Colors.Blue),
                Stretch = Stretch.Fill,
                Width = SystemMap.ActualWidth,
                Height = SystemMap.ActualHeight,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };

            SystemMap.Children.Add(path);
        }

        private void UpdateHyperLanes()
        {
            if (HyperLaneMap == null)
                return;

            HyperLaneMap.Children.Clear();

            if (ShowHyperLanes.IsChecked == true)
            {
                var hyperLaneBrush = Resources["ApplicationForegroundThemeBrush"] as Brush;
                var hyperLaneLines = GalacticObjectsRenderer.RenderHyperLanesAsLineList(GameState, HyperLaneMap.ActualWidth, HyperLaneMap.ActualHeight);
                foreach (var hyperLaneLine in hyperLaneLines)
                {
                    var line = new Line
                    {

                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top,
                        Stroke = hyperLaneBrush,
                        X1 = hyperLaneLine.Item1.X,
                        Y1 = hyperLaneLine.Item1.Y,
                        X2 = hyperLaneLine.Item2.X,
                        Y2 = hyperLaneLine.Item2.Y
                    };
                    HyperLaneMap.Children.Add(line);
                }
            }
        }

        private void UpdateSystemHighlight()
        {
            if (SystemHightlightMap == null)
                return;

            SystemHightlightMap.Children.Clear();
            
            // Player system
            if (HighlightStartingSystem.IsChecked == true)
            {
                var playerSystemCoordinate = GalacticObjectsRenderer.GetPlayerSystemCoordinates(GameState, SystemHightlightMap.ActualWidth, SystemHightlightMap.ActualHeight);
                var playerSystemBrush = new SolidColorBrush(Colors.OrangeRed);
                var playerSystemShape = new Ellipse
                {
                    Stroke = playerSystemBrush,
                    StrokeThickness = 2,
                    Width = 2 * MarkedSystemRadius,
                    Height = 2 * MarkedSystemRadius,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top
                };

                SystemHightlightMap.Children.Add(playerSystemShape);

                playerSystemShape.Margin = new Thickness(playerSystemCoordinate.X - MarkedSystemRadius, playerSystemCoordinate.Y - MarkedSystemRadius, 0, 0);
            }

            // Searched systems
            if (!String.IsNullOrEmpty(HightLightSystemName.Text))
            {
                var highlightedSystemCoordinates = GalacticObjectsRenderer.GetMatchingNameSystemCoordinates(GameState, MarkedSystemsMap.ActualWidth, MarkedSystemsMap.ActualHeight, HightLightSystemName.Text.ToLower());
                var highlightedSystemBrush = Resources["ApplicationForegroundThemeBrush"] as Brush;
                foreach (var highlightedSystemCoordinate in highlightedSystemCoordinates)
                {
                    var highlightedSystemShape = new Ellipse
                    {
                        Stroke = highlightedSystemBrush,
                        StrokeThickness = 2,
                        Width = 2 * MarkedSystemRadius,
                        Height = 2 * MarkedSystemRadius,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top
                    };

                    SystemHightlightMap.Children.Add(highlightedSystemShape);
                    highlightedSystemShape.Margin = new Thickness(highlightedSystemCoordinate.X - MarkedSystemRadius, highlightedSystemCoordinate.Y - MarkedSystemRadius, 0, 0);
                }
            }
        }

        private void UpdateMarkedSystems()
        {
            MarkedSystemsMap.Children.Clear();

            if (MarkSystemFlags.SelectedItem != null)
            {
                var markedFlags = MarkSystemFlags.SelectedItems.Select(i => (i as ListBoxItem)?.Content as string);
                var markedSystemCoordinates = GalacticObjectsRenderer.GetMarkedSystemCoordinates(GameState, MarkedSystemsMap.ActualWidth, MarkedSystemsMap.ActualHeight, markedFlags);
                var markedSystemBrush = new SolidColorBrush(Colors.DarkTurquoise);
                foreach (var markedSystemCoordinate in markedSystemCoordinates)
                {
                    var markedSystemShape = new Ellipse
                    {
                        Stroke = markedSystemBrush,
                        StrokeThickness = 2,
                        Width = 2 * MarkedSystemRadius,
                        Height = 2 * MarkedSystemRadius,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top
                    };

                    MarkedSystemsMap.Children.Add(markedSystemShape);
                    markedSystemShape.Margin = new Thickness(markedSystemCoordinate.X - MarkedSystemRadius, markedSystemCoordinate.Y - MarkedSystemRadius, 0, 0);
                }
            }
        }

        private void UpdateGameStateRawView()
        {
            if (GameStateRawAttributes?.Items == null || GameStateRawSections?.Items == null)
                return;

            GameStateRawAttributes.Items.Clear();
            GameStateRawSections.Items.Clear();

            GameStateRawHelpers.PopulateGameStateRawAttributes(GameStateRawAttributes,
                GameState.GameStateRaw.RootSection);
            GameStateRawHelpers.PopulateGameStateRawSections(GameStateRawSections,
                GameState.GameStateRaw.RootSection);
        }

        private void UpdateGameStateRawSectionChildList(GameStateRawSection selectedSection)
        {
            if (GameStateRawSectionChildList?.Items == null || selectedSection == null)
                return;

            GameStateRawSectionChildList.Items.Clear();

            GameStateRawHelpers.PopulateGameStateRawSectionDetails(GameStateRawSectionChildList, selectedSection);
        }

        private void UpdateGameStateRawSectionDetails(GameStateRawSection selectedSection)
        {
            GameStateRawSectionDetails.RootNodes.Clear();

            if (selectedSection == null)
                return;

            var rootNode = new TreeViewNode { IsExpanded = true };
            GameStateRawSectionDetails.RootNodes.Add(rootNode);

            GameStateRawHelpers.PopulateGameStateRawSectionDetails(rootNode, selectedSection);
        }

        private void UnloadMap()
        {
            SystemMap.Children.Clear();
            HyperLaneMap.Children.Clear();
            SystemHightlightMap.Children.Clear();
            MarkedSystemsMap.Children.Clear();
        }

        private void UnloadGameState()
        {
            FileNameLabel.Text = "";
            VersionLabel.Text = "";
            SaveNameLabel.Text = "";
            FilterPanel.Visibility = Visibility.Collapsed;
            MarkSystemFlags?.Items?.Clear();
            UnloadMap();
            GameStateRawAttributes?.Items?.Clear();
            GameStateRawSections?.Items?.Clear();
            GameStateRawSectionChildList?.Items?.Clear();
            GameStateRawSectionDetails.RootNodes.Clear();
            GameState = null;
        }
    }
}
