namespace GmadSharp.Converters
{
    using System;
    using Force.Crc32;
    using GmadSharp.Formats;
    using Yarhl.FileFormat;
    using Yarhl.FileSystem;
    using Yarhl.IO;

    public class Gmad2Binary : IInitializer<Action<string>>, IConverter<Gmad, BinaryFormat>
    {
        Action<string> progressReport;
        bool log = false;

        public void Initialize(Action<string> progressReport)
        {
            if (progressReport != null)
            {
                this.progressReport = progressReport;
                log = true;
            }
        }

        public BinaryFormat Convert(Gmad source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var binary = new BinaryFormat();
            var writer = new DataWriter(binary.Stream);

            writer.Write("GMAD", false); // Header
            writer.Write(Gmad.Version); // GMA version
            writer.Write((long)0); // SteamID (unused)
            writer.Write(DateTimeOffset.Now.ToUnixTimeSeconds()); // Timestamp
            writer.Write((byte)0); // Required content (unused?), https://github.com/Facepunch/gmad/blob/master/src/create_gmad.cpp#L74
            writer.Write(source.Name);
            writer.Write(source.Description);
            writer.Write(source.Author);
            writer.Write(1); // Addon version (unused)

            ReportAction("Writing file list...");

            using var crc32Algorithm = new Crc32Algorithm();

            uint fileNum = 0;
            foreach (var file in Navigator.IterateNodes(source.Root))
            {
                if (file.IsContainer)
                {
                    continue;
                }

                if (file.Stream.Length <= 0)
                {
                    var test123 = $"{file.Path}";
                    throw new FormatException($"File '{file.Name}' seems to be empty, or we couldn't get its size!");
                }

                fileNum++;
                writer.Write(fileNum);

                var filename = file.Path.Replace(source.Root.Path + NodeSystem.PathSeparator, string.Empty).ToLower().Replace('\\', '/');
                writer.Write(filename);

                writer.Write(file.Stream.Length);

                if (source.DoCRC)
                {
                    var hash = crc32Algorithm.ComputeHash(file.Stream);
                    Array.Reverse(hash);

                    var crc = BitConverter.ToUInt32(hash);
                    writer.Write(crc);
                }
                else
                {
                    writer.Write(0);
                }
            }

            fileNum = 0;
            writer.Write(fileNum);

            ReportAction("Writing files...");

            foreach (var file in Navigator.IterateNodes(source.Root))
            {
                if (file.IsContainer)
                {
                    continue;
                }

                var before = writer.Stream.Length;
                file.Stream.WriteTo(writer.Stream);
                var diff = writer.Stream.Length - before;

                if (diff < 1)
                {
                    throw new FormatException($"Failed to write file '{file.Name}' - written {diff} bytes! (Can't grow buffer?)");
                }
            }

            if (source.DoCRC)
            {
                var hash = crc32Algorithm.ComputeHash(writer.Stream);
                Array.Reverse(hash);

                var crc = BitConverter.ToUInt32(hash);
                writer.Write(crc);
            }
            else
            {
                writer.Write(0);
            }

            return binary;
        }

        void ReportAction(string value)
        {
            if (log)
            {
                progressReport(value);
            }
        }
    }
}
