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
        public int oldRank; // change in rank since previous matchday
        public int parts; // participations
        public int[] wins; // matchday positions

        public static int Compare(PlayerSummary x, PlayerSummary y)
        {
            int comp = y.totalPoints.CompareTo(x.totalPoints);
            return x.totalPoints != y.totalPoints ? 
                y.totalPoints.CompareTo(x.totalPoints) : 
                (y.totalPoints / y.nTracks).CompareTo(x.totalPoints / x.nTracks);
        }
    }
}
