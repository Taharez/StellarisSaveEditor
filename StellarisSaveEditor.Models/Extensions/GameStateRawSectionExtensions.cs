using StellarisSaveEditor.Models;
using System.Collections.Generic;
using System.Linq;

namespace StellarisSaveEditor.Models.Extensions
{
    public static class GameStateRawSectionExtensions
    {
        public static GameStateRawSection GetChildSectionByName(this GameStateRawSection gameStateRawSection, string childSectionName)
        {
            return gameStateRawSection.Sections.FirstOrDefault(s => s.Name == childSectionName);
        }

        public static GameStateRawAttribute GetAttributeByName(this GameStateRawSection gameStateRawSection, string attributeName)
        {
            return gameStateRawSection.Attributes.FirstOrDefault(s => s.Name == attributeName);
        }

        public static List<GameStateRawAttribute> GetAttributesByName(this GameStateRawSection gameStateRawSection, string attributeName)
        {
            return gameStateRawSection.Attributes.Where(s => s.Name == attributeName).ToList();
        }

        public static string GetAttributeValueByName(this GameStateRawSection gameStateRawSection, string attributeName)
        {
            return gameStateRawSection.GetAttributeByName(attributeName).Value;
        }

        public static List<string> GetAttributeValuesByName(this GameStateRawSection gameStateRawSection, string attributeName)
        {
            return gameStateRawSection.GetAttributesByName(attributeName).Select(a => a.Value).ToList();
        }
    }
}
