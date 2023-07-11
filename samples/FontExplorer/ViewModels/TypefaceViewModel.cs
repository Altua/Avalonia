using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media;
using HarfBuzzSharp;
using MiniMvvm;

namespace FontExplorer.ViewModels
{
    public class NameTable
    {
        public UInt16 Format { get; set; }
        public UInt16 Count { get; set; }
        public UInt16 StringOffset { get; set; }
        public NameRecord[] Records { get; set; }

    }

    public class NameRecord
    {
        public UInt16 PlatformId { get; set; }
        public UInt16 PlatformSpecificId { get; set; }
        public UInt16 LanguageId { get; set; }
        public UInt16 NameId { get; set; }
        public UInt16 Length { get; set; }
        public UInt16 Offset { get; set; }
    }

    public class NameTableReader
    {
        private BinaryReader reader;

        public NameTableReader(byte[] tableBuffer)
        {
            reader = new BinaryReader(new MemoryStream(tableBuffer));
        }

        public string? GetFontFamilyName()
        {
            var format = ReadBigEndianUInt16(reader);
            var count = ReadBigEndianUInt16(reader);
            var stringOffset = ReadBigEndianUInt16(reader);

            for (int i = 0; i < count; i++)
            {
                var platformId = ReadBigEndianUInt16(reader);
                var platformSpecificId = ReadBigEndianUInt16(reader);
                var languageId = ReadBigEndianUInt16(reader);
                var nameId = ReadBigEndianUInt16(reader);
                var length = ReadBigEndianUInt16(reader);
                var offset = ReadBigEndianUInt16(reader);

                if(nameId == 1 && platformId == 3 && platformSpecificId == 1)
                {
                    long position = stringOffset + offset;
                    reader.BaseStream.Seek(position, SeekOrigin.Begin);
                    byte[] nameBytes = reader.ReadBytes(length);
                    return Encoding.BigEndianUnicode.GetString(nameBytes);
                }
            }

            return null;
        }


        public static NameTable Read(BinaryReader reader)
        {
            var table = new NameTable
            {
                Format = ReadBigEndianUInt16(reader),
                Count = ReadBigEndianUInt16(reader),
                StringOffset = ReadBigEndianUInt16(reader)
            };

            table.Records = new NameRecord[table.Count];
            for (int i = 0; i < table.Count; i++)
            {
                table.Records[i] = new NameRecord
                {
                    PlatformId = ReadBigEndianUInt16(reader),
                    PlatformSpecificId = ReadBigEndianUInt16(reader),
                    LanguageId = ReadBigEndianUInt16(reader),
                    NameId = ReadBigEndianUInt16(reader),
                    Length = ReadBigEndianUInt16(reader),
                    Offset = ReadBigEndianUInt16(reader),
                };
            }


            var facenameRecord = table.Records.FirstOrDefault(r => r.NameId == 1 && r.PlatformId == 3 && r.PlatformSpecificId == 1);




            // Assuming ASCII encoding.
            var encoding = Encoding.ASCII;


            foreach (var record in table.Records)
            {
                if (record.NameId == 1)  // Name ID 6 is needed
                {
                    long position = table.StringOffset + record.Offset;
                    reader.BaseStream.Seek(position, SeekOrigin.Begin);
                    byte[] nameBytes = reader.ReadBytes(record.Length);
                    string name = encoding.GetString(nameBytes);

                    Console.WriteLine("Name: " + name);
                }
            }

            return table;
        }

        public static ushort ReadBigEndianUInt16(BinaryReader reader)
        {
            var data = reader.ReadBytes(2);
            Array.Reverse(data);
            return BitConverter.ToUInt16(data, 0);
        }
    }

    public class TypefaceViewModel : ViewModelBase
    {
        public TypefaceViewModel()
        {
            var fontManager = FontManager.Current;
            var fontFamily = fontManager.DefaultFontFamily;

            Typeface = new Typeface(fontFamily);

            if(fontManager.TryGetGlyphTypeface(Typeface, out var glyphTypeface))
            {
                GlyphTypeface = glyphTypeface;
            }
        }

        public TypefaceViewModel(Typeface typeface, IGlyphTypeface glyphTypeface)
        {
            Typeface = typeface;
            GlyphTypeface = glyphTypeface;

            var tag = new Tag('n', 'a', 'm', 'e');

            if(glyphTypeface.TryGetTable(tag, out var buffer))
            {
                var reader = new NameTableReader(buffer);

                FamilyName = reader.GetFontFamilyName();
            }

        }

        public Typeface Typeface { get; }
        public IGlyphTypeface GlyphTypeface { get; }
        public string Name => $"{Typeface.FontFamily.Name} {Typeface.Style} {Typeface.Weight} {Typeface.Stretch}";
        public string? FamilyName { get; set; }
    }
}
