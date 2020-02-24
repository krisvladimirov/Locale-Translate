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
        private static List<ErrorLog> errors = new List<ErrorLog>();
        private static IConfiguration config;
        private static AWSOptions awsOptions;
        private static AppSettings settings;
        private static List<dynamic> records = new List<dynamic>();
        private static List<string> allHeaders = new List<string>();
        // The total records including bad ones
        private static short totalRecords = 0;

        private static string SETTINGS_SECTION = "Settings";
        static void Main(string[] args)
        {
            // Builds the configuration provider
            Program.config = GetConfiguration();
            // Builds the AWS Options
            Program.awsOptions = config.GetAWSOptions();
            // Build the application settings data model
            Program.settings = config.GetSection(SETTINGS_SECTION).Get<AppSettings>();

            //var service = new TranslateService(awsOptions.CreateServiceClient<IAmazonTranslate>());
            var outcome = Program.LoadDynamicRecord();
            if (outcome)
            {
                Console.WriteLine("Successful");
            }
            else
            {
                Console.WriteLine("Unsuccessful");
            }
            Program.PrintErrorLog();



            //var properties = new List<DynamicTypeProperty>()
            //{
            //    new DynamicTypeProperty("doubleProperty", typeof(double)),
            //    new DynamicTypeProperty("stringProperty", typeof(string))
            //};

            //// create the new type
            //var dynamicType = DynamicType.CreateDynamicType(properties);
            //// create a list of the new type
            //var dynamicList = DynamicType.CreateDynamicList(dynamicType);

            //// get an action that will add to the list
            //var addAction = DynamicType.GetAddAction(dynamicList);

            //// call the action, with an object[] containing parameters in exact order added
            //addAction.Invoke(new object[] { 1.1, "item1" });
            //addAction.Invoke(new object[] { 2.1, "item2" });
            //addAction.Invoke(new object[] { 3.1, "item3" });
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
        private static bool LoadDynamicRecord()
        {
            var skipRow = false;
            var invalid = false;
            using (var reader = new StreamReader(settings.FileStructure.Path))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {

                csv.Configuration.HasHeaderRecord = true;
                csv.Configuration.MissingFieldFound = (headerNames, fieldIndex, context) =>
                {
                    skipRow = true;
                    Program.errors.Add(new ErrorLog(Occurance.WhenReadingMissingData, context.Row, context.HeaderRecord[fieldIndex], fieldIndex));
                };
                csv.Configuration.BadDataFound = (context) =>
                {
                    skipRow = true;
                    Program.errors.Add(new ErrorLog(Occurance.WhenReadingBadData, context.Row));
                };

                csv.Configuration.HasHeaderRecord = true;
                csv.Read();

                if (csv.ReadHeader())
                {
                    var fileHeaders = csv.Context.HeaderRecord;
                    byte i = 0;

                    if (fileHeaders.Length == 0)
                    {
                        invalid = true;
                        Program.errors.Add(new ErrorLog(Occurance.NoData));
                    }
                    else if (fileHeaders.Length != Program.settings.FileStructure.Headers.Count)
                    {
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

                    while (!invalid && i < fileHeaders.Length)
                    {
                        if (fileHeaders[i] != Program.settings.FileStructure.Headers[i])
                        {
                            invalid = true;
                            Program.errors.Add(new ErrorLog(Occurance.WhenHeadersAreNotMatching, 0, fileHeaders[i], i));
                        }
                        i++;
                    }
                }

                if (!invalid)
                {
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
                        Program.errors.Add(new ErrorLog(Occurance.CSVHelperThrow) { Message = ex.Message });
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
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        private static void RemoveUnnecissaryKeys()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Dump caught errors in the order they were caught
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

    /// <summary>
    /// A property name, and type used to generate a property in the dynamic class.
    /// </summary>
    public class DynamicTypeProperty
    {
        public DynamicTypeProperty(string name, Type type)
        {
            Name = name;
            Type = type;
        }
        public string Name { get; set; }
        public Type Type { get; set; }
    }

    public static class DynamicType
    {
        /// <summary>
        /// Creates a list of the specified type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IEnumerable<object> CreateDynamicList(Type type)
        {
            var listType = typeof(List<>);
            var dynamicListType = listType.MakeGenericType(type);
            return (IEnumerable<object>)Activator.CreateInstance(dynamicListType);
        }

        /// <summary>
        /// creates an action which can be used to add items to the list
        /// </summary>
        /// <param name="listType"></param>
        /// <returns></returns>
        public static Action<object[]> GetAddAction(IEnumerable<object> list)
        {
            var listType = list.GetType();
            var addMethod = listType.GetMethod("Add");
            var itemType = listType.GenericTypeArguments[0];
            var itemProperties = itemType.GetProperties();

            var action = new Action<object[]>((values) =>
            {
                var item = Activator.CreateInstance(itemType);

                for (var i = 0; i < values.Length; i++)
                {
                    itemProperties[i].SetValue(item, values[i]);
                }

                addMethod.Invoke(list, new[] { item });
            });

            return action;
        }

        /// <summary>
        /// Creates a type based on the property/type values specified in the properties
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static Type CreateDynamicType(IEnumerable<DynamicTypeProperty> properties)
        {
            StringBuilder classCode = new StringBuilder();

            // Generate the class code
            classCode.AppendLine("using System;");
            classCode.AppendLine("namespace Dexih {");
            classCode.AppendLine("public class DynamicClass {");

            foreach (var property in properties)
            {
                classCode.AppendLine($"public {property.Type.Name} {property.Name} {{get; set; }}");
            }
            classCode.AppendLine("}");
            classCode.AppendLine("}");

            var syntaxTree = CSharpSyntaxTree.ParseText(classCode.ToString());

            var references = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(DictionaryBase).GetTypeInfo().Assembly.Location)
            };

            var compilation = CSharpCompilation.Create("DynamicClass" + Guid.NewGuid() + ".dll",
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var ms = new MemoryStream())
            {
                var result = compilation.Emit(ms);

                if (!result.Success)
                {
                    var failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    var message = new StringBuilder();

                    foreach (var diagnostic in failures)
                    {
                        message.AppendFormat("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                    }

                    throw new Exception($"Invalid property definition: {message}.");
                }
                else
                {

                    ms.Seek(0, SeekOrigin.Begin);
                    var assembly = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromStream(ms);
                    var dynamicType = assembly.GetType("Dexih.DynamicClass");
                    return dynamicType;
                }
            }
        }
    }
}
