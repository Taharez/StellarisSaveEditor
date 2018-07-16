using System.Collections.Generic;
using StellarisSaveEditor.Models;
using StellarisSaveEditor.Extensions;
using System;
using System.Linq;
using StellarisSaveEditor.Enums;
using System.Text.RegularExpressions;

namespace StellarisSaveEditor.Helpers
{
    public static class GameStateParser
    {
        public static GameState ParseGamestate(List<string> gameStateText)
        {
            var gameState = new GameState();

            ParseGamestateCommon(gameState, gameStateText);

            ParseGamestateGalacticObjects(gameState, gameStateText);

            ParseGamestateCountries(gameState, gameStateText);

            return gameState;
        }

        private static void ParseGamestateCommon(GameState gameState, List<string> gameStateText)
        {
            gameState.Version = gameStateText.FindAndGetValue("version");
            gameState.VersionControlRevision = gameStateText.FindAndGetValue("version_control_revision");
            gameState.Name = gameStateText.FindAndGetValue("name");

            gameState.Player = new Player();
            int playerSectionStartLine = FindSectionStart("player", gameStateText);
            var playerNameLine = gameStateText[playerSectionStartLine + 2];
            gameState.Player.Name = playerNameLine.GetValue("name");
            var playerCountryLine = gameStateText[playerSectionStartLine + 3];
            int playerCountryIndex;
            int.TryParse(playerCountryLine.GetValue("country"), out playerCountryIndex);
            gameState.Player.CountryIndex = playerCountryIndex;
        }

