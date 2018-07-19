using System.Collections.Generic;
using StellarisSaveEditor.Enums;

namespace StellarisSaveEditor.Models
{
    public class GalacticObject
    {
        public GalacticObjectCoordinate Coordinate { get; set; }

        public GalacticObjectType Type { get; set; }

        public string Name { get; set; }

        public List<int> PlanetIndices { get; set; }

        public List<int> AmbientObjects { get; set; }

        public StarClass StarClass { get; set; }

        public List<HyperLane> HyperLanes { get; set; }

        public List<AsteroidBelt> AsteroidBelts { get; set; }

        public List<GalacticObjectFlag> GalacticObjectFlags { get; set; }

        public string Initializer { get; set; }

        public double InnerRadius { get; set; }

        public double OuterRadius { get; set; }

        public int StarBaseIndex { get; set; }

    }
}
