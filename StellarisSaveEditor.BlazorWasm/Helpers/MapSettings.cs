using StellarisSaveEditor.Models;

namespace StellarisSaveEditor.BlazorWasm.Helpers
{
    public class MapSettings
    {
        public double MapWidth { get; set; }
        public double MapHeight { get; set; }
        public double MinX { get; set; }
        public double MinY { get; set; }
        public double MaxX { get; set; }
        public double MaxY { get; set; }
        public double ModifierX { get; set; }
        public double ModifierY { get; set; }

        public bool ShowHyperLanes { get; set; }
        public bool ShowHomeSystem { get; set; }
        public bool ShowWormholes { get; set; }
        public bool ShowWormholeConnections { get; set; }
        public bool ShowGatewayConnections { get; set; }
        public bool ShowLgateConnections { get; set; }

        public Point GetModifiedCoordinate(Coordinate coordinate)
        {
            return new Point(
            MapWidth - ((coordinate.X - MinX) * ModifierX), // Flip X-coord to correspond to in-game coordinate system
            (coordinate.Y - MinY) * ModifierY
            );
        }

        public void Init(GameState gameState, double mapWidth, double mapHeight, double padding = 10.0)
        {
            MapWidth = mapWidth;
            MapHeight = mapHeight;
            MinX = gameState.GalacticObjects.Values.Min(o => o.Coordinate.X) - padding;
            MinY = gameState.GalacticObjects.Values.Min(o => o.Coordinate.Y) - padding;
            MaxX = gameState.GalacticObjects.Values.Max(o => o.Coordinate.X) + padding;
            MaxY = gameState.GalacticObjects.Values.Max(o => o.Coordinate.Y) + padding;
            ModifierX = MapWidth / (MaxX - MinX);
            ModifierY = MapHeight / (MaxY - MinY);

            ShowHyperLanes = true;
            ShowHomeSystem = true;
            ShowWormholes = true;
            ShowWormholeConnections = true;
            ShowGatewayConnections = true;
            ShowLgateConnections = true;
        }
    }
}
