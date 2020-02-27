using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Translate;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Configuration;
using CsvHelper;
using System.Globalization;
using System.Dynamic;

namespace LocalisationTranslator
{
    /// <summary>
    /// 
    /// </summary>
    public class Program
    {
        // List of error that were encountered while running
        private static List<ErrorLog> errors = new List<ErrorLog>();

        // The configuration builder
        private static IConfiguration config;

        // The AWS options
        private static AWSOptions awsOptions;

        // The application settings
        private static AppSettings settings;
        
        // Service shipping/receiving the translation requests to and from AWS Translate
        private static TranslateService translateService;

        // All successfully read records as ExpandoObjects
        private static List<dynamic> records = new List<dynamic>();

        // The lines at which errors were encountered while reading from the csv file
        private static List<int> erroredLines = new List<int>();

        // The total records including bad ones
        private static short totalRecords = 0;

        // Whether or not to terminate the main Thread
        private static bool outcome = true;

        private static string SETTINGS_SECTION = "Settings";
        static int Main(string[] args)
        {
            
            // Builds the configuration provider
            config = GetConfiguration();
            // Builds the AWS Options
            awsOptions = config.GetAWSOptions();
            // Build the application settings data model
            settings = config.GetSection(SETTINGS_SECTION).Get<AppSettings>();
            // Amazon's Translate service
            translateService = new TranslateService(awsOptions.CreateServiceClient<IAmazonTranslate>());

            outcome = Program.ReadLocalisation();
            if (outcome)
            {
                Console.WriteLine("Successful");
            }
            else
            {
                Console.WriteLine("Unsuccessful");
                return -1;
            }

            foreach(var expando in records)
            {
                var record = (IDictionary<string, object>) expando;
                var sb = new StringBuilder("");
                foreach (var kvp in record)
                {
                    sb.Append($"{kvp.Value} ");
                }
                sb.Append("\n");
                Console.WriteLine($"{sb}");
            }

            Program.Process();

            if (errors.Count > 0)
            {
                Program.PrintErrorLog();
            }

            return 0;
        }

        /// <summary>
        /// Makes the configuration for this program
        /// </summary>
        /// <returns>The built configuration</returns>
        private static IConfiguration GetConfiguration()
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
        private static bool ReadLocalisation()
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
                    Program.errors.Add(new ErrorLog(Occurance.WhenReadingMissingData, context.Row, context.HeaderRecord[fieldIndex], fieldIndex));
                    
                    // Omits lines which are already added, we don't want duplicates
                    if (lastErrorLine != context.Row)
                    {
                        Program.erroredLines.Add(context.Row);
                        lastErrorLine = context.Row;
                    }
                };

                // Configures a handler for bad data
                csv.Configuration.BadDataFound = (context) =>
                {
                    skipRow = true;
                    Program.errors.Add(new ErrorLog(Occurance.WhenReadingBadData, context.Row));

                    // Omits lines which are already added, we don't want duplicates
                    if (lastErrorLine != context.Row)
                    {
                        Program.erroredLines.Add(context.Row);
                        lastErrorLine = context.Row;
                    }
                };

                csv.Configuration.HasHeaderRecord = true;
                csv.Read();
                
                // Attempts to read the header for validation
                if (csv.ReadHeader())
                {
                    var fileHeaders = csv.Context.HeaderRecord;
                    byte i = 0;

                    // If the file is empty
                    if (fileHeaders.Length == 0)
                    {
                        invalid = true;
                        Program.errors.Add(new ErrorLog(Occurance.NoData));
                    }
                    else if (fileHeaders.Length != Program.settings.FileStructure.Headers.Count)
                    {
                        // If the length of the headers in the files and the provided headers does not match
                        invalid = true;
                        if (fileHeaders.Length > Program.settings.FileStructure.Headers.Count)
                        {
                            Program.errors.Add(new ErrorLog(Occurance.WhenHeadersAreMore) { Token = Program.settings.FileStructure.Path });
                        }
                        else
                        {
                            Program.errors.Add(new ErrorLog(Occurance.WhenHeadersAreLess) { Token = Program.settings.FileStructure.Path });
                        }
                    }

                    // If the header do not match
                    while (!invalid && i < fileHeaders.Length)
                    {
                        if (fileHeaders[i] != Program.settings.FileStructure.Headers[i])
                        {
                            invalid = true;
                            errors.Add(new ErrorLog(Occurance.WhenHeadersAreNotMatching, 0, fileHeaders[i], i));
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
                                Program.records.Add(record);
                            }
                            skipRow = false;
                            totalRecords++;
                        }
                        return true;
                    }
                    catch (Exception ex)
                    {
                        // TODO: Should I add erroredLines here as well?
                        errors.Add(new ErrorLog(Occurance.CSVHelperThrow) { Message = ex.Message });
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
        /// Performs all required steps based on the settings
        /// </summary>
        private static bool Process()
        {
            RemoveUnnecissaryKeys();
            // ProcessRequests();
            return false;
        }

        /// <summary>
        /// Starts shipping request to Amazon Translate
        /// </summary>
        private static void ProcessRequests()
        {
            // TODO: Make the spinny boi
            var count = 0;
            foreach(var expando in records){
                var data = (IDictionary<String, String>) expando;
                var text = data[settings.FileStructure.TextHeader]; 
                if (!string.IsNullOrEmpty(text)) {
                    try {
                        var task =  translateService.TranslateText(text, settings.Source, settings.Target);
                    }
                    catch (Exception ex) {
                        var line = FindOriginalErrorLine(count);
                        errors.Add(new ErrorLog(Occurance.WhenTranslating, line, ex.Message));
                    }
                    
                }
                count++;
            }
            throw new NotImplementedException();
        }

        /// <summary>
        /// Attempts to locate the record line which has caused an exception
        /// </summary>
        /// <param name="currentLine">The line of which an error occured while translating</param>
        /// <returns>The original line at which this error is placed in the input file</returns>
        private static int FindOriginalErrorLine(int currentLine) {
            // Represent how much needs to be added to a line to map to its original placement in the CSV file
            var positiveOffset = 0;
            if (erroredLines.Count == 0)
            {
                return currentLine;
            }


            // currentLine is zero based
            // while the errors are not zero based the header is also taken into account therefore if an error occures
            // while reading the first record it will be at line 2 not 1
            // that's why it is currentLine + 2
            var errIndex = erroredLines.FindIndex(x => x >= currentLine + 2);
            var errValue = erroredLines[errIndex];
            if (errValue == currentLine + 2)
            {
                var foundLine = false;
                while (!foundLine && errIndex < erroredLines.Count)
                {
                    errIndex++;
                    var nextValue = erroredLines[errIndex];
                    if (nextValue - errValue > 1)
                    {
                        positiveOffset = errIndex;
                        foundLine = true;
                    } 
                    else
                    {
                        errValue = nextValue;
                    }
                }
            } 
            else
            {
                // Whenever currentLine + 2 is smaller than the nth item, we simple return the index of the nth item
                positiveOffset = errIndex;
            }

            return positiveOffset;
        }

        /// <summary>
        /// 
        /// </summary>
        private static void RemoveUnnecissaryKeys()
        {   
            
            throw new NotImplementedException();
        }

        /// <summary>
        /// Dump caught errors in the order they were caught.
        /// </summary>
        private static void PrintErrorLog()
        {
            using (var writer = new StreamWriter("error_log.txt"))
            {
                foreach (var error in Program.errors)
                {
                    writer.WriteLine(error.Message);
                }
            }
        }
    }
}
