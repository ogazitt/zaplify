using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class WorkflowConstants
    {
        private const string IntentsFileName = "workflows\\Intents.txt";

        public static List<Intent> DefaultIntents()
        {
            if (!File.Exists(IntentsFileName))
                return null;

            // load intents from file
            var intents = new List<Intent>();
            using (var file = File.Open(IntentsFileName, FileMode.Open))
            using (var reader = new StreamReader(file))
            {
                string intentDef = reader.ReadLine();
                while (!String.IsNullOrEmpty(intentDef))
                {
                    string[] parts = intentDef.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 3)
                        continue;
                    intents.Add(new Intent()
                    {
                        Verb = parts[0],
                        Noun = parts[1],
                        WorkflowType = parts[2]
                    });
                    intentDef = reader.ReadLine();
                }
            }
            return intents;
        }

        public static List<WorkflowType> DefaultWorkflowTypes()
        {
            // load workflow types from files
            try
            {
                Directory.SetCurrentDirectory("workflows");
                var workflowTypes = new List<WorkflowType>();
                foreach (var filename in Directory.EnumerateFiles(".", "*.json"))
                {
                    string prefix = ".\\";
                    string suffix = ".json";
                    using (var file = File.Open(filename, FileMode.Open))
                    using (var reader = new StreamReader(file))
                    {
                        // strip ".\" off the beginning of the filename, and the ".json" extension
                        string workflowName = filename.StartsWith(prefix) ? filename.Substring(prefix.Length) : filename;
                        workflowName = workflowName.Replace(suffix, "");

                        string workflowDef = reader.ReadToEnd();
                        if (!String.IsNullOrEmpty(workflowDef))
                            workflowTypes.Add(new WorkflowType() { Type = workflowName, Definition = workflowDef });
                    }
                }
                return workflowTypes;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
