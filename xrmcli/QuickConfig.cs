

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.IsolatedStorage;
using System.Xml;
using System.Security.Cryptography;

namespace xrmcli
{
    public class QuickConfig
    {

      

        

        private static string GetConfigFile()
        {
            return Path.Combine("xrmcli.xml");
        }

        private static XmlDocument m_Doc;

        private static void EnsureDocLoaded()
        {
            if (m_Doc != null)
                return;

            m_Doc = new XmlDocument();
            try
            {
                if (File.Exists(GetConfigFile()))
                {
                    m_Doc.Load(GetConfigFile());
                }
                else
                {
                    XmlElement eNode = m_Doc.CreateElement("QuickConfig");
                    m_Doc.AppendChild(eNode);
                    SaveConfig();
                }
            }
            catch (System.Security.SecurityException)
            {
                IsolatedStorageFile isoStore =
                    IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);
                if (isoStore.GetFileNames(GetConfigFile()).Length > 0)
                {
                    StreamReader reader =
                            new StreamReader(new IsolatedStorageFileStream(GetConfigFile(), FileMode.Open, isoStore));

                    m_Doc.Load(reader);

                }
                else
                {
                    XmlElement eNode = m_Doc.CreateElement("QuickConfig");
                    m_Doc.AppendChild(eNode);
                    SaveConfig();
                }

            }
            catch (Exception e)
            {
                throw new Exception("Error Loading QuickConfig.xml : " + e.Message);
            }

        }

        /// <summary>
        /// Save the QuickConfig.xml document to the default directory.
        /// </summary>
        public static void SaveConfig()
        {
            EnsureDocLoaded();

            try
            {
                m_Doc.Save(GetConfigFile());
            }
            catch (System.Security.SecurityException se)
            {



                IsolatedStorageFile isoStore =
                    IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);

                StreamWriter writer = null;

                // Assign the writer to the store and the file TestStore.

                writer = new StreamWriter(new IsolatedStorageFileStream(GetConfigFile(), FileMode.Create, isoStore));


                m_Doc.Save(writer);

                writer.Close();


            }
        }

        /// <summary>
        /// Returns the string value of the passed attribute in the passed tag.  
        /// If the tag/attribute is not found, the default string passed in is returned.
        /// </summary>
        /// <param name="sTag">Xml tag name</param>
        /// <param name="sTokenName">Xml attribute name</param>
        /// <param name="sDefault">Default string value to be returned if no Tag or Token is found.</param>
        /// <returns></returns>
        public static string GetStringValue(string sTag, string sTokenName, string sDefault)
        {
            EnsureDocLoaded();

            XmlNode eNode = m_Doc.SelectSingleNode("//" + sTag);
            if (eNode == null)
                return sDefault;

            if (eNode.Attributes.GetNamedItem(sTokenName) != null)
                return eNode.Attributes.GetNamedItem(sTokenName).Value.ToString();
            else
                return sDefault;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sTag"></param>
        /// <param name="sTokenName"></param>
        /// <param name="sDefault"></param>
        /// <returns></returns>
        public static string GetProtectedStringValue(string sTag, string sTokenName, string sDefault)
        {
            EnsureDocLoaded();

            XmlNode eNode = m_Doc.SelectSingleNode("//" + sTag);
            if (eNode == null)
                return sDefault;



            if (eNode.Attributes.GetNamedItem(sTokenName) != null)
            {
                
                byte[] protectedBytes = Base64Decode(eNode.Attributes.GetNamedItem(sTokenName).Value.ToString());
                byte[] rawBytes = ProtectedData.Unprotect(protectedBytes,null, DataProtectionScope.CurrentUser);
                return System.Text.Encoding.Default.GetString(rawBytes);
            }
            else
                return sDefault;
        }

        /// <summary>
        /// Returns the integer value of the passed attribute in the passed tag.  
        /// If the tag/attribute is not found, the default integer passed in is returned.
        /// </summary>
        /// <param name="sTag">Xml tag name</param>
        /// <param name="sTokenName">Xml attribute name</param>
        /// <param name="sDefault">Default integer value to be returned if no Tag or Token is found.</param>
        /// <returns></returns>
        public static int GetIntValue(string sTag, string sTokenName, int sDefault)
        {
            EnsureDocLoaded();
            XmlNode eNode = m_Doc.SelectSingleNode("//" + sTag);
            if (eNode == null)
                return sDefault;

            if (eNode.Attributes.GetNamedItem(sTokenName) != null)
                return int.Parse(eNode.Attributes.GetNamedItem(sTokenName).Value.ToString());
            else
                return sDefault;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sTag"></param>
        /// <param name="sTokenName"></param>
        /// <param name="sValue"></param>
        public static void SetValue(string sTag, string sTokenName, string sValue)
        {
            EnsureDocLoaded();
            XmlNode eNode = m_Doc.SelectSingleNode("//" + sTag);
            if (eNode == null)
            {
                eNode = m_Doc.CreateElement(sTag);
                m_Doc.DocumentElement.AppendChild(eNode);
            }

            XmlElement eElm = eNode as XmlElement;
            eElm.SetAttribute(sTokenName, sValue);
        }
        public static string Base64Encode(byte[] data)
        {            
            return System.Convert.ToBase64String(data);
        }
        public static byte[] Base64Decode(string base64EncodedData)
        {
            return System.Convert.FromBase64String(base64EncodedData);
          
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sTag"></param>
        /// <param name="sTokenName"></param>
        /// <param name="sValue"></param>
        public static void SetProtectedValue(string sTag, string sTokenName, string sValue)
        {
            EnsureDocLoaded();

            XmlNode eNode = m_Doc.SelectSingleNode("//" + sTag);
            if (eNode == null)
            {
                eNode = m_Doc.CreateElement(sTag);
                m_Doc.DocumentElement.AppendChild(eNode);
            }
            byte[] rawBytes = Encoding.UTF8.GetBytes(sValue);

            // Encrypt the PIN by using the Protect() method.
            byte[] protectedBytes = ProtectedData.Protect(rawBytes, null, DataProtectionScope.CurrentUser);            

            

            XmlElement eElm = eNode as XmlElement;
            eElm.SetAttribute(sTokenName, Base64Encode(protectedBytes));
        }
    }
}

