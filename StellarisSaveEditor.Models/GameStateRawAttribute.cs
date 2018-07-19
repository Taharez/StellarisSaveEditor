using StellarisSaveEditor.Enums;

namespace StellarisSaveEditor.Models
{
    public class GameStateRawAttribute
    {
        public GameStateRawSection Parent { get; set; }

        public string Name { get; set; }

        public string Value { get; set; }
    }
}
