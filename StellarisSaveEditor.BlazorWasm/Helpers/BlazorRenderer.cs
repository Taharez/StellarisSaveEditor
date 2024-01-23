using Blazor.Extensions.Canvas.Canvas2D;
using StellarisSaveEditor.Models;

namespace StellarisSaveEditor.BlazorWasm.Helpers
{
    public class BlazorRenderer
    {
        private readonly Canvas2DContext _context;
        private readonly MapSettings _mapSettings;
        private readonly GameState _gameState;

        public BlazorRenderer(Canvas2DContext context, MapSettings mapSettings, GameState gameState) {
            _context = context;
            _mapSettings = mapSettings;
            _gameState = gameState;
        }

        public async Task Render(IEnumerable<string> markedFlags, string? searchSystemName)
        {
            await _context.SetFillStyleAsync("black");
            await _context.FillRectAsync(0, 0, _mapSettings.MapWidth, _mapSettings.MapHeight);

            await RenderSystems();
            await RenderHyperLanes();
            await RenderWormholes();
            await RenderGateways();
            await RenderLgates();
            await RenderPlayerSystem();
            await RenderMarkedSystemCoordinates(markedFlags);
            await RenderMatchingNameSystemCoordinates(searchSystemName);
        }

        private async Task RenderSystems(int objectWidth = 2)
        {
            await _context.BeginBatchAsync();
            await _context.SetFillStyleAsync("white");
            foreach (var galacticObject in _gameState.GalacticObjects.Values)
            {
                var c = _mapSettings.GetModifiedCoordinate(galacticObject.Coordinate);
                await _context.FillRectAsync(c.X, c.Y, objectWidth, objectWidth);
            }
            await _context.EndBatchAsync();
        }

        private async Task RenderHyperLanes()
        {
            if (!_mapSettings.ShowHyperLanes)
                return;

            await _context.BeginBatchAsync();
            await _context.BeginPathAsync();
            await _context.SetStrokeStyleAsync("gray");
            foreach (var galacticObject in _gameState.GalacticObjects.Values)
            {
                foreach (var hyperLane in galacticObject.HyperLanes)
                {
                    // Only render hyperlane once, since they are defined in two galactic objects choose the one with lowest "from"-id.
                    if (galacticObject.Id >= hyperLane.ToGalacticObjectIndex)
                        continue;

                    var target = _gameState.GalacticObjects[hyperLane.ToGalacticObjectIndex];
                    var p1 = _mapSettings.GetModifiedCoordinate(galacticObject.Coordinate);
                    var p2 = _mapSettings.GetModifiedCoordinate(target.Coordinate);
                    await _context.MoveToAsync(p1.X, p1.Y);
                    await _context.LineToAsync(p2.X, p2.Y);
                }
            }
            await _context.StrokeAsync();
            await _context.EndBatchAsync();
        }

        private async Task RenderBypasses(bool renderConnections, string bypassType, string color, double objectRadius = 5.0)
        {
            await _context.BeginBatchAsync();
            await _context.BeginPathAsync();
            await _context.SetStrokeStyleAsync(color);
            foreach (var bypass in _gameState.Bypasses.Values.Where(bp => bp.BypassType == bypassType))
            {
                var hasObject = _gameState.Indices.GalacticObjectByBypassId.TryGetValue(bypass.Id, out var galacticObject);
                if (!hasObject)
                    continue;

                var p1 = _mapSettings.GetModifiedCoordinate(galacticObject!.Coordinate);
                await _context.MoveToAsync(p1.X, p1.Y);
                await _context.ArcAsync(p1.X, p1.Y, objectRadius, 0, 360);

                if (renderConnections)
                {
                    foreach (var connectedBypassId in bypass.Connections)
                    {
                        var hasConnectedObject = _gameState.Indices.GalacticObjectByBypassId.TryGetValue(connectedBypassId, out var connectedGalacticObject);
                        if (!hasConnectedObject || connectedBypassId > bypass.Id)
                            continue;
                        var p2 = _mapSettings.GetModifiedCoordinate(connectedGalacticObject!.Coordinate);
                        await _context.MoveToAsync(p1.X, p1.Y);
                        await _context.LineToAsync(p2.X, p2.Y);
                    }
                }
            }
            await _context.StrokeAsync();
            await _context.EndBatchAsync();
        }

        private async Task RenderWormholes()
        {
            if (!_mapSettings.ShowWormholes)
                return;

            await RenderBypasses(true, "wormhole", "yellow");
        }

        private async Task RenderGateways()
        {
            if (!_mapSettings.ShowGateways)
                return;

            await RenderBypasses(false, "gateway", "lightblue");
        }

        private async Task RenderLgates()
        {
            if (!_mapSettings.ShowLgates)
                return;

            await RenderBypasses(false, "lgate", "purple");
        }

        private async Task RenderPlayerSystem(double objectRadius = 5.0)
        {
            if (!_mapSettings.ShowHomeSystem)
                return;

            var playerSystem = _gameState.GalacticObjects[_gameState.Countries[_gameState.Player.CountryIndex].StartingSystemIndex];
            var c = _mapSettings.GetModifiedCoordinate(playerSystem.Coordinate);
            await _context.BeginBatchAsync();
            await _context.BeginPathAsync();
            await _context.SetStrokeStyleAsync("white");
            await _context.SetLineWidthAsync(2);
            await _context.MoveToAsync(c.X, c.Y);
            await _context.ArcAsync(c.X, c.Y, objectRadius, 0, 360);
            await _context.StrokeAsync();
            await _context.EndBatchAsync();
        }

        private async Task RenderMarkedSystemCoordinates(IEnumerable<string> markedFlags, double objectRadius = 5.0)
        {
            if (markedFlags == null || !markedFlags.Any())
                return;

            var markedSystemCoordinates = new List<Point>();
            var markedSystems = _gameState.GalacticObjects.Values.Where(o => o.GalacticObjectFlags.Any(markedFlags.Contains));
            await _context.BeginBatchAsync();
            await _context.BeginPathAsync();
            await _context.SetStrokeStyleAsync("lightgray");
            await _context.SetLineWidthAsync(2);
            foreach (var markedSystem in markedSystems)
            {
                var c = _mapSettings.GetModifiedCoordinate(markedSystem.Coordinate);
                await _context.MoveToAsync(c.X, c.Y);
                await _context.ArcAsync(c.X, c.Y, objectRadius, 0, 360);
            }
            await _context.StrokeAsync();
            await _context.EndBatchAsync();
        }

        private async Task RenderMatchingNameSystemCoordinates(string? searchSystemName, double objectRadius = 5.0)
        {
            if (searchSystemName == null)
                return;

            var matchingNameSystemCoordinates = new List<Point>();
            var matchingNameSystems = _gameState.GalacticObjects.Values.Where(o => o.Name.Key.ToLower().StartsWith(searchSystemName));
            await _context.BeginBatchAsync();
            await _context.BeginPathAsync();
            await _context.SetStrokeStyleAsync("lightgray");
            await _context.SetLineWidthAsync(2);
            foreach (var matchingNameSystem in matchingNameSystems)
            {
                var c = _mapSettings.GetModifiedCoordinate(matchingNameSystem.Coordinate);
                await _context.MoveToAsync(c.X, c.Y);
                await _context.ArcAsync(c.X, c.Y, objectRadius, 0, 360);
            }
            await _context.StrokeAsync();
            await _context.EndBatchAsync();
        }
    }
}
