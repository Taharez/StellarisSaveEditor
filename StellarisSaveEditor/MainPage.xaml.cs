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

        private async void SelectFile_Clicked(object sender, RoutedEventArgs e)
        {
            var saveFile = await LoadSaveFile();
            if (saveFile != null)
            {
                // Application now has read/write access to the picked file
                FileNameLabel.Text = "Opened file: " + saveFile.Name;

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
                }
            }
            else
            {
                FileNameLabel.Text = "Operation cancelled.";
            }
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
                Fill = new SolidColorBrush(Colors.LightBlue),
                Stretch = Stretch.Fill,
                Width = MapCanvas.ActualWidth,
                Height = MapCanvas.ActualHeight,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            MapCanvas.Children.Add(path);
            Canvas.SetLeft(path, 0);
            Canvas.SetTop(path, 0);

            // Hyper lanes
            var hyperLaneBrush = new SolidColorBrush(Colors.AntiqueWhite);
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
                var markedSystemBrush = new SolidColorBrush(Colors.Turquoise);
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
    }
}
