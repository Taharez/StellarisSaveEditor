using System.Collections.Generic;
using System;
using System.Linq;
using StellarisSaveEditor.Models;
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
            var gameState = new GameState();

            var rawParser = new GameStateRawParser();
            rawParser.ParseGamestateRaw(gameState.GameStateRaw, gameStateText);
            
            ParseGamestateCommon(gameState);

            ParseGamestateGalacticObjects(gameState);

            ParseGamestateCountries(gameState);

            ParseGamestateBypasses(gameState);

            ParseGamestateWormholes(gameState);

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

            gameState.GalacticObjects = new Dictionary<int, GalacticObject>();
            gameState.Indices.GalacticObjectByBypassId = new Dictionary<int, GalacticObject>();
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
                galacticObject.Type = typeString;
                galacticObject.Name = ParseLocalizableString(galacticObjectItem);

                // Planets 
                var planetAttributes = galacticObjectItem.GetAttributeValuesByName("planet");
                foreach (var planetAttribute in planetAttributes)
                {
                    int.TryParse(planetAttribute, out var planetIndex);
                    galacticObject.PlanetIndices.Add(planetIndex);
                }

                // Star class
                var starClassString = galacticObjectItem.GetAttributeValueByName("star_class");
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

                // Bypasses
                var bypasses = ParseIntList(galacticObjectItem.GetChildSectionByName("bypasses"));
                if (bypasses.Any())
                {
                    foreach (var bypassId in bypasses)
                    {;
                        galacticObject.Bypasses.Add(bypassId);
                        gameState.Indices.GalacticObjectByBypassId.Add(bypassId, galacticObject);
                    }
                }

                // Flags
                var flagsSection = galacticObjectItem.GetChildSectionByName("flags");
                if (flagsSection != null)
                {
                    foreach (var flagAttribute in flagsSection.Attributes)
                    {
                        galacticObject.GalacticObjectFlags.Add(flagAttribute.Name);
                    }
                }

                gameState.GalacticObjects.Add(galacticObject.Id, galacticObject);
            }
        }

        private void ParseGamestateCountries(GameState gameState)
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

        private void ParseGamestateBypasses(GameState gameState)
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
                bypass.Connections = ParseIntList(bypassSectionItem.GetChildSectionByName("connections"));

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

        private LocalizableString ParseLocalizableString(GameStateRawSection rawSection)
        {
            var nameSection = rawSection.GetChildSectionByName("name");
            var key = nameSection.GetAttributeValueByName("key");

            return new LocalizableString
            {
                Key = key
            };
        }
        private List<int> ParseIntList(GameStateRawSection rawSection)
        {
            if (rawSection == null)
                return new List<int>();
            var rawAttribute = rawSection.Attributes.FirstOrDefault();
            if (rawAttribute == null)
                return new List<int>();
            return rawAttribute.Value.Split(' ').Select((i) => int.Parse(i)).ToList();
        }
    }
}
