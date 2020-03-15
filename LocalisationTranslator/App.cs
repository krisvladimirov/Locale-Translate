using Amazon.Extensions.NETCore.Setup;
using Amazon.Translate;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Dynamic;
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
        private static string SETTINGS_SECTION = "Settings";
        private static string ICU_HTML_LOG_FILE = "icu_html.txt";
        private static string TRANSLATED_FILE = "translated.csv";
        private static string COMPARISON_FILE = "comparison.csv";
        private static string SEPARATED_FILE = "separated_records.csv";
        private static string FAILED_TO_TRANSLATE = "FAILED_TO_TRANSLATE";
        private static string DO_NOT_TRANSLATE_REQUEST = "NOT_TRANSLATED_AS_REQUESTED";

        /// <summary>
        /// Performs all required steps based on the settings
        /// </summary>
        public static bool Process()
        {
            InitializeOptions();

            if (!Utils.ValidateFile())
            {
                Console.WriteLine("File could not be read, check output log");
                Utils.ExitApp(0);
            }
            
            if (!Utils.ReadLocalisation())
            {
                Console.WriteLine("The app terminated while reading the input file, check output log");
                Utils.ExitApp(0);
            }

            Utils.PromptUserToContinue();

            if (settings.Options.ComparisonFile)
            {
                MakeDeepCopy();
            }

            ProcessRequests();

            // Ensure the output folder in . is present
            Utils.CheckOutputPath();

            if (App.specialData.Count > 0)
            {
                // Produces the logs for any records which contained either ICU or HTML data
                Utils.DumpLog(specialData, App.ICU_HTML_LOG_FILE);
            }

            if (settings.Options.ComparisonFile)
            {
                Utils.DumpRecords(App.comparisonData, App.COMPARISON_FILE);
            }

            if (App.settings.Options.Validation.ICUandHTML.SeparateFile)
            {
                DumpSeparatedRecords();
                ClearSeparatedRecords();
            }

            Utils.DumpRecords(App.records, App.TRANSLATED_FILE);

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
        /// Starts shipping request to Amazon Translate
        /// </summary>
        public static void ProcessRequests()
        {
            var count = 0;
            using (var progress = new ProgressBar(App.records.Count + 1))
            {
                var failed = false;
                foreach (var expando in records)
                {
                    var translate = true;
                    var data = (IDictionary<string, object>)expando;
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
                                
                                // Handle the case whenever the user has requested a comparison file and *NO* translation of
                                // HTML or ICU containing strings
                                if (settings.Options.ComparisonFile)
                                {
                                    var x = (IDictionary<string, object>) App.comparisonData[count];
                                    x[settings.Target] = App.DO_NOT_TRANSLATE_REQUEST;
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

                                // TODO - Update for actual App
                                
                                if (settings.Options.ComparisonFile)
                                {
                                    // Grabs the current record that is being translated
                                    // adds the translated text to the comparisons file
                                    var x = (IDictionary<string, object>) App.comparisonData[count];
                                    x[settings.Target] = "Translated";
                                }

                            }
                            catch (Exception ex)
                            {
                                failed = true;
                                var line = FindOriginalLine(count);
                                App.errors.Add(new Log(Occurance.WhenTranslating, line, ex.Message));

                                if (settings.Options.ComparisonFile)
                                {
                                    // Grabs the current record that is being translated
                                    // adds the translated text to the comparisons file
                                    var x = (IDictionary<string, object>) App.comparisonData[count];
                                    x[settings.Target] = App.FAILED_TO_TRANSLATE;
                                }
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
        /// at which an error while translating has occured or the line was skipped while translating due to validation
        /// </param>
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

            // Whenever no errors have been recorded while reading, just return current line + 2
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
            // Handles the case when the currentLine is bigger than or equal to the last recorded error,
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

        /// <summary>
        /// Produces a partial deep copy of the records that have been read by CsvHelper
        /// The copy is partial as only the key and source text are copied over, the rest of the attributes are omitted
        /// NOTE: A possibly bottleneck, couldn't think of a better way to make a deep copy only of certain data in a dynamic object
        /// </summary>
        /// <returns>
        /// True if it successfully creates a copy, false otherwise.
        /// </returns>
        public static bool MakeDeepCopy()
        {   
            // Allocate as much space as we need + 1 space for the header
            // Or maybe I don't need to add the header?
            App.comparisonData = new List<dynamic>(App.records.Count + 1);

            var header = new ExpandoObject();
            header.TryAdd(settings.FileStructure.KeyHeader, settings.FileStructure.KeyHeader);
            header.TryAdd(settings.Source, settings.Source);
            header.TryAdd(settings.Target, settings.Target);
            App.comparisonData.Add(header);
    
            foreach(var expando in App.records)
            {
                var data = (IDictionary<string, object>) expando;
                var comparisonObject = new ExpandoObject();
                comparisonObject.TryAdd(App.settings.FileStructure.KeyHeader, data[App.settings.FileStructure.KeyHeader]);
                comparisonObject.TryAdd(App.settings.Source, data[settings.FileStructure.TextHeader]);
                // Placeholder for the translation
                comparisonObject.TryAdd(App.settings.Target, "");

                App.comparisonData.Add(comparisonObject);
            }

            // TODO
            return true;
        }

        /// <summary>
        /// Groups the records that need to be saved separately
        /// </summary>
        /// <returns>
        /// True if the records have been dumped successfully, false otherwise
        /// </returns>
        public static bool DumpSeparatedRecords()
        {
            var separatedRecords = new List<dynamic>(App.separatedRecords.Count);
            foreach(var index in App.separatedRecords)
            {
                var record = App.records[index];
                separatedRecords.Add(record);
            }

            return Utils.DumpRecords(separatedRecords, App.SEPARATED_FILE);
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
    }
}
