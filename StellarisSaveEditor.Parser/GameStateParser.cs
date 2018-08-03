using System.Collections.Generic;
using System;
using System.Linq;
using StellarisSaveEditor.Models;
using StellarisSaveEditor.Models.Enums;
using StellarisSaveEditor.Models.Extensions;
using StellarisSaveEditor.Common;

namespace StellarisSaveEditor.Parser
{
    public class GameStateParser
    {
        private readonly ILogger _logger;

        public GameStateParser(ILogger logger)
        {
            _logger = logger;
        }

        public GameState ParseGamestate(List<string> gameStateText)
        {
            var gameStateRaw = new GameStateRaw();
            var gameState = new GameState() { GameStateRaw = gameStateRaw };

            var rawParser = new GameStateRawParser();
            rawParser.ParseGamestateRaw(gameStateRaw, gameStateText);
            
            ParseGamestateCommon(gameState);

            ParseGamestateGalacticObjects(gameState);

            ParseGamestateCountries(gameState);

            return gameState;
        }

        private void ParseGamestateCommon(GameState gameState)
        {
            var gameStateRaw = gameState.GameStateRaw;
            
            gameState.Version = gameStateRaw.RootSection.GetAttributeValueByName("version");
            gameState.VersionControlRevision = gameStateRaw.RootSection.GetAttributeValueByName("version_control_revision");
            gameState.Name = gameStateRaw.RootSection.GetAttributeValueByName("name");

            gameState.Player = new Player();
            var galacticObjectSection = gameStateRaw.RootSection.GetChildSectionByName("player");
            gameState.Player.Name = galacticObjectSection.Sections.First().GetAttributeValueByName("name");
            int.TryParse(galacticObjectSection.Sections.First().GetAttributeValueByName("country"), out var playerCountryIndex);
            gameState.Player.CountryIndex = playerCountryIndex;
        }

        private void ParseGamestateGalacticObjects(GameState gameState)
        {
            var gameStateRaw = gameState.GameStateRaw;

            gameState.GalacticObjects = new List<GalacticObject>();
            var galacticObjectSection = gameStateRaw.RootSection.GetChildSectionByName("galactic_object");
            foreach (var galacticObjectItem in galacticObjectSection.Sections)
            {
                var galacticObject = new GalacticObject();

                // Coordinate
                var coordinateSection = galacticObjectItem.GetChildSectionByName("coordinate");
                double.TryParse(coordinateSection.GetAttributeValueByName("x"), out var x);
                double.TryParse(coordinateSection.GetAttributeValueByName("y"), out var y);
                long.TryParse(coordinateSection.GetAttributeValueByName("origin"), out var origin);
                var randomized = coordinateSection.GetAttributeValueByName("randomized");
                galacticObject.Coordinate.X = x;
                galacticObject.Coordinate.Y = y;
                galacticObject.Coordinate.Origin = origin;
                galacticObject.Coordinate.Randomized = randomized.Equals("yes");

                // Type and name
                var typeString = galacticObjectItem.GetAttributeValueByName("type");
                Enum.TryParse(typeString, out GalacticObjectType type);
                if (type == GalacticObjectType.unknown)
                {
                    // Log unknown flag
                    _logger.Log(LogLevel.Information, "Unknown galactic object type: " + typeString);
                }
                galacticObject.Type = typeString;
                galacticObject.Name = galacticObjectItem.GetAttributeValueByName("name");

                // Planets 
                var planetAttributes = galacticObjectItem.GetAttributeValuesByName("planet");
                foreach (var planetAttribute in planetAttributes)
                {
                    int.TryParse(planetAttribute, out var planetIndex);
                    galacticObject.PlanetIndices.Add(planetIndex);
                }

                // Star class
                var starClassString = galacticObjectItem.GetAttributeValueByName("star_class");
                Enum.TryParse(starClassString, out StarClass starClass);
                if (starClass == StarClass.unknown)
                {
                    // Log unknown flag
                    _logger.Log(LogLevel.Information, "Unknown star class: " + starClassString);
                }
                galacticObject.StarClass = starClassString;

                // Hyper lanes
                var hyperLanesSection = galacticObjectItem.GetChildSectionByName("hyperlane");
                if (hyperLanesSection != null)
                {
                    foreach (var hyperLaneSection in hyperLanesSection.Sections)
                    {
                        var hyperLane = new HyperLane();
                        int.TryParse(hyperLaneSection.GetAttributeValueByName("to"), out var toGalacticObjectIndex);
                        hyperLane.ToGalacticObjectIndex = toGalacticObjectIndex;
                        double.TryParse(hyperLaneSection.GetAttributeValueByName("length"), out var hyperLaneLength);
                        hyperLane.Length = hyperLaneLength;
                        galacticObject.HyperLanes.Add(hyperLane);
                    }
                }

                // Flags
                var flagsSection = galacticObjectItem.GetChildSectionByName("flags");
                if (flagsSection != null)
                {
                    foreach (var flagAttribute in flagsSection.Attributes)
                    {
                        Enum.TryParse(flagAttribute.Name, out GalacticObjectFlag flag);
                        if (flag == GalacticObjectFlag.unknown)
                        {
                            // Log unknown flag
                            _logger.Log(LogLevel.Information, "Unknown galactic object flag: " + flagAttribute.Name);
                        }
                        galacticObject.GalacticObjectFlags.Add(flagAttribute.Name);
                    }
                }

                gameState.GalacticObjects.Add(galacticObject);
            }
        }

        private void ParseGamestateCountries(GameState gameState)
        {
            var gameStateRaw = gameState.GameStateRaw;

            gameState.Countries = new List<Country>();
            var countrySection = gameStateRaw.RootSection.GetChildSectionByName("country");
            foreach (var countrySectionItem in countrySection.Sections)
            {
                var country = new Country {Name = countrySectionItem.GetAttributeValueByName("name")};

            var startingSystemAttribute = countrySectionItem.GetAttributeByName("starting_system");
                if (startingSystemAttribute != null)
                {
                    int.TryParse(startingSystemAttribute.Value, out var startingSystemIndex);
                    country.StartingSystemIndex = startingSystemIndex;
                }

                gameState.Countries.Add(country);
            }
        }

    }
}
