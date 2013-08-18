using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Xml;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace DataProviders
{
    public class XmlProvider : iDataProvider
    {
        XmlDocument m_XmlDoc = new XmlDocument();
        string m_FilePath;

        public XmlProvider(string filePath)
        {
            m_FilePath = filePath;
            if (!File.Exists(m_FilePath))
            {
                m_XmlDoc = new XmlDocument();
            }
            else
            {
                m_XmlDoc.Load(m_FilePath);
            }
        }

        public string GetData()
        {
            if (m_XmlDoc == null)
            {
                throw new Exception("XmlProvider was not initialized properly");
            }
            if (m_XmlDoc.FirstChild.NodeType == XmlNodeType.XmlDeclaration)
            {
                XmlDeclaration declaration = m_XmlDoc.FirstChild as XmlDeclaration;
                m_XmlDoc.RemoveChild(declaration);
            }
            return Regex.Replace(JsonConvert.SerializeXmlNode(m_XmlDoc, Newtonsoft.Json.Formatting.Indented), "(?<=\")(@)(?!.*\":\\s )", string.Empty, RegexOptions.IgnoreCase);
        }

        public void SaveData(string JSON)
        {
            m_XmlDoc = (XmlDocument)JsonConvert.DeserializeXmlNode(JSON);
            if (m_XmlDoc.FirstChild != null)
            {
                if (m_XmlDoc.FirstChild.NodeType != XmlNodeType.XmlDeclaration)
                {
                    XmlDeclaration declaration = m_XmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);
                    XmlElement root = m_XmlDoc.DocumentElement;
                    m_XmlDoc.InsertBefore(declaration, root);
                }
            }
            m_XmlDoc.Save(m_FilePath);
        }

    }
}
