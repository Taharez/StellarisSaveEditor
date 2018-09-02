using System.Collections.Generic;

namespace StellarisSaveEditor.Models
{
    public class GameState
    {
        // Raw file date used to build this game state
        public GameStateRaw GameStateRaw { get; set; }

        // Game state properties parsed from raw game state
        public string Version { get; set; }

        public string VersionControlRevision { get; set; }

        public string Name { get; set; }

        public string Date { get; set; }

        public List<string> RequiredDlcs { get; set; }

        public Player Player { get; set; }

        public int Tick { get; set; }

        public int RandomLogDay { get; set; }

        public List<Species> Species { get; set; }

        public int LastCreatedSpeciesIndex { get; set; }

        public List<Nebula> Nebula { get; set; }

        public Dictionary<int, Pop> Pops { get; set; }

        public int LastCreatedPopIndex { get; set; }

        public Dictionary<int, GalacticObject> GalacticObjects { get; set; }

        public Dictionary<int, Country> Countries { get; set; }

        public Dictionary<int, Bypass> Bypasses { get; set; }

        public Dictionary<int, NaturalWormhole> NaturalWormholes { get; set; }
    }
}
