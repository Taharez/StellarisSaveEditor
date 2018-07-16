using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using StellarisSaveEditor.Models;
using System.Text;
using StellarisSaveEditor.Enums;

namespace StellarisSaveEditor.Helpers
{
    public static class GalacticObjectsRenderer
    {
        private struct MapSettings
        {
            public double MinX { get; set; }
            public double MinY { get; set; }
            public double MaxX { get; set; }
            public double MaxY { get; set; }
            public double ModifierX { get; set; }
            public double ModifierY { get; set; }
        }

        public static byte[] RenderAsBitmap(GameState gameState, int mapPixelWidth, int mapPixelHeight)
        {
            // Uses BGRA format which is 4 bytes per pixel.
            byte[] imageArray = new byte[mapPixelHeight * mapPixelWidth * 4];

            // Write black background
            for (int i = 0; i < imageArray.Length; i += 4)
            {
                imageArray[i] = 0; // Blue
                imageArray[i + 1] = 0;  // Green
                imageArray[i + 2] = 0; // Red
                imageArray[i + 3] = 255; // Alpha
            }

            var mapSettings = GetMapSettings(gameState, mapPixelWidth, mapPixelHeight);
            foreach (var galacticObject in gameState.GalacticObjects)
            {
                var modifiedX = Math.Min((galacticObject.Coordinate.X - mapSettings.MinX) / mapSettings.ModifierX, mapPixelWidth - 1);
                var modifiedY = Math.Min((galacticObject.Coordinate.Y - mapSettings.MinY) / mapSettings.ModifierY, mapPixelHeight - 1);
                var pixelIndex = ((int)modifiedX + (int)modifiedY * mapPixelWidth) * 4;
                imageArray[pixelIndex] = 232;
                imageArray[pixelIndex + 1] = 128;
                imageArray[pixelIndex + 2] = 128;
            }

            return imageArray;
        }

        public static string RenderAsSvg(GameState gameState, int mapPixelWidth, int mapPixelHeight, double defaultObjectRadius = 2.0)
        {
            const int EstimatedCharactersPerSystem = 60;

            var svg = new StringBuilder(gameState.GalacticObjects.Count * EstimatedCharactersPerSystem);
            var mapSettings = GetMapSettings(gameState, mapPixelWidth, mapPixelHeight);
            foreach (var galacticObject in gameState.GalacticObjects)
            {
                double objectRadius = defaultObjectRadius;
                var cX = (galacticObject.Coordinate.X - mapSettings.MinX) * mapSettings.ModifierX;
                var cY = (galacticObject.Coordinate.Y - mapSettings.MinY) * mapSettings.ModifierY;
                svg.Append("M" + (cX - objectRadius) + "," + (cY - objectRadius) + 
                    "L" + (cX + objectRadius) + "," + (cY - objectRadius) + 
                    "L" + (cX + objectRadius) + "," + (cY + objectRadius) + 
                    "L" + (cX - objectRadius) + "," + (cY + objectRadius) + "z");
            }

            return svg.ToString();
        }

        public static List<Tuple<Point, Point>> RenderHyperLanesAsLineList(GameState gameState, int mapPixelWidth, int mapPixelHeight)
        {
            var lineList = new List<Tuple<Point, Point>>();
            var mapSettings = GetMapSettings(gameState, mapPixelWidth, mapPixelHeight);
            foreach (var galacticObject in gameState.GalacticObjects)
            {
                foreach (var hyperLane in galacticObject.HyperLanes)
                {
                    var target = gameState.GalacticObjects[hyperLane.ToGalacticObjectIndex];
                    var p1 = new Point((galacticObject.Coordinate.X - mapSettings.MinX) * mapSettings.ModifierX, (galacticObject.Coordinate.Y - mapSettings.MinY) * mapSettings.ModifierY);
                    var p2 = new Point((target.Coordinate.X - mapSettings.MinX) * mapSettings.ModifierX, (target.Coordinate.Y - mapSettings.MinY) * mapSettings.ModifierY);
                    lineList.Add(new Tuple<Point, Point>(p1, p2));
                }
            }
            return lineList;
        }

        public static Point GetPlayerSystemCoordinates(GameState gameState, int mapPixelWidth, int mapPixelHeight)
        {
            var playerSystem = gameState.GalacticObjects[gameState.Countries[gameState.Player.CountryIndex].StartingSystemIndex];
            var mapSettings = GetMapSettings(gameState, mapPixelWidth, mapPixelHeight);
            return new Point((playerSystem.Coordinate.X - mapSettings.MinX) * mapSettings.ModifierX, (playerSystem.Coordinate.Y - mapSettings.MinY) * mapSettings.ModifierY);
        }

        public static List<Point> GetMarkedSystemCoordinates(GameState gameState, int mapPixelWidth, int mapPixelHeight, IEnumerable<string> markedFlags)
        {
            var markedSystemFlags = markedFlags.Select(f => Enum.Parse(typeof(GalacticObjectFlag), f));
            var markedSystemCoordinates = new List<Point>();
            var markedSystems = gameState.GalacticObjects.Where(o => o.GalacticObjectFlags.Any(f => markedSystemFlags.Contains(f)));
            var mapSettings = GetMapSettings(gameState, mapPixelWidth, mapPixelHeight);
            foreach (var markedSystem in markedSystems)
            {
                markedSystemCoordinates.Add(new Point((markedSystem.Coordinate.X - mapSettings.MinX) * mapSettings.ModifierX, (markedSystem.Coordinate.Y - mapSettings.MinY) * mapSettings.ModifierY));
            }
            return markedSystemCoordinates;
        }

        private static MapSettings GetMapSettings(GameState gameState, int mapPixelWidth, int mapPixelHeight)
        {
            var settings = new MapSettings();
            settings.MinX = gameState.GalacticObjects.Min(o => o.Coordinate.X);
            settings.MinY = gameState.GalacticObjects.Min(o => o.Coordinate.Y);
            settings.MaxX = gameState.GalacticObjects.Max(o => o.Coordinate.X);
            settings.MaxY = gameState.GalacticObjects.Max(o => o.Coordinate.Y);
            settings.ModifierX = mapPixelWidth / (settings.MaxX - settings.MinX);
            settings.ModifierY = mapPixelHeight / (settings.MaxY - settings.MinY);
            return settings;
        }
    }
}
