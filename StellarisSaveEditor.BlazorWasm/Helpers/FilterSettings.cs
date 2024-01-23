using StellarisSaveEditor.Models;

namespace StellarisSaveEditor.BlazorWasm.Helpers
{
    public class FilterSettings
    {
        public bool ShowHyperLanes { get; set; }
        public bool ShowHomeSystem { get; set; }
        public bool ShowWormholes { get; set; }
        public bool ShowGateways { get; set; }
        public bool ShowLgates { get; set; }

        public FilterSettings() {
            ShowHyperLanes = true;
            ShowHomeSystem = true;
            ShowWormholes = false;
            ShowGateways = false;
            ShowLgates = false;
        }
    }
}
