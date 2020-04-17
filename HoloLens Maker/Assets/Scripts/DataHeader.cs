using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DataCollection
{

    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public class UnitSymbol : Attribute
    {
        readonly public string symbol;
        public UnitSymbol(string symbol)
        {
            this.symbol = symbol;
        }
    }

    public enum DataSourceType
    {
        FILE,
        SERIAL,
        NETWORK
    }

    public enum Unit
    {
        [UnitSymbol("?")]
        Unknown,

        [UnitSymbol("ft")]
        Foot,

        [UnitSymbol("in")]
        Inch,

        [UnitSymbol("lb")]
        Pound,

        [UnitSymbol("kip")]
        Kip,

        [UnitSymbol("psi")]
        Psi,

        [UnitSymbol("v")]
        Volts,

        [UnitSymbol("deg")]
        Degrees,

        [UnitSymbol("rad")]
        Radians,

        [UnitSymbol("sec")]
        Seconds,

        [UnitSymbol("m")]
        Meters,

        [UnitSymbol("k")]
        Kilograms,

        [UnitSymbol("a")]
        Ampere,

        [UnitSymbol("Θ")]
        Kelvin,

        [UnitSymbol("hz")]
        herts,

        [UnitSymbol("w")]
        watt,

        [UnitSymbol("p")]
        pascal,

        [UnitSymbol("j")]
        joule,

        [UnitSymbol("f")]
        farad,

        [UnitSymbol("Ω")]
        ohm,

        [UnitSymbol("T")]
        tesla,

        [UnitSymbol("lm")]
        luman,
    }

    public struct FloatRange
    {
        public FloatRange(float value, bool isFixed)
        {
            this.value = value;
            this.isFixed = isFixed;
        }

        [XmlText]
        public float value;

        [XmlAttribute]
        public bool isFixed;

        public static FloatRange Min => new FloatRange(float.MinValue, false);
        public static FloatRange Max => new FloatRange(float.MaxValue, false);
    }

    [XmlInclude(typeof(DataSourceType))]
    [XmlInclude(typeof(DataPointDefinition))]
    [XmlInclude(typeof(FloatRange))]
    [XmlRoot("SensorDefinition")]
    public class DataHeader
    {
        [XmlAttribute]
        public string Name;

        [XmlElement(IsNullable =true)]
        public float? DeltaTime;

        [XmlElement(IsNullable = false)]
        public DataSourceType SourceType;

        [XmlElement(IsNullable = false)]
        public string SourceLocation;

        [XmlAttribute]
        public string Delimeter;

        [XmlArray("DataPoints", IsNullable = false)]
        public DataPointDefinition[] DataPoints;

        [XmlAttribute(AttributeName = "schemaLocation", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
        public string schema = "http://www.w3schools.com SensorSchema.xsd";

        public static bool TryReadHeader(string filename, out DataHeader header)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(DataHeader));
                if(!File.Exists(filename))
                {
                    Console.WriteLine($"Unable to open file {filename}");
                    header = null;
                    return false;
                }
                using (var fp = File.OpenRead(filename))
                {
                    header = (DataHeader)serializer.Deserialize(fp);
                }
                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine($"Unable to parse header file:{e.InnerException}");
                header = null;
                return false;
            }
        }
    }

    [XmlInclude(typeof(Unit))]
    public class DataPointDefinition
    {
        [XmlElement]
        public string Name;

        [XmlElement(IsNullable = true)]
        public float? X;

        [XmlElement(IsNullable = true)]
        public float? Y;

        [XmlElement(IsNullable = true)]
        public float? Z;

        [XmlElement(IsNullable = true)]
        public uint? Index;

        [XmlElement(IsNullable = true)]
        public Unit? Units = Unit.Unknown;

        [XmlElement(IsNullable = true)]
        public FloatRange? Min = FloatRange.Max;

        [XmlElement(IsNullable = true)]
        public FloatRange? Max = FloatRange.Min;
    }

    public class InternalDataHeader
    {
        public static implicit operator InternalDataHeader(DataHeader header)
        {
            return new InternalDataHeader(header);
        }

        public InternalDataHeader(DataHeader header)
        {
            this.DataPoints = new InternalDataPointDefinition[header.DataPoints.Length];
            for(int i = 0; i < this.DataPoints.Length; i++)
            {
                this.DataPoints[i] = new InternalDataPointDefinition(header.DataPoints[i], (uint)i);
            }
            this.DataPoints = this.DataPoints.OrderBy(item => item.index).ToArray();
            this.Name = header.Name;
            this.DeltaTime = header.DeltaTime ?? .2f;
            this.SourceType = header.SourceType;
            this.SourceLocation = header.SourceLocation;
            this.delimeter = String.IsNullOrEmpty(header.Delimeter) ? ";" : header.Delimeter;

        }
        public InternalDataPointDefinition[] DataPoints;

        public string Name;

        public float DeltaTime = .2f;

        public DataSourceType SourceType;

        public string SourceLocation;

        public string delimeter = ";";
    }

    public class InternalDataPointDefinition
    {
        public static implicit operator InternalDataPointDefinition(DataPointDefinition point)
        {
            return new InternalDataPointDefinition(point, 0);
        }

        public InternalDataPointDefinition(DataPointDefinition point, uint index)
        {
            this.X = point.X ?? 0;
            this.Y = point.Y ?? 0;
            this.Z = point.Z ?? 0;
            this.Units = point.Units ?? Unit.Unknown;
            this.Min = point.Min?.value ?? float.MaxValue;
            this.isMinFixed = point.Min?.isFixed ?? false;
            this.Max = point.Max?.value ?? float.MinValue;
            this.isMaxFixed = point.Max?.isFixed ?? false;
            this.name = point.Name ?? "Unknown";
            this.index = point.Index ?? index;
        }
        public string name;
        public float X;
        public float Y;
        public float Z;
        public Unit Units;
        public float Min;
        public bool isMinFixed;
        public float Max;
        public bool isMaxFixed;
        public uint index;
    }
}
