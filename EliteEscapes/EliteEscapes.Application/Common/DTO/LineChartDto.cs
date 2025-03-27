namespace EliteEscapes.Web.ViewModels
{
    public class LineChartDto
    {
        public List<ChartData> Series { get; set; }
        public string[] Catagories { get; set; }
    }
    public class ChartData
    {
        public string Name { get; set; }
        public int[] Data { get; set; }
    }
}
