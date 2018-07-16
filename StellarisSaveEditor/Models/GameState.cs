﻿using System.Collections.Generic;

namespace StellarisSaveEditor.Models
{
    public class GameState
    {
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

        public List<Pop> Pops { get; set; }

        public int LastCreatedPopIndex { get; set; }

        public List<GalacticObject> GalacticObjects { get; set; }

        public List<Country> Countries { get; set; }
    }
}
