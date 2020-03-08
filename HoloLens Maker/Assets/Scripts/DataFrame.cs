using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO.Ports;
using System.Threading.Tasks;
using SlowFileWriter;
namespace DataCollection
{
    /// <summary>
    /// Structure to hold data for the mapping of the data point to the channel
    /// </summary>
    internal class DataPointMapping
    {
        public string name;
        public Vector3 position;
        public Unit unit;
        internal float min;
        internal bool isMinFixed;
        internal float max;
        internal bool isMaxFixed;
    }

    /// <summary>
    /// Data point for a channel
    /// </summary>
    public class DataPoint
    {
        /// <summary>
        /// mapping of the value to channel related data
        /// </summary>
        private DataPointMapping mapping;

        internal DataPoint(DataPointMapping mapping)
        {
            this.mapping = mapping;
            this.minValue = mapping.min;
            this.maxValue = mapping.max;
        }

        /// <summary>
        /// value of the channel
        /// </summary>
        public float value;

        /// <summary>
        /// Minimum value that has been seen
        /// </summary>
        public float minValue;

        /// <summary>
        /// Maximum value that has been seen
        /// </summary>
        public float maxValue;

        /// <summary>
        /// Change in value compared to the last frame
        /// </summary>
        public float deltaLastFrame;

        /// <summary>
        /// Change in value since the last zeroing was called
        /// </summary>
        public float deltaLastZero;

        /// <summary>
        /// channel number
        /// </summary>
        public Vector3 position => mapping.position;

        /// <summary>
        /// name of the channel
        /// </summary>
        public string sensorName => mapping.name;

        /// <summary>
        /// unit assigned to the channel
        /// </summary>
        public Unit unit => mapping.unit;
    }

    /// <summary>
    /// Channel data for aspecific time frame
    /// </summary>
    public class DataFrame
    {
        /// <summary>
        /// values provided in the data frame
        /// </summary>
        public DataPoint[] values;

        /// <summary>
        /// Time the frame was captured
        /// </summary>
        public DateTime frameTime;
    }

    /// <summary>
    /// Parser that takes data from a data source and produces data frames
    /// </summary>
    public class Parser : IDisposable
    {
        /// <summary>
        /// The current values provided from the data source
        /// </summary>
        public DataFrame currentInfo;

        private DataFrame ZeroFrame;

        /// <summary>
        /// Queue of frames that have been read
        /// </summary>
        private ConcurrentQueue<DataFrame> updates;

        /// <summary>
        /// Thread to handle reading of data
        /// </summary>
        private Thread fileParseingThread;

        /// <summary>
        /// Control to stop the data reading thread
        /// </summary>
        private bool isStopped = false;

        /// <summary>
        /// Types of units this recognizes
        /// </summary>
        public static Dictionary<string, Unit> UnitTypeParseMap
        {
            get {
                if(pUnitTypeParseMap == null)
                {
                    pUnitTypeParseMap = new Dictionary<string, Unit>();
                    var enumNames = Enum.GetNames(typeof(Unit));
                    foreach (var name in enumNames)
                    {
                        UnitSymbol[] symbols = typeof(Unit).GetField(name).GetCustomAttributes(false).OfType<UnitSymbol>().ToArray();
                        foreach (var symbol in symbols)
                        {
                            UnitTypeParseMap.Add($"[{symbol.symbol}]", (Unit)Enum.Parse(typeof(Unit), name));
                        }
                    }
                }
                return pUnitTypeParseMap;
            }
        }

        private static Dictionary<string, Unit> pUnitTypeParseMap;


        private IDataSource dataSource;

        /// <summary>
        /// Create a new data parser
        /// </summary>
        /// <param name="filename"></param>
        public Parser(IDataSource dataSource)
        {
            updates = new ConcurrentQueue<DataFrame>();
            fileParseingThread = new Thread(new ThreadStart(parseInput));
            this.dataSource = dataSource;
            this.currentInfo = new DataFrame() { values = new DataPoint[0], frameTime = DateTime.MinValue };
        }

        /// <summary>
        /// start parsing data from the stream
        /// </summary>
        public void start()
        {
            PopulateFromHeader();
            fileParseingThread.Start();
        }

        /// <summary>
        /// updates the current frame to the most up to date frame that the parser has read
        /// </summary>
        public void UpdateBeforeDraw()
        {
            DataFrame newFrame;
            while (updates.TryDequeue(out newFrame))
            {
                currentInfo = newFrame;
            }
        }

        public void Zero()
        {
            this.ZeroFrame = currentInfo;
        }

        /// <summary>
        /// stop the parser from reading the data
        /// </summary>
        public void stop()
        {
            isStopped = true;
            fileParseingThread.Abort();
        }

