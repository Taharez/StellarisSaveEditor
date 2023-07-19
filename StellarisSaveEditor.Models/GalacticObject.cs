using System.Collections.Generic;

namespace StellarisSaveEditor.Models
{
    public class GalacticObject
    {
        public GalacticObject()
        {
            Coordinate = new Coordinate();
            PlanetIndices = new List<int>();
            AmbientObjects = new List<int>();
            HyperLanes = new List<HyperLane>();
            AsteroidBelts = new List<AsteroidBelt>();
            GalacticObjectFlags = new List<string>();
        }

        public int Id { get; set; }

        public Coordinate Coordinate { get; set; }

        public string Type { get; set; }

        public LocalizableString Name { get; set; }

        public List<int> PlanetIndices { get; set; }

        public List<int> AmbientObjects { get; set; }

        public string StarClass { get; set; }

        public List<HyperLane> HyperLanes { get; set; }

        public List<AsteroidBelt> AsteroidBelts { get; set; }

        public List<string> GalacticObjectFlags { get; set; }

        public string Initializer { get; set; }

        public double InnerRadius { get; set; }

        public double OuterRadius { get; set; }

        public int StarBaseIndex { get; set; }

    }
}
