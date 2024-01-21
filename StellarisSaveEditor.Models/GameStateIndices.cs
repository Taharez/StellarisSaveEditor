using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StellarisSaveEditor.Models
{
    public class GameStateIndices
    {
        public Dictionary<int, GalacticObject> GalacticObjectByBypassId { get; set; }

        public GameStateIndices() {
            GalacticObjectByBypassId = new Dictionary<int, GalacticObject>();
        }
    }
}
