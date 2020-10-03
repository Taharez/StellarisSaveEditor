﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StellarisSaveEditor.Models;

namespace StellarisSaveEditor.Renderer
{
    public class GalacticObjectsRenderer
    {
        private struct MapSettings
        {
            public double MapWidth { get; set; }
            public double MapHeight { get; set; }
            public double MinX { get; set; }
            public double MinY { get; set; }
            public double MaxX { get; set; }
            public double MaxY { get; set; }
            public double ModifierX { get; set; }
            public double ModifierY { get; set; }
        }

        public struct Point
        {
            public double X { get; set; }
            public double Y { get; set; }

            public Point(double x, double y)
            {
                X = x;
                Y = y;
            }
        }

        public static string RenderAsSvg(GameState gameState, double mapWidth, double mapHeight, double defaultObjectRadius = 2.0)
        {
            const int estimatedCharactersPerSystem = 60;

            var svg = new StringBuilder(gameState.GalacticObjects.Count * estimatedCharactersPerSystem);
            var mapSettings = GetMapSettings(gameState, mapWidth, mapHeight);
            foreach (var galacticObject in gameState.GalacticObjects.Values)
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
            foreach (var galacticObject in gameState.GalacticObjects.Values)
            {
                foreach (var hyperLane in galacticObject.HyperLanes)
                {
                    // Only render hyperlane once, since they are defined in two galactic objects choose the one with lowest "from"-id.
                    if (galacticObject.Id >= hyperLane.ToGalacticObjectIndex)
                        continue;

                    var target = gameState.GalacticObjects[hyperLane.ToGalacticObjectIndex];
                    var p1 = GetModifiedCoordinate(mapSettings, galacticObject.Coordinate);
                    var p2 = GetModifiedCoordinate(mapSettings, target.Coordinate);
                    lineList.Add(new Tuple<Point, Point>(p1, p2));
                }
            }
            return lineList;
        }

        public static List<Point> GetWormholeSystemCoordinates(GameState gameState, double mapWidth, double mapHeight)
        {
            var wormholeSystemCoordinates = new List<Point>();
            var mapSettings = GetMapSettings(gameState, mapWidth, mapHeight);
            foreach (var wormhole in gameState.NaturalWormholes.Values)
            {
                var wormholeGalacticObject = gameState.GalacticObjects[(int)wormhole.Coordinate.Origin];
                wormholeSystemCoordinates.Add(GetModifiedCoordinate(mapSettings, wormholeGalacticObject.Coordinate));
            }

            return wormholeSystemCoordinates;
        }

        public static List<Tuple<Point, Point>> RenderWormholeConnectionsAsLineList(GameState gameState, double mapWidth, double mapHeight)
        {
            var lineList = new List<Tuple<Point, Point>>();
            var mapSettings = GetMapSettings(gameState, mapWidth, mapHeight);
            foreach (var wormholeBypass in gameState.Bypasses.Values.Where(bp => bp.BypassType == "wormhole"))
            {
                // Only render wormhole connection once
                if (!(wormholeBypass.Id < wormholeBypass.LinkedToBypassId))
                    continue;

                var startingWormhole = gameState.NaturalWormholes.Values.FirstOrDefault(wh => wh.BypassId == wormholeBypass.Id);
                var targetWormhole = gameState.NaturalWormholes.Values.FirstOrDefault(wh => wh.BypassId == wormholeBypass.LinkedToBypassId);
                if (startingWormhole == null || targetWormhole == null)
                    continue;

                var startingGalacticObject = gameState.GalacticObjects[(int)startingWormhole.Coordinate.Origin];
                var targetGalacticObject = gameState.GalacticObjects[(int)targetWormhole.Coordinate.Origin];
                var p1 = GetModifiedCoordinate(mapSettings, startingGalacticObject.Coordinate);
                var p2 = GetModifiedCoordinate(mapSettings, targetGalacticObject.Coordinate);
                lineList.Add(new Tuple<Point, Point>(p1, p2));
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
            var markedSystems = gameState.GalacticObjects.Values.Where(o => o.GalacticObjectFlags.Any(markedFlags.Contains));
            var mapSettings = GetMapSettings(gameState, mapWidth, mapHeight);
            foreach (var markedSystem in markedSystems)
            {
                markedSystemCoordinates.Add(GetModifiedCoordinate(mapSettings, markedSystem.Coordinate));
            }
            return markedSystemCoordinates;
        }

        public static List<Point> GetMatchingNameSystemCoordinates(GameState gameState, double mapWidth, double mapHeight, string name)
        {
            var matchingNameSystemCoordinates = new List<Point>();
            var matchingNameSystems = gameState.GalacticObjects.Values.Where(o => o.Name.ToLower().StartsWith(name));
            var mapSettings = GetMapSettings(gameState, mapWidth, mapHeight);
            foreach (var matchingNameSystem in matchingNameSystems)
            {
                matchingNameSystemCoordinates.Add(GetModifiedCoordinate(mapSettings, matchingNameSystem.Coordinate));
            }
            return matchingNameSystemCoordinates;
        }



        // Shared helper methods

        private static Point GetModifiedCoordinate(MapSettings mapSettings, Coordinate coordinate)
        {
            return new Point(
                mapSettings.MapWidth - ((coordinate.X - mapSettings.MinX) * mapSettings.ModifierX), // Flip X-coord to correspond to in-game coordinate system
                (coordinate.Y - mapSettings.MinY) * mapSettings.ModifierY
                );
        }

        private static MapSettings GetMapSettings(GameState gameState, double mapWidth, double mapHeight)
        {
            var settings = new MapSettings
            {
                MapWidth = mapWidth,
                MapHeight = mapHeight,
                MinX = gameState.GalacticObjects.Values.Min(o => o.Coordinate.X),
                MinY = gameState.GalacticObjects.Values.Min(o => o.Coordinate.Y),
                MaxX = gameState.GalacticObjects.Values.Max(o => o.Coordinate.X),
                MaxY = gameState.GalacticObjects.Values.Max(o => o.Coordinate.Y)
            };
            settings.ModifierX = settings.MapWidth / (settings.MaxX - settings.MinX);
            settings.ModifierY = settings.MapHeight / (settings.MaxY - settings.MinY);
            return settings;
        }
    }
}
