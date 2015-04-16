using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.IO;
using UnityEngine;

namespace Assets.Scripts.Shared
{

    /// <summary>
    /// A Logging class implementing the Singleton pattern and an internal Queue to be flushed perdiodically
    /// http://www.bondigeek.com/blog/2011/09/08/a-simple-c-thread-safe-logging-class/
    /// </summary>
    public class LogWriter
    {
        public static LogWriter TextEntryLog = new LogWriter("TextEntry");
        public static LogWriter HeadAngleLog = new LogWriter("HeadAngle");
		public static LogWriter HSV = new LogWriter("HSV");
        public static LogWriter AmountOfReality = new LogWriter("AmountOfReality");


        private Queue<String> logQueue;
        private string logDir = "";
        private string logFile = DateTime.Today.ToString("d").Replace('/', '-');
        private int maxLogAge = 3;
        private int queueSize = 10;
        private DateTime LastFlushed = DateTime.Now;

        private FileStream fs;
        private StreamWriter log;

        /// <summary>
        /// Private constructor to prevent instance creation
        /// </summary>
        public LogWriter(string fileName)
        {

            Boolean gotFile = false;
            int fileCount = 0;

            while (!gotFile)
            {
                try
                {
                    logFile = DateTime.Now.ToString("yyyyMMdd") + "--" + DateTime.Now.ToString("hh:mm:sstt") + "-" + fileName + "-" + fileCount + ".txt";
                    logFile = logFile.Replace(':', '-');

                    while (File.Exists(logFile))
                    {
                        fileCount++;
                        logFile = DateTime.Today.ToString("d").Replace('/', '-') + "-" + fileName + "-" + fileCount + ".txt";
                        logFile = logFile.Replace(':', '-');
                    }

                    fs = File.Open(logFile, FileMode.Append, FileAccess.Write);
                    log = new StreamWriter(fs);
                    log.AutoFlush = true;
                    logQueue = new Queue<String>();

                    gotFile = true;
                }
                catch (Exception e)
                {
                    Debug.LogError("Problem logging to file " + logFile + e.ToString());
                    fileCount++;
                    gotFile = false;
                }
            }
        }

        /// <summary>
        /// The single instance method that writes to the log file
        /// </summary>
        /// <param name="message">The message to write to the log</param>
        public void WriteToLog(string message)
        {
            // Lock the queue while writing to prevent contention for the log file
            lock (logQueue)
            {
                // instead of periodically flushing, write every time...
                log.WriteLine(string.Format("{0}\t{1}", DateTime.Now.ToString("hh:mm:ss.fff tt"), message));
                

                // Create the entry and push to the Queue
                //Log logEntry = new Log(message);
                //logQueue.Enqueue(logEntry);

                // If we have reached the Queue Size then flush the Queue
                //if (logQueue.Count >= queueSize || DoPeriodicFlush())
                //{
                //    FlushLog();
                //}
            }
        }

        public void Close()
        {
            log.Close();
        }

        private bool DoPeriodicFlush()
        {
            TimeSpan logAge = DateTime.Now - LastFlushed;
            if (logAge.TotalSeconds >= maxLogAge)
            {
                LastFlushed = DateTime.Now;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Flushes the Queue to the physical log file
        /// </summary>
        public void FlushLog()
        {
            lock (logQueue)
            {
                while (logQueue.Count > 0)
                {
                    //Log entry = logQueue.Dequeue();
                    //log.WriteLine(string.Format("{0},\t{1}", entry.LogTime, entry.Message));
                }
            }
        }

        ~LogWriter()
        {
            FlushLog();
        }

    }

    /// <summary>
    /// A Log class to store the message and the Date and Time the log entry was created
    /// </summary>
    //public class Log
    //{
    //    public string Message { get; set; }
    //    public string LogTime { get; set; }
    //    public string LogDate { get; set; }

    //    public Log(string message)
    //    {
    //        Message = message;
    //        LogDate = DateTime.Now.ToString("yyyy-MM-dd");
    //        LogTime = DateTime.Now.ToString("hh:mm:ss.fff tt");
    //    }
    //}
}