        private static void ParseGamestateGalacticObjects(GameState gameState, List<string> gameStateText, int startIndex = 0)
        {
            int galacticObjectSectionStartLine = FindSectionStart("galactic_object", gameStateText, startIndex);

            if (galacticObjectSectionStartLine < 0)
                return;

            int galacticObjectSectionEndLine = GetSectionEndLine(gameStateText, galacticObjectSectionStartLine);
            if (galacticObjectSectionEndLine < galacticObjectSectionStartLine)
                return;

            // Read GalacticObjects
            gameState.GalacticObjects = new List<GalacticObject>();
            for (int i = galacticObjectSectionStartLine; i < galacticObjectSectionEndLine; ++i)
            {
                var galacticObject = new GalacticObject();
                var line = gameStateText[i];
                if (Regex.IsMatch(line, "[0-9]+={"))
                {
                    int objectStartLine = i;
                    int objectEndLine = GetSectionEndLine(gameStateText, objectStartLine);
                    galacticObject.Coordinate = new GalacticObjectCoordinate();

                    // Coordinates
                    var xLine = gameStateText[i + 2];
                    var yLine = gameStateText[i + 3];
                    var originLine = gameStateText[i + 4];
                    var randomizedLine = gameStateText[i + 5];
                    double x;
                    double y;
                    long origin;
                    string randomized;
                    double.TryParse(xLine.GetValue("x"), out x);
                    double.TryParse(yLine.GetValue("y"), out y);
                    long.TryParse(originLine.GetValue("origin"), out origin);
                    randomized = randomizedLine.GetValue("randomized");

                    galacticObject.Coordinate.X = x;
                    galacticObject.Coordinate.Y = y;
                    galacticObject.Coordinate.Origin = origin;
                    galacticObject.Coordinate.Randomized = randomized.Equals("yes");

                    // Type and name
                    var typeLine = gameStateText[i + 7];
                    GalacticObjectType type;
                    Enum.TryParse(line.GetValue("type"), out type);
                    galacticObject.Type = type;
                    var nameLine = gameStateText[i + 8];
                    galacticObject.Name = nameLine.GetValue("name");

                    // Planets 
                    galacticObject.PlanetIndices = new List<int>();
                    var planetIndexLine = 9;
                    var planetLine = gameStateText[i + planetIndexLine];
                    while (planetLine.Contains("planet"))
                    {
                        int planetIndex;
                        int.TryParse(planetLine.GetValue("planet"), out planetIndex);
                        galacticObject.PlanetIndices.Add(planetIndex);
                        ++planetIndexLine;
                        planetLine = gameStateText[i + planetIndexLine];
                    }

                    i = i + planetIndexLine;

                    int starClassLineIndex = FindNextAttribute("star_class", gameStateText, i);

                    // Star class
                    var starClassLine = gameStateText[starClassLineIndex];
                    StarClass starClass;
                    Enum.TryParse(starClassLine.GetValue("star_class"), out starClass);
                    galacticObject.StarClass = starClass;

                    i = starClassLineIndex + 1;

                    // Hyper lanes
                    galacticObject.HyperLanes = new List<HyperLane>();
                    int hyperLaneSectionStartLine = FindSectionStart("hyperlane", gameStateText, i, galacticObjectSectionEndLine);
                    if (hyperLaneSectionStartLine > 0)
                    {
                        int hyperLaneSectionEndLine = GetSectionEndLine(gameStateText, hyperLaneSectionStartLine);
                        for (int h = hyperLaneSectionStartLine; h < hyperLaneSectionEndLine; ++h)
                        {
                            var hyperLaneLine = gameStateText[h];
                            if (hyperLaneLine.Trim().StartsWith("to="))
                            {
                                var hyperLane = new HyperLane();
                                int toGalacticObjectIndex;
                                int.TryParse(hyperLaneLine.GetValue("to"), out toGalacticObjectIndex);
                                hyperLane.ToGalacticObjectIndex = toGalacticObjectIndex;
                                hyperLaneLine = gameStateText[h + 1];
                                double hyperLaneLength;
                                double.TryParse(hyperLaneLine.GetValue("length"), out hyperLaneLength);
                                hyperLane.Length = hyperLaneLength;
                                galacticObject.HyperLanes.Add(hyperLane);
                                h += 3;
                            }
                        }

                        i = hyperLaneSectionEndLine;
                    }

                    // Flags
                    galacticObject.GalacticObjectFlags = new List<GalacticObjectFlag>();
                    int flagsSectionStartLine = FindSectionStart("flags", gameStateText, i, galacticObjectSectionEndLine);
                    if (flagsSectionStartLine > 0)
                    {
                        int flagsSectionEndLine = GetSectionEndLine(gameStateText, flagsSectionStartLine);
                        for (int f = flagsSectionStartLine + 1; f < flagsSectionEndLine; ++f)
                        {
                            var flagLine = gameStateText[f].Trim(); ;
                            GalacticObjectFlag flag;
                            Enum.TryParse(flagLine.Substring(0, flagLine.IndexOf("=")), out flag);
                            galacticObject.GalacticObjectFlags.Add(flag);
                        }
                    }

                    gameState.GalacticObjects.Add(galacticObject);

                    i = objectEndLine;
                }
            }
        }

        private static void ParseGamestateCountries(GameState gameState, List<string> gameStateText, int startIndex = 0)
        {
            int countrySectionStartLine = FindSectionStart("country", gameStateText, startIndex);

            int countrySectionEndLine = GetSectionEndLine(gameStateText, countrySectionStartLine);
            if (countrySectionEndLine < countrySectionStartLine)
                return;

            // Read countries
            gameState.Countries = new List<Country>();
            for (int i = countrySectionStartLine; i < countrySectionEndLine; ++i)
            {
                var country = new Country();
                var line = gameStateText[i];
                if (Regex.IsMatch(line, "[0-9]+={"))
                {
                    int countryStartLine = i;
                    int countryEndLine = GetSectionEndLine(gameStateText, countryStartLine);

                    country.Name = gameStateText[i + 18].GetValue("name");

                    int startingSystemIndexLine = FindNextAttribute("starting_system", gameStateText, i + 18, countryEndLine);
                    if (startingSystemIndexLine > 0)
                    {
                        int startingSystemIndex;
                        int.TryParse(gameStateText[startingSystemIndexLine], out startingSystemIndex);
                        country.StartingSystemIndex = startingSystemIndex;
                    }

                    gameState.Countries.Add(country);

                    i = countryEndLine;
                }
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
