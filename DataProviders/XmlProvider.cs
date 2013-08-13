using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Xml;
using Newtonsoft.Json;

namespace DataProviders
{
    public class XmlProvider : iDataProvider
    {
        XmlDocument m_XmlDoc = new XmlDocument();
        string m_FilePath;

        public XmlProvider(string filePath)
        {
            m_FilePath = filePath;
            m_XmlDoc.Load(m_FilePath);
        }

        public string GetData()
        {
            if (m_XmlDoc == null)
            {
                throw new Exception("XmlProvider was not initialized properly");
            }
            return JsonConvert.SerializeXmlNode(m_XmlDoc);
        }

        public void SaveData(string JSON)
        {
            m_XmlDoc = (XmlDocument)JsonConvert.DeserializeXmlNode(JSON);
            m_XmlDoc.Save(m_FilePath);
        }

    }
}
