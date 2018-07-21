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

            var rawParser = new GameStateRawParser(_logger);
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
            int playerCountryIndex;
            int.TryParse(galacticObjectSection.Sections.First().GetAttributeValueByName("country"), out playerCountryIndex);
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
                var typeString = galacticObjectItem.GetAttributeValueByName("type");
                Enum.TryParse(typeString, out type);
                if (type == GalacticObjectType.unknown)
                {
                    // Log unknown flag
                    _logger.Log(LogLevel.Information, "Unknown galactic object type: " + typeString);
                }
                galacticObject.Type = typeString;
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
                var starClassString = galacticObjectItem.GetAttributeValueByName("star_class");
                Enum.TryParse(starClassString, out starClass);
                if (starClass == StarClass.unknown)
                {
                    // Log unknown flag
                    _logger.Log(LogLevel.Information, "Unknown star class: " + starClassString);
                }
                galacticObject.StarClass = starClassString;

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
                galacticObject.GalacticObjectFlags = new List<string>();
                var flagsSection = galacticObjectItem.GetChildSectionByName("flags");
                if (flagsSection != null)
                {
                    foreach (var flagAttribute in flagsSection.Attributes)
                    {
                        GalacticObjectFlag flag;
                        Enum.TryParse(flagAttribute.Name, out flag);
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

    }
}
