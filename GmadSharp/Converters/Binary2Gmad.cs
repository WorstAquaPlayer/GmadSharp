namespace GmadSharp.Converters
{
    using System;
    using System.Collections.Generic;
    using Force.Crc32;
    using GmadSharp.Formats;
    using Yarhl.FileFormat;
    using Yarhl.FileSystem;
    using Yarhl.IO;

    public class Binary2Gmad : IInitializer<bool?>, IConverter<BinaryFormat, Gmad>
    {
        bool checkCrc = false;

        public void Initialize(bool? checkCrc)
        {
            if (checkCrc.HasValue)
            {
                this.checkCrc = checkCrc.Value;
            }
            else
            {
                this.checkCrc = false;
            }
        }

        public Gmad Convert(BinaryFormat source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var gmad = new Gmad();

            var reader = new DataReader(source.Stream);

            // Check header
            if (reader.ReadChar() != 'G' || reader.ReadByte() != 'M' || reader.ReadByte() != 'A' || reader.ReadByte() != 'D')
            {
                throw new FormatException("Invalid GMAD header");
            }

            var version = reader.ReadByte();

            if (version > Gmad.Version)
            {
                throw new FormatException("Unsupported GMAD file version");
            }

            reader.ReadUInt64(); // steamid
            reader.ReadUInt64(); // timestamp

            if (version > 1)
            {
                var content = reader.ReadString();

                while (!string.IsNullOrEmpty(content))
                {
                    content = reader.ReadString();
                }
            }

            gmad.Name = reader.ReadString();
            gmad.Description = reader.ReadString();
            gmad.Author = reader.ReadString();

            reader.ReadInt32(); // Addon version

            int fileNumber = 0;
            long offset = 0;

            var entries = new List<AddonEntry>();

            while (reader.ReadUInt32() != 0)
            {
                var entry = new AddonEntry();

                entry.Name = reader.ReadString().Replace('/', '\\');
                entry.Size = reader.ReadInt64();
                entry.CRC = reader.ReadUInt32();
                entry.FileNumber = fileNumber;
                entry.Offset = offset;

                offset += entry.Size;
                fileNumber++;

                entries.Add(entry);
            }

            var filesBlock = reader.Stream.Position;
            reader.Stream.Position = 0;

            using var crc32Algorithm = new Crc32Algorithm();

            foreach (var entry in entries)
            {
                var stream = DataStreamFactory.FromStream(reader.Stream, filesBlock + entry.Offset, entry.Size);

                if (checkCrc)
                {
                    var hash = crc32Algorithm.ComputeHash(stream);
                    Array.Reverse(hash);

                    var crc = BitConverter.ToUInt32(hash);

                    if (crc != entry.CRC)
                    {
                        throw new FormatException($"\"{entry.Name}\" calculated hash ({crc}) doesn't match the one in the file ({entry.CRC})");
                    }
                }

                gmad.Root.Add(new Node(entry.Name, new BinaryFormat(stream)));
            }

            if (checkCrc)
            {
                reader.Stream.Position = reader.Stream.Length - 4;

                using var stream = DataStreamFactory.FromStream(reader.Stream, 0, reader.Stream.Length - 4);

                var hash = crc32Algorithm.ComputeHash(stream);
                Array.Reverse(hash);

                var crc = BitConverter.ToUInt32(hash);

                if (crc != reader.ReadUInt32())
                {
                    throw new FormatException($"Hash at the end of file doesn't match with the actual file.");
                }
            }

            reader.Stream.Position = 0;

            return gmad;
        }
    }
}
