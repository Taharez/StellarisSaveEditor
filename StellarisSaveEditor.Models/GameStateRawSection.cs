using System.Collections.Generic;

namespace StellarisSaveEditor.Models
{
    public class GameStateRawSection
    {
        public GameStateRawSection()
        {
            Attributes = new List<GameStateRawAttribute>();
            Sections = new List<GameStateRawSection>();
            FromFlattenedList = false;
        }

        public GameStateRawSection Parent { get; set; }

        public string Name { get; set; }

        public List<GameStateRawAttribute> Attributes { get; set; }

        public List<GameStateRawSection> Sections { get; set; }

        // If true, this is a dummy section created to hold first-level objects with non-unique names (such as nebula which are not a list in the gamestate file for some reason).)
        public bool FromFlattenedList { get; set; }
    }
}
