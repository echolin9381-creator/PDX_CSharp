namespace PDX_CSharp.Models
{
    public class LoopData
    {
        public int    LoopNo      { get; set; }
        public string Phase       { get; set; }
        public double LoadKW      { get; set; }   // total kW
        public double UnitLoadKW  { get; set; }   // kW per device
        public int    DeviceCount { get; set; }   // 1 = single, >1 = multi
        public double Current     { get; set; }
        public string Breaker     { get; set; }
        public string Cable       { get; set; }
        public string Conduit     { get; set; }
        public string DeviceName  { get; set; }
    }
}
