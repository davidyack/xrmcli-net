using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace xrmcli
{
    class Program
    {
        static void Main(string[] args)
        {
            var command = Args.Configuration.Configure<CommandObject>().CreateAndBind(args);

            switch (command.Action.ToLower())
            {
                case "connect":
                    StoreCRMConnection(command);
                    break;
                case "clear":
                    ClearCRMConnection(command);
                    break;
                case "whoamo":
                    ShowWhoAmI(command);
                    break;
                case "deploywebresource":
                    DeployWebResource(command);
                    break;
                case "deployassembly":
                    DeployAssembly(command);
                    break;
                default:
                    Console.WriteLine("Action not supported");
                    DisplaySupportedActions();
                    break;
            }
        }

        private static void ShowWhoAmI(CommandObject command)
        {
            var server = QuickConfig.GetProtectedStringValue("Connection", "Server", string.Empty);
            var user = QuickConfig.GetProtectedStringValue("Connection", "User", string.Empty);
            Console.WriteLine("Server:" + server);
            Console.WriteLine("User:" + user);
            var service = GetCRMService();
            if (service != null)
            {
                WhoAmIResponse resp = service.Execute(new WhoAmIRequest()) as WhoAmIResponse;
                Console.WriteLine("UserID is : " + resp.UserId);
            }
        }

        private static void DisplaySupportedActions()
        {
            Console.WriteLine("Supported actions are:");
            Console.WriteLine("  Connect");
            Console.WriteLine("  Clear");
            Console.WriteLine("  WhoAmI");
            Console.WriteLine("  DeployWebResource");
            Console.WriteLine("  DeployAssembly");
        }

        private static void DeployAssembly(CommandObject command)
        {
            if (string.IsNullOrEmpty(command.File) &&
                   string.IsNullOrEmpty(command.FilePath))
            {
                Console.WriteLine("You must provide /file or /filepath");
                return;
            }
            
            int assemblyCount = 0;
            if (!string.IsNullOrEmpty(command.FilePath))
            {
                string[] files = Directory.GetFiles(command.FilePath, "*.dll");
                foreach (var file in files)
                {
                    bool hasPlugins = false;
                    Assembly pluginAssembly = CheckIfAssemblyIsPlugin(file, ref hasPlugins);
                    if (hasPlugins)
                    {
                        //Console.WriteLine("Should update file - " + pluginAssembly.GetName().Name + " file-" + file);
                        if (UpdateAssembly(file, pluginAssembly.GetName().Name))
                            assemblyCount++;
                    }
                    
                }
                Console.WriteLine("Found and Updated {0} assemblies", assemblyCount);
            }
            else
            {
            
                UpdateAssembly(command.File, command.UniqueName);
            }


        }

        private static Assembly CheckIfAssemblyIsPlugin(string file, ref bool hasPlugins)
        {
            try
            {
                var pluginAssembly = Assembly.LoadFrom(file);
                foreach (var type in pluginAssembly.GetTypes())
                {
                    var pluginInterface = typeof(IPlugin);
                    if (type != pluginInterface && pluginInterface.IsAssignableFrom(type))
                    {
                        hasPlugins = true;
                    }
                }

                return pluginAssembly;
            }
            catch(Exception ex)
            {
                return null;
            }
        }

        private static bool UpdateAssembly(string file, string name)
        {
            var service = GetCRMService();
            if (service == null)
            {
                return false;
            }
            var fileData = GetFileData(file);            
            QueryExpression q = new QueryExpression("pluginassembly");
            q.ColumnSet = new ColumnSet(new[] { "pluginassemblyid" });
            q.Criteria = new FilterExpression();
            q.Criteria.AddCondition("name", ConditionOperator.Equal, name);
            var qr = service.RetrieveMultiple(q);
            var wr = new Entity("pluginassembly");
            wr["name"] = name;
            var encodedData = Convert.ToBase64String(fileData, 0, fileData.Length);
            wr["content"] = encodedData;
            if (qr.Entities.Count > 0)
            {
                var wrexisting = qr.Entities.FirstOrDefault();
                wr.Id = wrexisting.Id;
                service.Update(wr);
                Console.WriteLine("{0}:Updated assembly",name);
            }
            else
            {
                //service.Create(wr);
                Console.WriteLine("{0} : Assembly Not Found updload using Plugin Registration tool first ",name);
            }
            return true;
        }

        private static void DeployWebResource(CommandObject command)
        {
            var service = GetCRMService();
            Console.WriteLine("Loading file data");
            var fileData = GetFileData(command.File);
            Console.WriteLine("File Data Loaded");
            QueryExpression q = new QueryExpression("webresource");
            q.ColumnSet = new ColumnSet(new[] { "webresourceid" });
            q.Criteria = new FilterExpression();
            q.Criteria.AddCondition("name", ConditionOperator.Equal, command.UniqueName);
            var qr = service.RetrieveMultiple(q);
            var wr = new Entity("webresource");
            wr["name"] = command.UniqueName;
            wr["displayname"] = command.DisplayName;
            var encodedData = Convert.ToBase64String(fileData, 0, fileData.Length);
            wr["content"] = encodedData;

            if (qr.Entities.Count > 0)
            {
                var wrexisting = qr.Entities.FirstOrDefault();
                wr.Id = wrexisting.Id;
                service.Update(wr);
                Console.WriteLine("Updated web resource");
                PublishXmlRequest pubReq = new PublishXmlRequest();
                pubReq.ParameterXml = "<importexportxml><webresources><webresource>{" + wr.Id + "}</webresource></webresources></importexportxml>";
                service.Execute(pubReq);
                Console.WriteLine("Web Resource Published");

            }
            else
            {
                wr["webresourcetype"] = GetWebResourceType(command.WebResourceType);
                service.Create(wr);
                Console.WriteLine("Created web resource");
            }

        }
        private static OptionSetValue GetWebResourceType(string type)
        {
            switch (type)
            {
                case "HTML":
                    return new OptionSetValue(1);
                case "CSS":
                    return new OptionSetValue(2);
                case "JavaScript":
                    return new OptionSetValue(3);
                case "XML":
                    return new OptionSetValue(4);
                case "PNG":
                    return new OptionSetValue(5);
                case "JPG":
                    return new OptionSetValue(6);
                case "GIF":
                    return new OptionSetValue(7);
                case "XAP":
                    return new OptionSetValue(8);
                case "XSL":
                    return new OptionSetValue(9);
                case "ICO":
                    return new OptionSetValue(10);

            }
            return null;
        }
        private static byte[] GetFileData(string fileName)
        {
            FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            byte[] data = new byte[fileStream.Length];
            fileStream.Read(data, 0, (int)fileStream.Length);
            fileStream.Close();

            return data;
        }

        private static IOrganizationService GetCRMService()
        {
            var server = QuickConfig.GetProtectedStringValue("Connection", "Server", string.Empty);
            var user = QuickConfig.GetProtectedStringValue("Connection", "User", string.Empty);
            var password = QuickConfig.GetProtectedStringValue("Connection", "Password", string.Empty);
            var cs = string.Format("AuthType=Office365; Url={0}; Username={1}; Password={2};", server, user, password);

            if (string.IsNullOrEmpty(server) ||
                string.IsNullOrEmpty(user) ||
                string.IsNullOrEmpty(password))
            {
                Console.WriteLine("You must use /action Connect first");
                return null;
            }
            
            CrmServiceClient connection = new CrmServiceClient(cs);
            
            return connection;
        }

        private static void StoreCRMConnection(CommandObject command)
        {
            if (string.IsNullOrEmpty(command.Server) ||
                    string.IsNullOrEmpty(command.User) 
                    )
            {
                Console.WriteLine("You must provide /server and /user and /password");
                return;
            }

            QuickConfig.SetProtectedValue("Connection", "Server", command.Server);
            QuickConfig.SetProtectedValue("Connection", "User", command.User);
            if (string.IsNullOrEmpty(command.Password))
            {
                string pass = "";
                Console.Write("Enter your password: ");
                ConsoleKeyInfo key;

                do
                {
                    key = Console.ReadKey(true);
                
                    if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                    {
                        pass += key.KeyChar;
                        Console.Write("*");
                    }
                    else
                    {
                        if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                        {
                            pass = pass.Substring(0, (pass.Length - 1));
                            Console.Write("\b \b");
                        }
                    }
                }
                
                while (key.Key != ConsoleKey.Enter);

                Console.WriteLine();                
                command.Password = pass;
            }
            QuickConfig.SetProtectedValue("Connection", "Password", command.Password);
            QuickConfig.SaveConfig();

            try
            {
                Console.WriteLine("trying to connect to Dynamics 365");
                var service = GetCRMService();
                var response = service.Execute(new WhoAmIRequest()) as WhoAmIResponse;
                Console.WriteLine("Successfully connected to Dynamics 365");
            }
            catch(Exception ex)
            {
                Console.WriteLine("hmm...Not so good connection failed - please check info!");
                Console.WriteLine(ex.Message);
                return;
            }

           




        }
    
    private static void ClearCRMConnection(CommandObject command)
    {
      
        QuickConfig.SetProtectedValue("Connection", "Server", "");
        QuickConfig.SetProtectedValue("Connection", "User", "");        
        QuickConfig.SetProtectedValue("Connection", "Password", "");
        QuickConfig.SaveConfig();

        Console.WriteLine("Connection Cleared");

    }
}
public class CommandObject
    {
        public string Action { get; set; }
        public string File { get; set; }

        public string FilePath { get; set; }

        public string Server { get; set; }

        public string User { get; set; }

        public string Password { get; set; }

        public string Solution { get; set; }

        public string UniqueName { get; set; }

        public string DisplayName { get; set; }

        public string WebResourceType { get; set; }
    }

}
