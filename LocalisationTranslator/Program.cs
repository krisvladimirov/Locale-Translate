using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Translate;
using Microsoft.Extensions.Configuration;

namespace LocalisationTranslator
{
    /// <summary>
    /// 
    /// </summary>
    public class Program
    {
        private static Dictionary<Occurance, List<ErrorLog>> errors = new Dictionary<Occurance, List<ErrorLog>>();
        private static IConfiguration config;
        private static AWSOptions awsOptions;
        private static AppSettings settings;

        private static string SETTINGS_SECTION = "Settings";
        static void Main(string[] args)
        {
            // Builds the configuration provider
            Program.config = GetConfiguration();
            // Builds the AWS Options
            Program.awsOptions = config.GetAWSOptions();
            // Build the application settings data model
            Program.settings = config.GetSection(SETTINGS_SECTION).Get<AppSettings>();

            Console.WriteLine("Hello");
            //var service = new TranslateService(awsOptions.CreateServiceClient<IAmazonTranslate>());
            Program.Process();
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
        /// Performs all required steps based on the settings
        /// </summary>
        private static bool Process()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads the CSV localisation file
        /// </summary>
        private static bool ReadLocalisation()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Starts shipping request to Amazon Translate
        /// </summary>
        private static void ProcessRequests()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        private static void RemoveUnnecissaryKeys()
        {

        }

        private static void PrintErrorLog()
        {
            using (var writer = new StreamWriter("error_log.txt"))
            {
                foreach(var kvp in errors)
                {
                    if (kvp.Key == Occurance.WhenValidating)
                    {
                        //writer.WriteLine($"The following errors occured while reading data from {}");
                    }
                    else if (kvp.Key == Occurance.WhenShipping)
                    {
                        writer.WriteLine();
                    }

                    kvp.Value.ForEach(x =>
                    {
                        writer.WriteLine(x.FormatMessage(kvp.Key));
                    });
                }
            }

        }
    }
}
