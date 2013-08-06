using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RegistryAccess;
using DataProtectionSimple;
using Microsoft.VisualBasic;
using System.IO;
using System.Xml;

namespace FraterniTree
{
    public partial class StartUp : Form
    {
        public int    m_Port       { get; set; }
        public bool   m_bXML       { get; set; }
        public bool   m_bIsMale    { get; set; }
        public string m_Server     { get; set; }
        public string m_DBase      { get; set; }
        public string m_UName      { get; set; }
        public string m_PWord      { get; set; }
        public string m_FilePath   { get; set; }
        public string m_ParentNode { get; set; }

        public StartUp()
        {
            InitializeComponent();

            m_Port    = 0;
            m_bIsMale = true;
            m_Server  = "";
            m_DBase   = "";
            m_UName   = "";
            m_PWord   = "";

            RegAccess.APPLICATION_NAME = AssemblyProduct;
            RegAccess.COMPANY_NAME = AssemblyCompany;
            string[] dbs = RegAccess.GetAllSubKeys();

            if (dbs != null)
            {
                foreach (string db in dbs)
                {
                    ToolStripMenuItem tmp = new ToolStripMenuItem(db);
                    tmp.Click += genericToolStripMenuItem_Click;
                    connectToolStripMenuItem.DropDownItems.Add(tmp);
                }
            }
        }

        private bool[] m_bInputValid = new bool[5];

        private void tbValidator()
        {
            bool ready = true;
            foreach (bool b in m_bInputValid)
            {
                if (!b)
                {
                    ready = false;
                    return;
                }
            }
            if (ready)
            {
                btnSubmit.Enabled = true;
            }
            else
            {
                btnSubmit.Enabled = false;
            }
        }

        private void tbServer_TextChanged(object sender, EventArgs e)
        {
            if (tbServer.Text != "")
            {
                m_bInputValid[0] = true;
            }
            else
            {
                m_bInputValid[0] = false;
            }
            tbValidator();
        }

        private void tbPort_TextChanged(object sender, EventArgs e)
        {
            if (tbPort.Text.IndexOfAny(new char[10] {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9'}) >= 0)
            {
                m_bInputValid[1] = true;
            }
            else
            {
                m_bInputValid[1] = false;
            }
            tbValidator();
        }

        private void tbDb_TextChanged(object sender, EventArgs e)
        {
            if (tbDb.Text != "")
            {
                m_bInputValid[2] = true;
            }
            else
            {
                m_bInputValid[2] = false;
            }
            tbValidator();
        }

        private void tbUser_TextChanged(object sender, EventArgs e)
        {
            if (tbUser.Text != "")
            {
                m_bInputValid[3] = true;
            }
            else
            {
                m_bInputValid[3] = false;
            }
            tbValidator();
        }

        private void tbPass_TextChanged(object sender, EventArgs e)
        {
            if (tbPass.Text != "")
            {
                m_bInputValid[4] = true;
            }
            else
            {
                m_bInputValid[4] = false;
            }
            tbValidator();
        }

        private void btnSubmit_Click(object sender, EventArgs e)
        {
            m_Port   = Int32.Parse(tbPort.Text);
            m_Server = tbServer.Text;
            m_DBase  = tbDb.Text;
            m_UName  = tbUser.Text;
            m_PWord  = tbPass.Text;
            this.Close();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void rbMale_CheckedChanged(object sender, EventArgs e)
        {
            if (rbMale.Checked)
            {
                m_bIsMale = true;
            }
            else
            {
                m_bIsMale = false;
            }
        }

        private void StartUp_FormClosing(object sender, FormClosingEventArgs e)
        {
            //this.DialogResult = DialogResult.Abort;
        }

        private void genericToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string dbName = ((ToolStripMenuItem)(sender)).Text;
            tbDb.Text = RegistryAccess.RegAccess.GetStringRegistryValue("db", "", dbName);
            tbPort.Text = RegistryAccess.RegAccess.GetStringRegistryValue("port", "", dbName);
            tbServer.Text = RegistryAccess.RegAccess.GetStringRegistryValue("server", "", dbName);
            tbUser.Text = RegistryAccess.RegAccess.GetStringRegistryValue("user", "", dbName);
            tbPass.Text = DP.Decrypt(RegistryAccess.RegAccess.GetStringRegistryValue("pass", "", dbName), "1950");
        }

        #region Assembly Attribute Accessors

        public string AssemblyTitle
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (titleAttribute.Title != "")
                    {
                        return titleAttribute.Title;
                    }
                }
                return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
            }
        }

        public string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        public string AssemblyDescription
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyDescriptionAttribute)attributes[0]).Description;
            }
        }

        public string AssemblyProduct
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyProductAttribute)attributes[0]).Product;
            }
        }

        public string AssemblyCopyright
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }

        public string AssemblyCompany
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCompanyAttribute)attributes[0]).Company;
            }
        }
        #endregion

        private void btnXml_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "XML Document|*.xml|All Files|*.*";
            ofd.DefaultExt = ".xml";
            ofd.AddExtension = true;
            ofd.CheckFileExists = true;
            DialogResult res = ofd.ShowDialog();
            if (res == DialogResult.OK)
            {
                m_FilePath = ofd.FileName;
                if (!File.Exists(ofd.FileName))
                {
                    m_ParentNode = Interaction.InputBox("Please enter a name for the parent XML node...\n" +
                                                        "Example: \"DeltaSigmaPhi-AlphaEta\"",
                                                        "Parent Node Name",
                                                        "MyTree");
                }
                else
                {
                    XmlDocument tmpDoc = new XmlDocument();
                    tmpDoc.Load(ofd.FileName);
                    m_ParentNode = tmpDoc.DocumentElement.Name;
                }
                m_bXML = true;
            }
            this.DialogResult = res;
            this.Close();
        }

        private void btnMysql_Click(object sender, EventArgs e)
        {
            this.Text = "Connect to Database";
            gbGender.Visible = true;
            rbFemale.Visible = true;
            rbMale.Visible = true;
            btnSubmit.Visible = true;
            btnExit.Visible = true;
            tbServer.Visible = true;
            tbUser.Visible = true;
            tbDb.Visible = true;
            lblServer.Visible = true;
            lblPort.Visible = true;
            lblDBase.Visible = true;
            tbPass.Visible = true;
            tbPort.Visible = true;
            lblUsername.Visible = true;
            lblPass.Visible = true;
            menuStrip1.Visible = true;
            connectToolStripMenuItem.Visible = true;

            btnMysql.Visible = false;
            btnXml.Visible = false;

            m_bXML = false;
        }
    }
}
