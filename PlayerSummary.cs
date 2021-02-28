using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASRT_BoostLeagueAssistant
{
    public class PlayerSummary
    {
        public string name;
        public double[] points; // either individual tracks, or frequency data
        public int[] positions; // either individual tracks, or frequency data
        public double totalPoints;
        public double totalPositions;
        public int nTracks;
    }
}
