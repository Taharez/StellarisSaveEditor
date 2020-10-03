using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

        public GameState ParseGameStateFromRaw(GameStateRaw gameStateRaw)
        {
            var gameState = new GameState {GameStateRaw = gameStateRaw};
            
            ParseGameStateCommon(gameState);

            ParseGameStateGalacticObjects(gameState);

            ParseGameStateCountries(gameState);

            ParseGameStateBypasses(gameState);

            ParseGamestateWormholes(gameState);

            return gameState;
        }

        public async Task<GameState> ParseGameStateAsync(Stream gameStateStream)
        {
            var gameStateRaw = new GameStateRaw();
            var rawParser = new GameStateRawParser();
            var isRawParsed = await rawParser.ParseGameStateRawAsync(gameStateRaw, gameStateStream);

            if (!isRawParsed)
                return null;
            
            return ParseGameStateFromRaw(gameStateRaw);
        }

        private void ParseGameStateCommon(GameState gameState)
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

        private void ParseGameStateGalacticObjects(GameState gameState)
        {
            var gameStateRaw = gameState.GameStateRaw;

            gameState.GalacticObjects = new Dictionary<int, GalacticObject>();
            var galacticObjectSection = gameStateRaw.RootSection.GetChildSectionByName("galactic_object");
            foreach (var galacticObjectItem in galacticObjectSection.Sections)
            {
                var galacticObject = new GalacticObject();

                int.TryParse(galacticObjectItem.Name, out int galacticObjectId);
                galacticObject.Id = galacticObjectId;

                // Coordinate
                galacticObject.Coordinate = ParseCoordinate(galacticObjectItem);

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

                gameState.GalacticObjects.Add(galacticObject.Id, galacticObject);
            }
        }

        private void ParseGameStateCountries(GameState gameState)
        {
            var gameStateRaw = gameState.GameStateRaw;

            gameState.Countries = new Dictionary<int, Country>();
            var countrySection = gameStateRaw.RootSection.GetChildSectionByName("country");
            foreach (var countrySectionItem in countrySection.Sections)
            {
                var country = new Country
                {
                    Id = int.Parse(countrySectionItem.Name),
                    Name = countrySectionItem.GetAttributeValueByName("name")
                };

                var startingSystemAttribute = countrySectionItem.GetAttributeByName("starting_system");
                if (startingSystemAttribute != null)
                {
                    int.TryParse(startingSystemAttribute.Value, out var startingSystemIndex);
                    country.StartingSystemIndex = startingSystemIndex;
                }

                gameState.Countries.Add(country.Id, country);
            }
        }

        private void ParseGameStateBypasses(GameState gameState)
        {
            var gameStateRaw = gameState.GameStateRaw;

            gameState.Bypasses = new Dictionary<int, Bypass>();
            var bypassSection = gameStateRaw.RootSection.GetChildSectionByName("bypasses");
            foreach (var bypassSectionItem in bypassSection.Sections)
            {
                var bypass = new Bypass{ Id = int.Parse(bypassSectionItem.Name) };

                bypass.BypassType = bypassSectionItem.GetAttributeByName("type").Value;
                var activeAttribute = bypassSectionItem.GetAttributeByName("active");
                bypass.IsActive = activeAttribute.Value.Equals("yes");
                var linkedToAttribute = bypassSectionItem.GetAttributeByName("linked_to");
                if (linkedToAttribute != null && !linkedToAttribute.Value.Equals(String.Empty))
                {
                    int.TryParse(linkedToAttribute.Value, out var linkedTo);
                    bypass.LinkedToBypassId = linkedTo;
                }

                bypass.Owner = ParseOwner(bypassSectionItem);

                gameState.Bypasses.Add(bypass.Id, bypass);
            }
        }

        private void ParseGamestateWormholes(GameState gameState)
        {
            var gameStateRaw = gameState.GameStateRaw;

            gameState.NaturalWormholes = new Dictionary<int, NaturalWormhole>();
            var wormholeSection = gameStateRaw.RootSection.GetChildSectionByName("natural_wormholes");
            foreach (var wormholeSectionItem in wormholeSection.Sections)
            {
                var wormhole = new NaturalWormhole() { Id = int.Parse(wormholeSectionItem.Name) };

                wormhole.Coordinate = ParseCoordinate(wormholeSectionItem);
                var bypassAttribute = wormholeSectionItem.GetAttributeByName("bypass");
                int.TryParse(bypassAttribute.Value, out var bypassId);
                wormhole.BypassId = bypassId;

                gameState.NaturalWormholes.Add(wormhole.Id, wormhole);
            }
        }



        // Shared parsers
        
        private Coordinate ParseCoordinate(GameStateRawSection rawSection)
        {
            var coordinateSection = rawSection.GetChildSectionByName("coordinate");
            double.TryParse(coordinateSection.GetAttributeValueByName("x"), out var x);
            double.TryParse(coordinateSection.GetAttributeValueByName("y"), out var y);
            long.TryParse(coordinateSection.GetAttributeValueByName("origin"), out var origin);
            var randomized = coordinateSection.GetAttributeValueByName("randomized");

            return new Coordinate
            {
                X = x,
                Y = y,
                Origin = origin,
                Randomized = randomized.Equals("yes")
            };
        }

        private Owner ParseOwner(GameStateRawSection rawSection)
        {
            var coordinateSection = rawSection.GetChildSectionByName("owner");
            int.TryParse(coordinateSection.GetAttributeValueByName("type"), out var type);
            int.TryParse(coordinateSection.GetAttributeValueByName("id"), out var id);

            return new Owner
            {
                Type = type,
                Id = id
            };
        }
    }
}
