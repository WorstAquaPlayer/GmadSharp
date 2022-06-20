namespace GmadSharp.Formats
{
    using System.Collections.Generic;

    public class GmadJson
    {
        public string description { get; set; }
        public string type { get; set; }
        public IEnumerable<string> tags { get; set; }
    }
}
