using System.Collections.Generic;

namespace StellarisSaveEditor.Models
{
    public class GameStateRawSection
    {
        public GameStateRawSection()
        {
            Attributes = new List<GameStateRawAttribute>();
            Sections = new List<GameStateRawSection>();
        }

        public GameStateRawSection Parent { get; set; }

        public string Name { get; set; }

        public List<GameStateRawAttribute> Attributes { get; set; }

        public List<GameStateRawSection> Sections { get; set; }
    }
}
