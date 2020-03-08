using Amazon.Extensions.NETCore.Setup;
using Amazon.Translate;
using CsvHelper;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LocalisationTranslator
{
    /// <summary>
    /// 
    /// </summary>
    public static class App
    {
        // List of log messages
        public readonly static List<Log> errors = new List<Log>();

        // List of log messages regarding keys that contain HTML and/or ICU data
        public readonly static List<Log> specialData = new List<Log>();

        // List of records each having the source and target text for easy comparison
        public static List<dynamic> comparisonData;

        // The configuration builder
        public static IConfiguration config;

        // The AWS options
        public static AWSOptions awsOptions;

        // The application settings
        public static AppSettings settings;

        // Service shipping/receiving the translation requests to and from AWS Translate
        public static TranslateService translateService;

        // All successfully read records as ExpandoObjects
        public readonly static List<dynamic> records = new List<dynamic>();

        // A deep copy of all successfully read records as ExpandoObjects
        public readonly static List<dynamic> copiedRecords = new List<dynamic>();

        // A list of records containing HTML and/or ICU data that will be saved separately either translated or in original form
        public readonly static List<int> separatedRecords = new List<int>();

        // The lines at which errors were encountered while reading from the csv file
        public readonly static List<int> erroredLines = new List<int>();

        // The total records including bad ones
        public static short totalRecords = 0;

        // Whether or not to terminate the main Thread
        public static bool outcome = true;

        // The Settings section in the AppSettings.json
        public static string SETTINGS_SECTION = "Settings";
        public static string ERROR_LOG_FILE = "error_log.txt";
        public static string ICU_HTML_LOG_FILE = "icu_html.txt";

        /// <summary>
        /// Performs all required steps based on the settings
        /// </summary>
        public static bool Process()
        {
            InitializeOptions();
            ReadLocalisation();
            ProcessRequests();
            if (App.settings.Options.Validation.ICUandHTML.SeparateFile)
            {
                DumpSeparatedRecords();
                ClearSeparatedRecords();
            }
            if (App.specialData.Count > 0)
            {
                DumpLog(specialData, App.ICU_HTML_LOG_FILE);
            }
            if (settings.Options.ComparisonFile)
            {
                MakeComparison();
            }
            SaveTranslatedRecords();

            return true;
        }

        /// <summary>
        /// Initializes the application and its services
        /// </summary>
        public static void InitializeOptions()
        {
            App.config = GetConfiguration();
            App.awsOptions = App.config.GetAWSOptions();
            App.settings = App.config.GetSection(SETTINGS_SECTION).Get<AppSettings>();
            App.translateService = new TranslateService(awsOptions.CreateServiceClient<IAmazonTranslate>());

        }

        /// <summary>
        /// Makes the configuration for this program
        /// </summary>
        /// <returns>The built configuration</returns>
        public static IConfiguration GetConfiguration()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            return config;
        }

        /// <summary>
        /// Loads the CSV
        /// </summary>
        /// <returns>'True' on successful read (may include bad data or missing fields), 'False' if the header were not validated or an internal error occured.</returns>
        public static bool ReadLocalisation()
        {
            var skipRow = false;
            var invalid = false;
            var lastErrorLine = -1;
            using (var reader = new StreamReader(settings.FileStructure.Path))
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
                            errors.Add(new Log(Occurance.WhenHeadersAreNotMatching, 0, fileHeaders[i], i));
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
                            totalRecords++;
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
        /// Starts shipping request to Amazon Translate
        /// </summary>
        public static void ProcessRequests()
        {
            var count = 0;
            // var translate = true;
            using (var progress = new ProgressBar())
            {
                foreach (var expando in records)
                {
                    var translate = true;
                    var data = (IDictionary<String, Object>)expando;
                    var text = (string)data[settings.FileStructure.TextHeader];
                    if (!string.IsNullOrEmpty(text))
                    {

                        // Locate strings containing HTML
                        // Locate strings containing ICU
                        // There index would be the count variable
                        if (Regex.IsMatch(text, @"(?<=\{)[^}]*(?=\})") || !Regex.IsMatch(text, "^((?!\\<(|\\/)[a-z][a-z0-9]*>).)*$"))
                        {
                            var token = (string)data[settings.FileStructure.KeyHeader];
                            Occurance occurance;

                            if (!settings.Options.Validation.ICUandHTML.Translate)
                            {

                                translate = false;
                                if (!settings.Options.Validation.ICUandHTML.SeparateFile)
                                {
                                    occurance = Occurance.NotTranslatedSameFile;
                                    // Update the language code to match the provided target
                                    data[settings.FileStructure.LanguageHeader] = settings.Target;
                                }
                                else
                                {
                                    occurance = Occurance.NotTranslatedSeparateFile;
                                    // Add the index of the records that would be extracted
                                    App.separatedRecords.Add(count);
                                }

                            }
                            else
                            {

                                if (!settings.Options.Validation.ICUandHTML.SeparateFile)
                                {
                                    occurance = Occurance.TranslatedSameFile;
                                    // Update the language code to match the provided target
                                    data[settings.FileStructure.LanguageHeader] = settings.Target;
                                }
                                else
                                {
                                    occurance = Occurance.TranslatedSeparateFile;
                                    // Add the index of the records that would be extracted
                                    App.separatedRecords.Add(count);
                                }

                            }

                            var line = FindOriginalLine(count);
                            App.specialData.Add(new Log(occurance, line, token));
                        }


                        if (translate)
                        {
                            try
                            {
                                // var task = translateService.TranslateText(text, settings.Source, settings.Target);
                                // task.Wait();
                                // var translatedText = task.Result.TranslatedText;
                                // Overwrite the language code
                                data[settings.FileStructure.LanguageHeader] = settings.Target;
                                // // Overwrite the value of the key containing the text with the translated version
                                // data[settings.FileStructure.TextHeader] = task.Result.TranslatedText;
                            }
                            catch (Exception ex)
                            {
                                var line = FindOriginalLine(count);
                                App.errors.Add(new Log(Occurance.WhenTranslating, line, ex.Message));
                            }
                        }
                    }

                    progress.Report(count);
                    count++;
                }
            }
        }

        /// <summary>
        /// Attempts to locate the original line of a record that has either triggered an error or has been skipped
        /// </summary>
        /// <param name="currentLine">The line in the already read localisation, excluding errored lines, i.e. records, 
        /// at which an error while translating has occured or the line was skipped while translating due to validation</param>
        /// <returns>
        /// The original line at which the error or skipped line was placed before reading the localisation file.
        /// </returns>
        public static int FindOriginalLine(int currentLine)
        {
            // NOTE
            // currentLine is zero based
            // while errors is not zero based the header is also taken into account therefore if an error occures
            // while reading the first record it will be at line 2 not 1
            // that's why it is currentLine + 2


            // Represent how much needs to be added to a line to map to its original placement in the CSV file
            var positiveOffset = 0;
            if (App.erroredLines.Count == 0)
            {
                return currentLine + 2;
            }

            // .First() should never throw an error as we are checking that erroredLines has elements in it in the previous line
            // Handles the case when the first line of the error is greater than the currentLine (index from records), 
            // therefore the error has happened after and no change in order has taken place
            if (App.erroredLines.First() > currentLine + 2)
            {
                return currentLine + 2;
            }

            // .Last() should never throw an error as we are checking that erroredLines has elements in it in the previous line
            // Handles the case when the currentLine is bigger than the last recorded error,
            // therefore the original line will always be the currentLine + 2 + the total amount of errors
            if (App.erroredLines.Last() <= currentLine + 2)
            {
                return currentLine + 2 + App.erroredLines.Count;
            }

            var errIndex = App.erroredLines.FindIndex(x => x >= currentLine + 2); // If no element is matched return -1
            var erroredLine = App.erroredLines[errIndex];

            // Handles cases when there are consecutive errored lines
            if (erroredLine == currentLine + 2 && errIndex != App.erroredLines.Count - 1)
            {
                var foundLine = false;
                while (!foundLine && errIndex < App.erroredLines.Count)
                {
                    errIndex++;
                    var nextErroredLine = App.erroredLines[errIndex];
                    if (nextErroredLine - erroredLine > 1)
                    {
                        positiveOffset = errIndex;
                        foundLine = true;
                    }
                    else
                    {
                        erroredLine = nextErroredLine;
                    }
                }
            }
            else
            {   
                // Handles cases when the consecutive errored lines have been passed by the currentLine
                positiveOffset = errIndex;
            }

            return currentLine + 2 + positiveOffset;
        }

        public static bool MakeComparison()
        {
            // TODO: Predefine size
            App.comparisonData = new List<dynamic>();

            return true;
        }

        /// <summary>
        /// Saves the separated records into a csv file
        /// </summary>
        public static bool DumpSeparatedRecords()
        {
            using (var writer = new StreamWriter("separated_records.csv"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {

                try
                {
                    // Picks up only the records that are separated, index is saved during translation
                    foreach (var index in App.separatedRecords)
                    {
                        var record = App.records[index];
                        csv.WriteRecord(record);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
            throw new NotImplementedException();
        }

        /// <summary>
        /// Saves the translated records
        /// </summary>
        /// <returns>
        /// True is successful, false otherwise
        /// </returns>
        public static bool SaveTranslatedRecords()
        {
            using (var writer = new StreamWriter("translated.csv"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                try
                {
                    csv.WriteRecords(App.records);
                    return true;
                }
                catch (Exception ex)
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
        /// <returns></returns>
        public static bool DumpRecords(string filename, ref List<dynamic> data)
        
        {
            using (var writer = new StreamWriter(filename))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                try
                {
                    csv.WriteRecords(data);
                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }   
            }
        }

        /// <summary>
        /// Removes the already separated records
        /// </summary>
        public static void ClearSeparatedRecords()
        {
            foreach (var index in App.separatedRecords)
            {
                App.records.RemoveAt(index);
            }
        }

        /// <summary>
        /// Dump caught log in the order it was caught.
        /// </summary>
        public static void DumpLog(List<Log> appLogs, string filename)
        {
            using (var writer = new StreamWriter(filename))
            {
                foreach (var log in appLogs)
                {
                    writer.WriteLine(log.Message);
                }
            }
        }
    }
}
