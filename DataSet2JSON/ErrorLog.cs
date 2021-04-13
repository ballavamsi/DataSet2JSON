using System;
using System.IO;

namespace DataSet2JSON
{
    public static class ErrorLog
    {
        public static string logFilePath { get; set; } = AppContext.BaseDirectory;

        public static void WriteExLog(Exception ex)
        {
            FileStream fileStream = null;
            StreamWriter streamWriter = null;
            try
            {
                DirectoryInfo logDirInfo = null;
                logFilePath = logFilePath + $"error-{DateTime.Now.Day}-{DateTime.Now.Month}-{DateTime.Now.Year}.log"; 
                FileInfo logFileInfo = new FileInfo(logFilePath);
                logDirInfo = new DirectoryInfo(logFileInfo.DirectoryName);
                if (!logDirInfo.Exists)
                {
                    logDirInfo.Create();
                }
                if (!logFileInfo.Exists)
                {
                    fileStream = logFileInfo.Create();
                }
                else
                {
                    fileStream = new FileStream(logFilePath, FileMode.Append);
                }
                streamWriter = new StreamWriter(fileStream);
                streamWriter.WriteLine("******************************************************************");

                streamWriter.WriteLine("\nDATE: " + System.DateTime.Now);
                streamWriter.WriteLine("\nMESSAGE: " + ex.Message);

                streamWriter.WriteLine("\nSOURCE: " + ex.Source);
                streamWriter.WriteLine("\nINSTANCE: " + ex.InnerException);

                streamWriter.WriteLine("\nDATA: " + ex.Data);
                streamWriter.WriteLine("\nTARGETSITE: " + ex.TargetSite);

                streamWriter.WriteLine("\nSTACKTRACE: " + ex.StackTrace + "\n");
                streamWriter.WriteLine("\n******************************************************************");
            }
            finally
            {
                if (streamWriter != null)
                {
                    streamWriter.Close();
                }
            }
        }


        public static void WriteLogMessage(string message)
        {
            FileStream fileStream = null;
            StreamWriter streamWriter = null;
            try
            {
                DirectoryInfo logDirInfo = null;
                FileInfo logFileInfo = new FileInfo(logFilePath);
                logDirInfo = new DirectoryInfo(logFileInfo.DirectoryName);
                if (!logDirInfo.Exists)
                {
                    logDirInfo.Create();
                }
                if (!logFileInfo.Exists)
                {
                    fileStream = logFileInfo.Create();
                }
                else
                {
                    fileStream = new FileStream(logFilePath, FileMode.Append);
                }
                streamWriter = new StreamWriter(fileStream);
                streamWriter.WriteLine("******************************************************************");

                streamWriter.WriteLine("\nDATE: " + System.DateTime.Now);
                streamWriter.WriteLine("\nMESSAGE: " + message);
                streamWriter.WriteLine("\n******************************************************************");
            }
            finally
            {
                if (streamWriter != null)
                {
                    streamWriter.Close();
                }
            }
        }


    }
}
