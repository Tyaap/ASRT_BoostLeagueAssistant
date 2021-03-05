namespace ASRT_BoostLeagueAssistant
{
    public class PlayerSummary
    {
        public int rank;
        public string name;
        public double[] points; // either individual tracks, or frequency data
        public int[] positions; // either individual tracks, or frequency data   
        public double totalPoints;
        public double totalPositions;
        public int nTracks;

        // Multi-matchday info
        public double totalPointsOld; // change in points since last matchday
        public int rankOld; // change in rank since last matchday
        public int parts; // participations
        public int[] wins; // matchday overall positions

        // Selectively show deltas
        public bool showRankDelta;
        public bool showPointDelta;

        public static int Compare(PlayerSummary x, PlayerSummary y)
        {
            return x.totalPoints != y.totalPoints || x.nTracks == 0 ? 
                y.totalPoints.CompareTo(x.totalPoints) : 
                (y.totalPoints / y.nTracks).CompareTo(x.totalPoints / x.nTracks);
        }

        public static int CompareOld(PlayerSummary x, PlayerSummary y)
        {
            return x.totalPointsOld != y.totalPointsOld || x.nTracks == 0?
                y.totalPointsOld.CompareTo(x.totalPointsOld) :
                (y.totalPointsOld / y.nTracks).CompareTo(x.totalPointsOld / x.nTracks);
        }
    }
}
