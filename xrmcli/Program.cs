﻿using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Client;
using Microsoft.Xrm.Client.Services;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xrmcli
{
    class Program
    {
        static void Main(string[] args)
        {
            var command = Args.Configuration.Configure<CommandObject>().CreateAndBind(args);
            
            switch(command.Action)
            {
                case "Connect":
                    StoreCRMConnection(command);
                    break;
                case "DeployWebResource":
                    DeployWebResource(command);
                    break;
                default:
                    Console.WriteLine("Action not supported");
                    break;
            }
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
                pubReq.ParameterXml ="<importexportxml><webresources><webresource>{" + wr.Id + "}</webresource></webresources></importexportxml>";
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
            switch(type)
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
            var cs = string.Format("Url={0}; Username={1}; Password={2};", server, user, password);
            
            CrmConnection c = CrmConnection.Parse(cs);
            OrganizationService service = new OrganizationService(c);
            return service;
        }

        private static void StoreCRMConnection(CommandObject command)
        {
            QuickConfig.SetProtectedValue("Connection", "Server", command.Server);
            QuickConfig.SetProtectedValue("Connection", "User", command.User);
            QuickConfig.SetProtectedValue("Connection", "Password", command.Password);
            QuickConfig.SaveConfig();
            Console.WriteLine("trying to connect to CRM");
            var service =  GetCRMService();
            var response = service.Execute(new WhoAmIRequest()) as WhoAmIResponse;
            Console.WriteLine("Successfully connected to CRM");

            

        
        }
    }
    public class CommandObject
    {
        public string Action { get; set; }
        public string File { get; set; }

        public string Server { get; set; }

        public string User { get; set; }

        public string Password { get; set; }

        public string Solution { get; set; }

        public string UniqueName { get; set; }

        public string DisplayName { get; set; }

        public string WebResourceType { get; set; }
    }

}