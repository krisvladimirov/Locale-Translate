using System.Collections.Generic;
using System.Globalization;
using CsvHelper;
using System.IO;
using System;
using System.Text.RegularExpressions;

namespace LocalisationTranslator
{
    /// <summary>
    /// Handles the Read/Write of files from the App
    /// </summary>
    public static class Utils
    {
        // The output directory of the App
        private static string outputDir = "..\\output";

        // The sub-directory in which to save the outputs
        private static string whereToSave;
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static bool CheckOutputPath()
        {
            // Check if output directory exists
            // If so check how many sub-directories exists
            // For each run of the App create a new sub directory
            var exists = Directory.Exists(outputDir);
            Console.WriteLine(exists);
            if (!exists)
            {
                // Creates the main output directory
                Directory.CreateDirectory(outputDir);
                // Creates the sub-directory for each run
                Utils.whereToSave = outputDir + "\\run-1";
                Directory.CreateDirectory(whereToSave);
            }
            else
            {
                var numberOfRuns = 0;
                // Check how many sub-directories are present
                List<string> paths = new List<string>(Directory.EnumerateDirectories(Utils.outputDir));
                foreach(var dir in paths)
                {
                    if (Regex.IsMatch(dir, @"\\run-[0-9]"))
                    {
                        numberOfRuns++;
                    }
                }
                Utils.whereToSave = Utils.outputDir + $"\\run-{numberOfRuns + 1}";
                Directory.CreateDirectory(whereToSave);

            }

            return true;
        }

        /// <summary>
        /// Loads the CSV
        /// </summary>
        /// <returns>
        /// True on successful read (may include bad data or missing fields), false if the header were not validated or an internal error occured.
        /// </returns>
        public static bool ReadLocalisation()
        {
            var skipRow = false;
            var invalid = false;
            var lastErrorLine = -1;
             using (var reader = new StreamReader(App.settings.FileStructure.Path))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {

                csv.Configuration.HasHeaderRecord = true;

                // Configures a handler for missing fields
                csv.Configuration.MissingFieldFound = (headerNames, fieldIndex, context) =>
                {
                    skipRow = true;
                    App.errors.Add(new Log(Occurance.WhenReadingMissingData, context.Row, context.HeaderRecord[fieldIndex], fieldIndex));

                    // Omits lines which are already added, we don't want duplicates
                    if (lastErrorLine != context.Row)
                    {
                        App.erroredLines.Add(context.Row);
                        lastErrorLine = context.Row;
                    }
                };

                // Configures a handler for bad data
                csv.Configuration.BadDataFound = (context) =>
                {
                    skipRow = true;
                    App.errors.Add(new Log(Occurance.WhenReadingBadData, context.Row));

                    // Omits lines which are already added, we don't want duplicates
                    if (lastErrorLine != context.Row)
                    {
                        App.erroredLines.Add(context.Row);
                        lastErrorLine = context.Row;
                    }
                };

                csv.Configuration.HasHeaderRecord = true;
                csv.Read();

                // Attempts to read the header for validation
                if (csv.ReadHeader())
                {
                    var fileHeaders = csv.Context.HeaderRecord;
                    ushort i = 0;

                    // If the file is empty
                    if (fileHeaders.Length == 0)
                    {
                        invalid = true;
                        App.errors.Add(new Log(Occurance.NoData));
                    }
                    else if (fileHeaders.Length != App.settings.FileStructure.Headers.Count)
                    {
                        // If the length of the headers in the files and the provided headers does not match
                        invalid = true;
                        if (fileHeaders.Length > App.settings.FileStructure.Headers.Count)
                        {
                            App.errors.Add(new Log(Occurance.WhenHeadersAreMore) { Token = App.settings.FileStructure.Path });
                        }
                        else
                        {
                            App.errors.Add(new Log(Occurance.WhenHeadersAreLess) { Token = App.settings.FileStructure.Path });
                        }
                    }

                    // If the header do not match
                    while (!invalid && i < fileHeaders.Length)
                    {
                        if (fileHeaders[i] != App.settings.FileStructure.Headers[i])
                        {
                            invalid = true;
                            App.errors.Add(new Log(Occurance.WhenHeadersAreNotMatching, 0, fileHeaders[i], i));
                        }
                        i++;
                    }
                }

                if (!invalid)
                {
                    // Attempts to read row by row
                    try
                    {
                        while (csv.Read())
                        {
                            var record = csv.GetRecord<dynamic>();
                            if (!skipRow)
                            {
                                App.records.Add(record);
                            }
                            skipRow = false;
                            App.totalRecords++;
                        }
                        return true;
                    }
                    catch (Exception ex)
                    {
                        // TODO: Should I add erroredLines here as well?
                        App.errors.Add(new Log(Occurance.CSVHelperThrow) { Message = ex.Message });
                        return false;
                    }

                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Saves a list of records
        /// </summary>
        /// <param name="filename">The filename or where to save</param>
        /// <param name="data">The data/records to be saved</param>
        /// <returns>
        /// True if the records have been dumped successfully, false otherwise
        /// </returns>
        public static bool DumpRecords(List<dynamic> data, string filename)
        {
            using (var writer = new StreamWriter(Utils.whereToSave + $"\\{filename}"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                try
                {
                    csv.WriteRecords(data);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }   
            }
        }

        /// <summary>
        /// Dump caught log in the order it was caught.
        /// </summary>
        /// <param name="appLogs">The logs to dump</param>
        /// <param name="filename">The file where to dump the logs</param>
        /// <returns>
        /// True if the logs have been dumped successfully, false otherwise
        /// </returns>
        public static bool DumpLog(List<Log> appLogs, string filename)
        {
            using (var writer = new StreamWriter(Utils.whereToSave + $"\\{filename}"))
            {
                try
                {
                    foreach (var log in appLogs)
                    {
                        writer.WriteLine(log.Message);
                    }
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
    }
}