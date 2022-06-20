namespace GmadSharp.Formats
{
    public class AddonEntry
    {
        public string Name { get; set; }
        public long Size { get; set; }
        public uint CRC { get; set; }
        public int FileNumber { get; set; }
        public long Offset { get; set; }
    }
}
