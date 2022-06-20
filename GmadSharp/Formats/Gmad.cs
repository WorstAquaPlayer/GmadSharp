namespace GmadSharp.Formats
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json;
    using Yarhl.FileSystem;
    using Yarhl.IO;

    public class Gmad : NodeContainerFormat
    {
        public static readonly byte Version = 3;

        public string Name { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public bool DoCRC { get; set; }

        public static Gmad FromFileList(string dirPath, IEnumerable<string> fileList, FileOpenMode mode = FileOpenMode.ReadWrite)
        {
            dirPath = Path.GetFullPath(dirPath);

            if (dirPath[^1] == Path.DirectorySeparatorChar)
            {
                dirPath = dirPath.Remove(dirPath.Length - 1);
            }

            string nodeName = Path.GetFileName(dirPath);

            var gmad = new Gmad();
            gmad.Root.Tags["DirectoryInfo"] = new DirectoryInfo(dirPath);

            foreach (string filePath in fileList)
            {
                var fullPath = Path.Combine(dirPath, filePath);
                NodeFactory.CreateContainersForChild(gmad.Root, Path.GetDirectoryName(filePath), NodeFactory.FromFile(fullPath, mode));
            }

            foreach (Node node in Navigator.IterateNodes(gmad.Root))
            {
                if (!node.IsContainer || node.Tags.ContainsKey("DirectoryInfo"))
                {
                    continue;
                }

                int rootPathLength = $"{NodeSystem.PathSeparator}{nodeName}".Length;
                string nodePath = Path.GetFullPath(string.Concat(dirPath, node.Path[rootPathLength..]));
                node.Tags["DirectoryInfo"] = new DirectoryInfo(nodePath);
            }

            return gmad;
        }

        public string GenerateJson()
        {
            var jsonDoc = JsonDocument.Parse(Description);

            var type = jsonDoc.RootElement.GetProperty("type").GetString();

            var tagsProperty = jsonDoc.RootElement.GetProperty("tags");
            var tags = new List<string>();

            for (int i = 0; i < tagsProperty.GetArrayLength(); i++)
            {
                tags.Add(tagsProperty[i].GetString());
            }

            var addonJson = new AddonJson();
            addonJson.title = Name;
            addonJson.type = type;
            addonJson.tags = tags;
            addonJson.ignore = new List<string> { "*.psd", "*.vcproj", "*.svn" }; // Default value, feel free to change it

            var serializerOptions = new JsonSerializerOptions { WriteIndented = true };

            return JsonSerializer.Serialize(addonJson, serializerOptions);
        }
    }
}
