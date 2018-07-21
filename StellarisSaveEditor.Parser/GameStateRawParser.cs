using System.Collections.Generic;
using System;
using System.Linq;
using System.Diagnostics;
using StellarisSaveEditor.Models;
using StellarisSaveEditor.Common;

namespace StellarisSaveEditor.Parser
{
    public class GameStateRawParser
    {
        private readonly ILogger _logger;

        public GameStateRawParser(ILogger logger)
        {
            _logger = logger;
        }

        public void ParseGamestateRaw(GameStateRaw gameStateRaw, List<string> gameStateText)
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
    }
}
