using System.Collections.Generic;
using System;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using StellarisSaveEditor.Models;

namespace StellarisSaveEditor.Parser
{
    public class GameStateRawParser
    {
        public void ParseGameStateRaw(GameStateRaw gameStateRaw, List<string> gameStateText)
        {
            gameStateRaw.RootSection = new GameStateRawSection // Container for all top-level sections and attributes
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
                }
                else if (currentLine.Contains("={"))
                {
                    // Named section start
                    var sectionName = currentLine.Substring(0, currentLine.IndexOf("={", StringComparison.InvariantCulture));
                    var section = new GameStateRawSection
                    {
                        Parent = currentSection,
                        Name = sectionName
                    };
                    currentSection.Sections.Add(section);
                    // Check for single-line section/list
                    if (currentLine.Contains("}"))
                    {
                        var valueStartIndex = currentLine.IndexOf("={", StringComparison.InvariantCulture) + 2;
                        var valueEndIndex = currentLine.IndexOf("}", StringComparison.InvariantCulture) - 1;
                        if (valueStartIndex < 0)
                            throw new Exception("Start index < 0, line: '" + currentLine + "'");
                        if (valueEndIndex < 0)
                            throw new Exception("End index < 0, line: '" + currentLine + "'");
                        if (valueEndIndex - valueStartIndex < 0)
                            throw new Exception("End - Start index < 0, line: '" + currentLine + "'");
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
                    var attributeName = currentLine.Substring(0, currentLine.IndexOf("=", StringComparison.InvariantCulture));
                    var attributeValue = currentLine.Substring(currentLine.IndexOf("=", StringComparison.InvariantCulture) + 1).Trim('\"');
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

        public async Task<bool> ParseGameStateRawAsync(GameStateRaw gameStateRaw, Stream gameStateStream)
        {
            gameStateRaw.RootSection = new GameStateRawSection // Container for all top-level sections and attributes
            {
                Name = "Root"
            };
            var currentSection = gameStateRaw.RootSection;
            using (var reader = new StreamReader(gameStateStream))
            {
                while (!reader.EndOfStream)
                {
                    var currentLine = await reader.ReadLineAsync();
                    currentLine = currentLine.Trim();
                    if (String.IsNullOrWhiteSpace(currentLine))
                    {
                        // Skip blank lines
                    }
                    else if (currentLine.Contains("={"))
                    {
                        // Named section start
                        var sectionName = currentLine.Substring(0,
                            currentLine.IndexOf("={", StringComparison.InvariantCulture));
                        var section = new GameStateRawSection
                        {
                            Parent = currentSection,
                            Name = sectionName
                        };
                        currentSection.Sections.Add(section);
                        // Check for single-line section/list
                        if (currentLine.Contains("}"))
                        {
                            var valueStartIndex = currentLine.IndexOf("={", StringComparison.InvariantCulture) + 2;
                            var valueEndIndex = currentLine.IndexOf("}", StringComparison.InvariantCulture) - 1;
                            if (valueStartIndex < 0)
                                throw new Exception("Start index < 0, line: '" + currentLine + "'");
                            if (valueEndIndex < 0)
                                throw new Exception("End index < 0, line: '" + currentLine + "'");
                            if (valueEndIndex - valueStartIndex < 0)
                                throw new Exception("End - Start index < 0, line: '" + currentLine + "'");
                            var attributeValue = currentLine.Substring(valueStartIndex, valueEndIndex - valueStartIndex)
                                .Trim();
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
                            //Debug.Assert(i == gameStateText.Count - 1);
                            //return false;
                        }
                        else
                        {
                            currentSection = currentSection.Parent;
                        }
                    }
                    else if (currentLine.Contains("="))
                    {
                        // Attribute
                        var attributeName = currentLine.Substring(0,
                            currentLine.IndexOf("=", StringComparison.InvariantCulture));
                        var attributeValue = currentLine
                            .Substring(currentLine.IndexOf("=", StringComparison.InvariantCulture) + 1).Trim('\"');
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
                foreach (var sectionToMove in groupedSection)
                {
                    sectionToMove.Parent = section;
                    gameStateRaw.RootSection.Sections.Remove(sectionToMove);
                    section.Sections.Add(sectionToMove);
                }
            }

            return true;
        }
    }
}
