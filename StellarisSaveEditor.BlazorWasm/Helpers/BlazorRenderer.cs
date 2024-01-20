using Blazor.Extensions.Canvas.Canvas2D;
using StellarisSaveEditor.Models;

namespace StellarisSaveEditor.BlazorWasm.Helpers
{
    public static class BlazorRenderer
    {
        private struct Point
        {
            public Point(double x, double y)
            {
                X = x; Y = y;
            }
            public double X { get; set; }
            public double Y { get; set; }
        };

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

        public static async Task Render(Canvas2DContext context, double mapWidth, double mapHeight, GameState gameState, IEnumerable<string> markedFlags, string? searchSystemName)
        {
            await context.SetFillStyleAsync("black");
            await context.FillRectAsync(0, 0, mapWidth, mapHeight);

            var mapSettings = GetMapSettings(gameState, mapWidth, mapHeight);
            await RenderSystems(context, gameState, mapSettings);
            await RenderHyperLanes(context, gameState, mapSettings);
            await RenderWormholes(context, gameState, mapSettings);
            await RenderPlayerSystem(context, gameState, mapSettings);
            await RenderMarkedSystemCoordinates(context, gameState, mapSettings, markedFlags);
            await RenderMatchingNameSystemCoordinates(context, gameState, mapSettings, searchSystemName);
        }

        private static async Task RenderSystems(Canvas2DContext context, GameState gameState, MapSettings mapSettings, int objectWidth = 2)
        {
            await context.BeginBatchAsync();
            await context.SetFillStyleAsync("white");
            foreach (var galacticObject in gameState.GalacticObjects.Values)
            {
                var c = GetModifiedCoordinate(mapSettings, galacticObject.Coordinate);
                await context.FillRectAsync(c.X, c.Y, objectWidth, objectWidth);
            }
            await context.EndBatchAsync();
        }

        private static async Task RenderHyperLanes(Canvas2DContext context, GameState gameState, MapSettings mapSettings)
        {
            await context.BeginBatchAsync();
            await context.BeginPathAsync();
            await context.SetStrokeStyleAsync("gray");
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
                    await context.MoveToAsync(p1.X, p1.Y);
                    await context.LineToAsync(p2.X, p2.Y);
                }
            }
            await context.StrokeAsync();
            await context.EndBatchAsync();
        }

        private static async Task RenderWormholes(Canvas2DContext context, GameState gameState, MapSettings mapSettings, double objectRadius = 5.0)
        {
            await context.BeginBatchAsync();
            await context.BeginPathAsync();
            await context.SetStrokeStyleAsync("orange");
            foreach (var wormhole in gameState.NaturalWormholes.Values)
            {
                var wormholeGalacticObject = gameState.GalacticObjects[(int)wormhole.Coordinate.Origin];
                var c = GetModifiedCoordinate(mapSettings, wormholeGalacticObject.Coordinate);
                await context.MoveToAsync(c.X, c.Y);
                await context.ArcAsync(c.X, c.Y, objectRadius, 0, 360);
            }
            await context.StrokeAsync();
            await context.EndBatchAsync();
            await RenderWormholeConnections(context, gameState, mapSettings);
        }

        private static async Task RenderWormholeConnections(Canvas2DContext context, GameState gameState, MapSettings mapSettings)
        {
            await context.BeginBatchAsync();
            await context.BeginPathAsync();
            await context.SetStrokeStyleAsync("yellow");
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
                await context.MoveToAsync(p1.X, p1.Y);
                await context.LineToAsync(p2.X, p2.Y);
            }
            await context.StrokeAsync();
            await context.EndBatchAsync();
        }

        private static async Task RenderPlayerSystem(Canvas2DContext context, GameState gameState, MapSettings mapSettings, double objectRadius = 5.0)
        {
            var playerSystem = gameState.GalacticObjects[gameState.Countries[gameState.Player.CountryIndex].StartingSystemIndex];
            var c = GetModifiedCoordinate(mapSettings, playerSystem.Coordinate);
            await context.BeginBatchAsync();
            await context.BeginPathAsync();
            await context.SetStrokeStyleAsync("white");
            await context.SetLineWidthAsync(2);
            await context.MoveToAsync(c.X, c.Y);
            await context.ArcAsync(c.X, c.Y, objectRadius, 0, 360);
            await context.StrokeAsync();
            await context.EndBatchAsync();
        }

        private static async Task RenderMarkedSystemCoordinates(Canvas2DContext context, GameState gameState, MapSettings mapSettings, IEnumerable<string> markedFlags, double objectRadius = 5.0)
        {
            if (markedFlags == null || !markedFlags.Any())
                return;

            var markedSystemCoordinates = new List<Point>();
            var markedSystems = gameState.GalacticObjects.Values.Where(o => o.GalacticObjectFlags.Any(markedFlags.Contains));
            await context.BeginBatchAsync();
            await context.BeginPathAsync();
            await context.SetStrokeStyleAsync("lightgray");
            await context.SetLineWidthAsync(2);
            foreach (var markedSystem in markedSystems)
            {
                var c = GetModifiedCoordinate(mapSettings, markedSystem.Coordinate);
                await context.MoveToAsync(c.X, c.Y);
                await context.ArcAsync(c.X, c.Y, objectRadius, 0, 360);
            }
            await context.StrokeAsync();
            await context.EndBatchAsync();
        }

        private static async Task RenderMatchingNameSystemCoordinates(Canvas2DContext context, GameState gameState, MapSettings mapSettings, string? searchSystemName, double objectRadius = 5.0)
        {
            if (searchSystemName == null)
                return;

            var matchingNameSystemCoordinates = new List<Point>();
            var matchingNameSystems = gameState.GalacticObjects.Values.Where(o => o.Name.Key.ToLower().StartsWith(searchSystemName));
            await context.BeginBatchAsync();
            await context.BeginPathAsync();
            await context.SetStrokeStyleAsync("lightgray");
            await context.SetLineWidthAsync(2);
            foreach (var matchingNameSystem in matchingNameSystems)
            {
                var c = GetModifiedCoordinate(mapSettings, matchingNameSystem.Coordinate);
                await context.MoveToAsync(c.X, c.Y);
                await context.ArcAsync(c.X, c.Y, objectRadius, 0, 360);
            }
            await context.StrokeAsync();
            await context.EndBatchAsync();
        }

        // Shared helper methods

        private static Point GetModifiedCoordinate(MapSettings mapSettings, Coordinate coordinate)
        {
            return new Point(
                mapSettings.MapWidth - ((coordinate.X - mapSettings.MinX) * mapSettings.ModifierX), // Flip X-coord to correspond to in-game coordinate system
                (coordinate.Y - mapSettings.MinY) * mapSettings.ModifierY
                );
        }

        private static MapSettings GetMapSettings(GameState gameState, double mapWidth, double mapHeight, double padding = 10.0)
        {
            var settings = new MapSettings
            {
                MapWidth = mapWidth,
                MapHeight = mapHeight,
                MinX = gameState.GalacticObjects.Values.Min(o => o.Coordinate.X) - padding,
                MinY = gameState.GalacticObjects.Values.Min(o => o.Coordinate.Y) - padding,
                MaxX = gameState.GalacticObjects.Values.Max(o => o.Coordinate.X) + padding,
                MaxY = gameState.GalacticObjects.Values.Max(o => o.Coordinate.Y + padding)
            };
            settings.ModifierX = settings.MapWidth / (settings.MaxX - settings.MinX);
            settings.ModifierY = settings.MapHeight / (settings.MaxY - settings.MinY);
            return settings;
        }
    }
}