        /// <summary>
        /// Dispose of the object
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose of the object
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (fileParseingThread != null)
                {
                    fileParseingThread.Abort();
                    fileParseingThread = null;                   
                }
            }
        }

        private void PopulateFromHeader()
        {
            this.header = dataSource.readInternalDataHeader();
            mapping = new DataPointMapping[this.header.DataPoints.Length];
            for(int i = 0; i < this.header.DataPoints.Length; i++)
            {
                mapping[i] = new DataPointMapping();
                mapping[i].name = this.header.DataPoints[i].name;
                mapping[i].unit = this.header.DataPoints[i].Units;
                mapping[i].position = new Vector3(this.header.DataPoints[i].X, this.header.DataPoints[i].Y, this.header.DataPoints[i].Z);
                mapping[i].min = this.header.DataPoints[i].Min;
                mapping[i].isMinFixed = this.header.DataPoints[i].isMinFixed;
                mapping[i].max = this.header.DataPoints[i].Max;
                mapping[i].isMaxFixed = this.header.DataPoints[i].isMaxFixed;
            }
        }

        InternalDataHeader header;
        DataPointMapping[] mapping;

        private void parseInput()
        {
            
            //keep updating data until the object is disposed
            DataFrame lastFrame = null;
            while (!isStopped)
            {
                string[] newInput = dataSource.readLine().Split(new[]{ header.delimeter },StringSplitOptions.None);
                DataFrame newFrame = new DataFrame();
                newFrame.values = new DataPoint[mapping.Length];
                newFrame.frameTime = DateTime.Now;
                for (int i = 0; i < mapping.Length; i++)
                {
                    
                    if (newInput.Length <= header.DataPoints[i].index)
                        continue;
                    bool parsed = float.TryParse(newInput[header.DataPoints[i].index], out float value);
                    if (parsed == false)
                    {
                        newFrame.values[i] = new DataPoint(mapping[i]);
                        newFrame.values[i].deltaLastFrame = 0;
                        newFrame.values[i].deltaLastZero = 0;
                    }
                    if (!mapping[i].isMaxFixed)
                        mapping[i].max = mapping[i].max > value ? mapping[i].max : value;
                    
                    if (!mapping[i].isMinFixed)
                        mapping[i].min = mapping[i].min < value ? mapping[i].min : value;

                    newFrame.values[i] = new DataPoint(mapping[i]);
                    newFrame.values[i].value = value;
                    newFrame.values[i].deltaLastFrame = lastFrame != null ? newFrame.values[i].value - lastFrame.values[i].value : 0;
                    if (ZeroFrame != null && ZeroFrame.values.Length > i)
                    {
                        newFrame.values[i].deltaLastZero = newFrame.values[i].value - ZeroFrame.values[i].value;
                    }
                    else
                    {
                        newFrame.values[i].deltaLastZero = 0;
                    }
                    
                }
                updates.Enqueue(newFrame);
                if (ZeroFrame == null)
                    ZeroFrame = newFrame;
                lastFrame = newFrame;
                Thread.Sleep((int)(header.DeltaTime * 500));
            }
        }

        public DataHeader GenerateHeaderFromData()
        {
            DataHeader newHeader = new DataHeader();
            newHeader.Name = header.Name;
            newHeader.SourceLocation = header.SourceLocation;
            newHeader.SourceType = header.SourceType;
            newHeader.DeltaTime = header.DeltaTime;
            newHeader.Delimeter = header.delimeter;
            newHeader.DataPoints = new DataPointDefinition[mapping.Length];
            for(int i = 0; i < mapping.Length; i++)
            {
                newHeader.DataPoints[i] = new DataPointDefinition();
                newHeader.DataPoints[i].Index = header.DataPoints[i].index;
                newHeader.DataPoints[i].Max = new FloatRange(mapping[i].max, mapping[i].isMaxFixed);
                newHeader.DataPoints[i].Min = new FloatRange(mapping[i].min, mapping[i].isMinFixed);
                newHeader.DataPoints[i].Name = header.DataPoints[i].name;
                newHeader.DataPoints[i].Units = header.DataPoints[i].Units;
                newHeader.DataPoints[i].X = mapping[i].position.x;
                newHeader.DataPoints[i].Y = mapping[i].position.y;
                newHeader.DataPoints[i].Z = mapping[i].position.z;
            }
            return newHeader;
        }

    }

    public interface IDataSource
    {
        string readLine();
        InternalDataHeader readInternalDataHeader();
    }

    public class FakeDataSource : IDataSource, IDisposable
    {
        private string filename;
        private int latency;
        private Thread TestFileReader;
        ConcurrentQueue<string> lines = new ConcurrentQueue<string>();
        DataHeader header;

        public FakeDataSource(string filename, DataHeader header) : base()
        {
            this.filename = filename;
            this.latency = (int)((header.DeltaTime ?? .2f) * 1000);
            this.header = header;
            TestFileReader = new Thread(new ThreadStart(BackgroundEnumlateFileRead));
            TestFileReader.Start();
            readLine();
            readLine();
            readLine();
            readLine();
            readLine();
            readLine();
            readLine();
        }

        public FakeDataSource(string filename) : base()
        {
            this.filename = filename;
            TestFileReader = new Thread(new ThreadStart(BackgroundEnumlateFileRead));
            TestFileReader.Start();
            this.header = new DataHeader();
            this.header.SourceType = DataSourceType.FILE;
            this.header.SourceLocation = filename;
            readLine(); //DASYLab - V 11.00.00
            string lineIn = readLine(); //Worksheet name: 6by10beamlayout
            string[] splits = lineIn.Split(':');
            if (splits.Length > 1)
                this.header.Name = splits[1];

            readLine(); //Recording date     : 7/1/2016,  4:52:39 PM

            readLine(); //Block length       : 2

            lineIn = readLine(); //Delta              : 0.2 sec.
            splits = lineIn.Split(':');
            if (splits.Length > 1)
            {
                this.header.DeltaTime = Convert.ToSingle(splits[1].Trim().Split(' ')[0]);
            }


            lineIn = readLine(); //Number of channels : 16
            splits = lineIn.Split(':');
            int numChanels = 0;
            if(splits.Length > 1)
                numChanels = Convert.ToInt32(splits[1]);

            // read the channel information
            lineIn = readLine();
            Regex inputMappingRegex = new Regex("(?<Name>.*?)(?<Unit>\\[.*?\\])?;");
            MatchCollection inputMatches = inputMappingRegex.Matches(lineIn);

            numChanels = Math.Max(inputMatches.Count, numChanels);
            this.header.DataPoints = new DataPointDefinition[numChanels];
            for(int i = 0; i < numChanels; i++)
            {
                this.header.DataPoints[i] = new DataPointDefinition();
            }

            int j = 0;
            foreach (Match match in inputMatches)
            {
                this.header.DataPoints[j].Name = match.Groups["Name"].Value;
                if (Parser.UnitTypeParseMap.ContainsKey(match.Groups["Unit"].Value))
                    this.header.DataPoints[j].Units = Parser.UnitTypeParseMap[match.Groups["Unit"].Value];
                j++;
            }
        }

        private void BackgroundEnumlateFileRead()
        {
            
            using (var fp = new StreamReader(filename))
            {
                lines.Enqueue(fp.ReadLine());
                lines.Enqueue(fp.ReadLine());
                lines.Enqueue(fp.ReadLine());
                lines.Enqueue(fp.ReadLine());
                lines.Enqueue(fp.ReadLine());
                lines.Enqueue(fp.ReadLine());
                lines.Enqueue(fp.ReadLine());
                while (!fp.EndOfStream)
                {
                    lines.Enqueue(fp.ReadLine());
                    Thread.Sleep(latency);
                }
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(Boolean disposing)
        {
            if(disposing == true && TestFileReader != null)
            {
                TestFileReader.Abort();
                TestFileReader = null;
            }
        }

        public String readLine()
        {
            string line;
            while (!lines.TryDequeue(out line))
            {
                Thread.Sleep(this.latency / 2);
            }
            return line;
        }

        public InternalDataHeader readInternalDataHeader()
        {
            return header;
        }

        public DataHeader readDataHeader()
        {
            return header;
        }
    }

    public class SerialDataSource : IDataSource, IDisposable
    {
        SerialPort port;
        DataHeader header;

        public SerialDataSource(string portName, DataHeader header)
        {
            this.header = header;
            port =new SerialPort(portName, 9600,Parity.None,8,StopBits.One);
            port.Open();
        }

        public SerialDataSource(string portName)
        {
            this.header = null;
            port =new SerialPort(portName, 9600,Parity.None,8,StopBits.One);
            port.Open();
        }


        /// <summary>
        /// Dispose of the object
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose of the object
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if(port != null)
                {
                    port.Close();
                    port.Dispose();
                }

            }
        }

        public void GenerateHeader( string sheetName, string[] names, Unit[] units, string delimeter = ";")
        {

            DateTime start = DateTime.Now;
            string sampleInput = readLine();
            readLine();
            readLine();
            DateTime end = DateTime.Now;
            float deltaTime = (float)(end - start).TotalSeconds / 3;

            int channelCount = sampleInput.Split(new[] { delimeter }, StringSplitOptions.None).Length;
            DataHeader header = new DataHeader();
            header.Delimeter = delimeter;
            header.DeltaTime = deltaTime;
            header.Name = sheetName;
            header.SourceLocation = port.PortName;
            header.SourceType = DataSourceType.SERIAL;
            header.DataPoints = new DataPointDefinition[channelCount];
            for(int i = 0; i < channelCount; i++)
            {
                header.DataPoints[i] = new DataPointDefinition();
                if (names != null && names.Length < i)
                    header.DataPoints[i].Name = names[i];
                if (units != null && units.Length < i)
                    header.DataPoints[i].Units = new Unit?(units[i]);
            }
        }

        /// <summary>
        /// read a line of data points
        /// </summary>
        /// <returns></returns>
        public String readLine()
        {
            string line = port.ReadLine();
            while (string.IsNullOrEmpty(line))
                line = port.ReadLine();
            return line;
        }

        public InternalDataHeader readInternalDataHeader()
        {
            if (header == null)
                GenerateHeader("Unknown source name", null, null);
            return this.header;
        }

        public DataHeader readDataHeader()
        {
            if (header == null)
                GenerateHeader("Unknown source name", null, null);
            return this.header;
        }
    }

    public class NetworkDataSource
    {

    }
}