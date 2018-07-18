using System.Collections.Generic;
using StellarisSaveEditor.Models;
using StellarisSaveEditor.Extensions;
using System;
using System.Linq;
using StellarisSaveEditor.Enums;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace StellarisSaveEditor.Helpers
{
    public static class GameStateParser
    {
        public static GameState ParseGamestate(List<string> gameStateText)
        {
            var gameStateRaw = new GameStateRaw();
            var gameState = new GameState() { GameStateRaw = gameStateRaw };

            ParseGamestateRaw(gameStateRaw, gameStateText);
            
            ParseGamestateCommon(gameState);

            ParseGamestateGalacticObjects(gameState);

            ParseGamestateCountries(gameState);

            return gameState;
        }

        private static void ParseGamestateRaw(GameStateRaw gameStateRaw, List<string> gameStateText)
        {
            gameStateRaw.RootSection = new GameStateRawSection() // Container for all top-level sections and attributes
            {
                Name = "Root"
            };
            var currentSection = gameStateRaw.RootSection;
            for (int i = 0; i < gameStateText.Count; ++i)
            {
                var currentLine = gameStateText[i].Trim();
                if (String.IsNullOrWhiteSpace(currentLine))
                {
                    // Skip blank lines
                    continue;
                }
                else if (currentLine.Contains("={"))
                {
                    // Named section start
                    var sectionName = currentLine.Substring(0, currentLine.IndexOf("={"));
                    var section = new GameStateRawSection
                    {
                        Parent = currentSection,
                        Name = sectionName
                    };
                    currentSection.Sections.Add(section);
                    // Check for single-line section/list
                    if (currentLine.Contains("}"))
                    {
                        var valueStartIndex = currentLine.IndexOf("={") + 2;
                        var valueEndIndex = currentLine.IndexOf("}") - 1;
                        var attributeValue = currentLine.Substring(valueStartIndex, valueEndIndex - valueStartIndex).Trim();
                        var attribute = new GameStateRawAttribute
                        {
                            Parent = currentSection,
                            Name = null,
                            Value = attributeValue
                        };
                        section.Attributes.Add(attribute);
                    }
                    else
                    {
                        // Start of new section, update current section until end of scope
                        currentSection = section;
                    }
                }
                else if (currentLine.Contains("{"))
                {
                    // Unnamed section start
                    var section = new GameStateRawSection
                    {
                        Parent = currentSection,
                        Name = null
                    };
                    currentSection.Sections.Add(section);
                    currentSection = section;
                }
                else if (currentLine.Contains("}"))
                {
                    // Section close
                    if (currentSection.Parent == null)
                    {
                        Debug.Assert(i == gameStateText.Count - 1);
                    }
                    else
                    {
                        currentSection = currentSection.Parent;
                    }                            
                }
                else if (currentLine.Contains("="))
                {
                    // Attribute
                    var attributeName = currentLine.Substring(0, currentLine.IndexOf("="));
                    var attributeValue = currentLine.Substring(currentLine.IndexOf("=") + 1).Trim('\"');
                    var attribute = new GameStateRawAttribute
                    {
                        Parent = currentSection,
                        Name = attributeName,
                        Value = attributeValue
                    };
                    currentSection.Attributes.Add(attribute);
                }
                else
                {
                    // Unnamed attribute/list item
                    var attributeValue = currentLine;
                    var attribute = new GameStateRawAttribute
                    {
                        Parent = currentSection,
                        Name = null,
                        Value = attributeValue
                    };
                    currentSection.Attributes.Add(attribute);
                }
            }

            // Post-process, since some first-level sections are actually list items (identical names)
            var groupedSections = gameStateRaw.RootSection.Sections.GroupBy(s => s.Name).Where(g => g.Count() > 1);
            foreach (var groupedSection in groupedSections)
            {
                // Create new first-level sections to hold list items
                var section = new GameStateRawSection
                {
                    Parent = gameStateRaw.RootSection,
                    Name = groupedSection.Key,
                    FromFlattenedList = true
                };
                gameStateRaw.RootSection.Sections.Add(section);
                foreach(var sectionToMove in groupedSection)
                {
                    sectionToMove.Parent = section;
                    gameStateRaw.RootSection.Sections.Remove(sectionToMove);
                    section.Sections.Add(sectionToMove);
                }
            }
        }

        private static void ParseGamestateCommon(GameState gameState)
        {
            var gameStateRaw = gameState.GameStateRaw;
            
            gameState.Version = gameStateRaw.RootSection.GetAttributeValueByName("version");
            gameState.VersionControlRevision = gameStateRaw.RootSection.GetAttributeValueByName("version_control_revision");
            gameState.Name = gameStateRaw.RootSection.GetAttributeValueByName("name");

            gameState.Player = new Player();
            var galacticObjectSection = gameStateRaw.RootSection.GetChildSectionByName("player");
            gameState.Player.Name = galacticObjectSection.Sections.First().GetAttributeValueByName("name");
            int playerCountryIndex;
            int.TryParse(galacticObjectSection.Sections.First().GetAttributeValueByName("country"), out playerCountryIndex);
            gameState.Player.CountryIndex = playerCountryIndex;
        }

        private static void ParseGamestateGalacticObjects(GameState gameState)
        {
            var gameStateRaw = gameState.GameStateRaw;

            gameState.GalacticObjects = new List<GalacticObject>();
            var galacticObjectSection = gameStateRaw.RootSection.GetChildSectionByName("galactic_object");
            foreach (var galacticObjectItem in galacticObjectSection.Sections)
            {
                var galacticObject = new GalacticObject();

                // Coordinate
                galacticObject.Coordinate = new GalacticObjectCoordinate();
                var coordinateSection = galacticObjectItem.GetChildSectionByName("coordinate");
                double x;
                double y;
                long origin;
                string randomized;
                double.TryParse(coordinateSection.GetAttributeValueByName("x"), out x);
                double.TryParse(coordinateSection.GetAttributeValueByName("y"), out y);
                long.TryParse(coordinateSection.GetAttributeValueByName("origin"), out origin);
                randomized = coordinateSection.GetAttributeValueByName("randomized");
                galacticObject.Coordinate.X = x;
                galacticObject.Coordinate.Y = y;
                galacticObject.Coordinate.Origin = origin;
                galacticObject.Coordinate.Randomized = randomized.Equals("yes");

                // Type and name
                GalacticObjectType type;
                Enum.TryParse(galacticObjectItem.GetAttributeValueByName("type"), out type);
                galacticObject.Type = type;
                galacticObject.Name = galacticObjectItem.GetAttributeValueByName("name");

                // Planets 
                galacticObject.PlanetIndices = new List<int>();
                var planetAttributes = galacticObjectItem.GetAttributeValuesByName("planet");
                foreach (var planetAttribute in planetAttributes)
                {
                    int planetIndex;
                    int.TryParse(planetAttribute, out planetIndex);
                    galacticObject.PlanetIndices.Add(planetIndex);
                }

                // Star class
                StarClass starClass;
                Enum.TryParse(galacticObjectItem.GetAttributeValueByName("star_class"), out starClass);
                galacticObject.StarClass = starClass;

                // Hyper lanes
                galacticObject.HyperLanes = new List<HyperLane>();
                var hyperLanesSection = galacticObjectItem.GetChildSectionByName("hyperlane");
                if (hyperLanesSection != null)
                {
                    foreach (var hyperLaneSection in hyperLanesSection.Sections)
                    {
                        var hyperLane = new HyperLane();
                        int toGalacticObjectIndex;
                        int.TryParse(hyperLaneSection.GetAttributeValueByName("to"), out toGalacticObjectIndex);
                        hyperLane.ToGalacticObjectIndex = toGalacticObjectIndex;
                        double hyperLaneLength;
                        double.TryParse(hyperLaneSection.GetAttributeValueByName("length"), out hyperLaneLength);
                        hyperLane.Length = hyperLaneLength;
                        galacticObject.HyperLanes.Add(hyperLane);
                    }
                }

                // Flags
                galacticObject.GalacticObjectFlags = new List<GalacticObjectFlag>();
                var flagsSection = galacticObjectItem.GetChildSectionByName("flags");
                if (flagsSection != null)
                {
                    foreach (var flagAttribute in flagsSection.Attributes)
                    {
                        GalacticObjectFlag flag;
                        Enum.TryParse(flagAttribute.Name, out flag);
                        galacticObject.GalacticObjectFlags.Add(flag);
                    }
                }

                gameState.GalacticObjects.Add(galacticObject);
            }
        }

        private static void ParseGamestateCountries(GameState gameState)
        {
            var gameStateRaw = gameState.GameStateRaw;

            gameState.Countries = new List<Country>();
            var countrySection = gameStateRaw.RootSection.GetChildSectionByName("country");
            foreach (var countrySectionItem in countrySection.Sections)
            {
                var country = new Country();
                
                country.Name = countrySectionItem.GetAttributeValueByName("name");

                var startingSystemAttribute = countrySectionItem.GetAttributeByName("starting_system");
                if (startingSystemAttribute != null)
                {
                    int startingSystemIndex;
                    int.TryParse(startingSystemAttribute.Value, out startingSystemIndex);
                    country.StartingSystemIndex = startingSystemIndex;
                }

                gameState.Countries.Add(country);
            }
        }

        private static int GetSectionEndLine(List<string> gameStateText, int objectStartLine)
        {
            int objectEndLine = gameStateText.Count - 1;
            int scopeCount = 0;
            for (int i = objectStartLine; i < gameStateText.Count; ++i)
            {
                var line = gameStateText[i];
                if (line.Contains("{"))
                    ++scopeCount;
                if (line.Contains("}"))
                    --scopeCount;
                if (scopeCount == 0)
                {
                    objectEndLine = i;
                    break;
                }
            }
            return objectEndLine;
        }

        private static int FindSectionStart(string sectionName, List<string> gameStateText, int startSearchOnLine = 0, int maxSearchToLine = 0)
        {
            var sectionStartLine = -1;
            var maxLine = maxSearchToLine > 0 ? maxSearchToLine : gameStateText.Count - 1;
            for (int i = startSearchOnLine; i <= maxLine; ++i)
            {
                var line = gameStateText[i];
                if (line.TrimStart().StartsWith(sectionName + "={"))
                {
                    sectionStartLine = i;
                    break;
                }
            }
            return sectionStartLine;
        }

        private static int FindNextAttribute(string attributeName, List<string> gameStateText, int startSearchOnLine = 0, int maxSearchToLine = 0)
        {
            var sectionStartLine = -1;
            var maxLine = maxSearchToLine > 0 ? maxSearchToLine : gameStateText.Count - 1;
            for (int i = startSearchOnLine; i <= maxLine; ++i)
            {
                var line = gameStateText[i];
                if (line.TrimStart().StartsWith(attributeName + "="))
                {
                    sectionStartLine = i;
                    break;
                }
            }
            return sectionStartLine;
        }
    }
}
