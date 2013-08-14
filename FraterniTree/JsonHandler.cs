using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FraterniTree
{
    public static class JsonHandler
    {
        private static string m_Json = "";
        private static string m_Name = "";
        private static string m_Stripped = "";

        public static string Json
        {
            get
            {
                return m_Json;
            }

            set
            {
                m_Json = value;
                StripJson();
            }
        }

        public static string GetName()
        {
            return m_Name;
        }

        public static Brother[] GetFullTree()
        {
            return JsonToBrother();
        }

        private static Brother[] JsonToBrother()
        {
            List<Brother> BrotherList = JsonConvert.DeserializeObject<List<Brother>>(m_Stripped);
            Brother[] Brothers = new Brother[BrotherList.Count];
            foreach (Brother b in BrotherList)
            {
                Brothers[b.ID] = b;
            }

            JObject tree = JObject.Parse(m_Json);

            for (int i = 0; i < BrotherList.Count; i++)
            {
                if (tree[m_Name]["Brother"][i]["Children"] == null)
                {
                    continue;
                }

                if (tree[m_Name]["Brother"][i]["Children"]["BrotherID"].Type == JTokenType.Array)
                {
                    JArray ChildrenIds = (JArray)tree[m_Name]["Brother"][i]["Children"]["BrotherID"];
                    foreach (string id in ChildrenIds.Children())
                    {
                        BrotherList[i].AddChild(Brothers[Int32.Parse(id)]);
                    }
                    BrotherList[i].RefreshLittleOrder();
                }
                else
                {
                    JValue ChildId = (JValue)tree[m_Name]["Brother"][i]["Children"]["BrotherID"];
                    BrotherList[i].AddChild(Brothers[Int32.Parse((string)ChildId.Value)]);
                    BrotherList[i].RefreshLittleOrder();
                }
            }
            return Brothers;
        }

        private static string BrotherToJson()
        {
            string jsonData = "";



            return jsonData;
        }

        private static void StripJson()
        {
            m_Stripped = m_Json.Substring(m_Json.IndexOf('[', 1), m_Json.LastIndexOf(']') - m_Json.IndexOf('[', 1) + 1);
            m_Name = m_Json.Substring(m_Json.IndexOf('\"') + 1, m_Json.IndexOf('\"', m_Json.IndexOf('\"') + 1) - m_Json.IndexOf('\"') - 1);
        }
    }
}
