namespace GmadSharp.Formats
{
    using System.Collections.Generic;

    public class AddonJson
    {
        public string title { get; set; }
        public string type { get; set; }
        public IEnumerable<string> tags { get; set; }
        public IEnumerable<string> ignore { get; set; }
    }
}
