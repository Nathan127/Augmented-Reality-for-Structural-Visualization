using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
namespace DataCollection
{

    public enum Unit
    {
        Unknown,
        Foot,
        Inch,
        Pound,
        Kip,
        Psi,
    }

    /// <summary>
    /// Structure to hold data for the mapping of the data point to the channel
    /// </summary>
    internal class DataPointMapping
    {
        public string name;
        public float position;
        public Unit unit;
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
        public float position => mapping.position;

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
        Dictionary<string, Unit> UnitTypeParseMap = new Dictionary<string, Unit>()
        {
            {
                "[in]",
                Unit.Inch
            }
        };

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

        private void parseInput()
        {
            //read the header
            dataSource.readLine();
            dataSource.readLine();
            DateTime startTime = DateTime.Parse(String.Join(":", dataSource.readLine().Split(':').Skip(1).ToArray()));
            dataSource.readLine();
            dataSource.readLine();
            int numChanels = Convert.ToInt32(dataSource.readLine().Split(':')[1]);

            // read the channel information
            Regex inputMappingRegex = new Regex("(?<Name>.*?)(?<Unit>\\[.*?\\])?;");
            MatchCollection inputMatches = inputMappingRegex.Matches(dataSource.readLine());

            DataPointMapping[] mappings = new DataPointMapping[inputMatches.Count];
            int j = 0;
            foreach (Match match in inputMatches)
            {  
                //read the name and units of each channel
                mappings[j] = new DataPointMapping();
                mappings[j].name = match.Groups["Name"].Value;


                //check known units and assign it if known
                Group unitGroup = match.Groups["Unit"];
                if (UnitTypeParseMap.ContainsKey(unitGroup.Value))
                {
                    mappings[j].unit = UnitTypeParseMap[unitGroup.Value];
                }
                mappings[j].position = j;
                j++;
            }

            //keep updating data until the object is disposed
            DataFrame lastFrame = null;
            while (!isStopped)
            {
                string[] newInput = dataSource.readLine().Split(';');
                DataFrame newFrame = new DataFrame();
                newFrame.values = new DataPoint[numChanels];
                newFrame.frameTime = startTime.Add(TimeSpan.Parse(newInput[1]));
                for (int i = 0; i < numChanels; i++)
                {

                    newFrame.values[i] = new DataPoint(mappings[i + 2]);
                    newFrame.values[i].value = Convert.ToSingle(newInput[i + 2]);
                    newFrame.values[i].deltaLastFrame = lastFrame != null ? newFrame.values[i].value - lastFrame.values[i].value : 0;
                    newFrame.values[i].deltaLastZero = ZeroFrame != null ? newFrame.values[i].value - ZeroFrame.values[i].value : 0;
                    newFrame.values[i].maxValue = lastFrame != null ? lastFrame.values[i].maxValue : float.MinValue;
                    newFrame.values[i].minValue = lastFrame != null ? lastFrame.values[i].minValue : float.MaxValue;
                    newFrame.values[i].maxValue = newFrame.values[i].value > lastFrame.values[i].maxValue ? newFrame.values[i].value : lastFrame.values[i].maxValue;
                    newFrame.values[i].minValue = newFrame.values[i].value < lastFrame.values[i].minValue ? newFrame.values[i].value : lastFrame.values[i].minValue;
                }
                updates.Enqueue(newFrame);
                if (ZeroFrame == null)
                    ZeroFrame = newFrame;
                lastFrame = newFrame;
            }
        }
    }

    public interface IDataSource
    {
        string readLine();
    }

    public class FakeDataSource : IDataSource, IDisposable
    {
        private string filename;
        private int latency;
        private Thread TestFileReader;
        ConcurrentQueue<string> lines = new ConcurrentQueue<string>();

        public FakeDataSource(string filename, int latency) : base()
        {
            this.filename = filename;
            this.latency = latency;

            TestFileReader = new Thread(new ThreadStart(testReadFile));
            TestFileReader.Start();
        }

        private void testReadFile()
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
                Thread.Sleep(100);
            }
            return line;
        }
    }
}