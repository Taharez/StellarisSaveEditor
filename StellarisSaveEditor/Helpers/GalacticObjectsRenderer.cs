using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using StellarisSaveEditor.Models;
using System.Text;

namespace StellarisSaveEditor.Helpers
{
    public static class GalacticObjectsRenderer
    {
        private struct MapSettings
        {
            public double MapPixelWidth { get; set; }
            public double MapPixelHeight { get; set; }
            public double MinX { get; set; }
            public double MinY { get; set; }
            public double MaxX { get; set; }
            public double MaxY { get; set; }
            public double ModifierX { get; set; }
            public double ModifierY { get; set; }
        }

        public static string RenderAsSvg(GameState gameState, double mapWidth, double mapHeight, double defaultObjectRadius = 2.0)
        {
            const int EstimatedCharactersPerSystem = 60;

            var svg = new StringBuilder(gameState.GalacticObjects.Count * EstimatedCharactersPerSystem);
            var mapSettings = GetMapSettings(gameState, mapWidth, mapHeight);
            foreach (var galacticObject in gameState.GalacticObjects)
            {
                double objectRadius = defaultObjectRadius;
                var c = GetModifiedCoordinate(mapSettings, galacticObject.Coordinate);
                svg.Append("M" + (c.X - objectRadius) + "," + (c.Y - objectRadius) + 
                    "L" + (c.X + objectRadius) + "," + (c.Y - objectRadius) + 
                    "L" + (c.X + objectRadius) + "," + (c.Y + objectRadius) + 
                    "L" + (c.X - objectRadius) + "," + (c.Y + objectRadius) + "z");
            }

            return svg.ToString();
        }

        public static List<Tuple<Point, Point>> RenderHyperLanesAsLineList(GameState gameState, double mapWidth, double mapHeight)
        {
            var lineList = new List<Tuple<Point, Point>>();
            var mapSettings = GetMapSettings(gameState, mapWidth, mapHeight);
            foreach (var galacticObject in gameState.GalacticObjects)
            {
                foreach (var hyperLane in galacticObject.HyperLanes)
                {
                    var target = gameState.GalacticObjects[hyperLane.ToGalacticObjectIndex];
                    var p1 = GetModifiedCoordinate(mapSettings, galacticObject.Coordinate);
                    var p2 = GetModifiedCoordinate(mapSettings, target.Coordinate);
                    lineList.Add(new Tuple<Point, Point>(p1, p2));
                }
            }
            return lineList;
        }

        public static Point GetPlayerSystemCoordinates(GameState gameState, double mapWidth, double mapHeight)
        {
            var playerSystem = gameState.GalacticObjects[gameState.Countries[gameState.Player.CountryIndex].StartingSystemIndex];
            var mapSettings = GetMapSettings(gameState, mapWidth, mapHeight);
            return GetModifiedCoordinate(mapSettings, playerSystem.Coordinate);
        }

        public static List<Point> GetMarkedSystemCoordinates(GameState gameState, double mapWidth, double mapHeight, IEnumerable<string> markedFlags)
        {
            var markedSystemCoordinates = new List<Point>();
            var markedSystems = gameState.GalacticObjects.Where(o => o.GalacticObjectFlags.Any(f => markedFlags.Contains(f)));
            var mapSettings = GetMapSettings(gameState, mapWidth, mapHeight);
            foreach (var markedSystem in markedSystems)
            {
                markedSystemCoordinates.Add(GetModifiedCoordinate(mapSettings, markedSystem.Coordinate));
            }
            return markedSystemCoordinates;
        }

        private static Point GetModifiedCoordinate(MapSettings mapSettings, GalacticObjectCoordinate coordinate)
        {
            return new Point(
                mapSettings.MapPixelWidth - ((coordinate.X - mapSettings.MinX) * mapSettings.ModifierX), // Flip X-coord to correspond to in-game coordinate system
                (coordinate.Y - mapSettings.MinY) * mapSettings.ModifierY
                );
        }

        private static MapSettings GetMapSettings(GameState gameState, double mapWidth, double mapHeight)
        {
            var settings = new MapSettings
            {
                MapPixelWidth = mapWidth,
                MapPixelHeight = mapHeight,
                MinX = gameState.GalacticObjects.Min(o => o.Coordinate.X),
                MinY = gameState.GalacticObjects.Min(o => o.Coordinate.Y),
                MaxX = gameState.GalacticObjects.Max(o => o.Coordinate.X),
                MaxY = gameState.GalacticObjects.Max(o => o.Coordinate.Y)
            };
            settings.ModifierX = mapWidth / (settings.MaxX - settings.MinX);
            settings.ModifierY = mapHeight / (settings.MaxY - settings.MinY);
            return settings;
        }
    }
}
