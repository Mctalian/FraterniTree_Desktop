using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using FraterniTree.Properties;
using Microsoft.VisualBasic;
using MySql.Data.MySqlClient;

namespace FraterniTree.UserInterface
{

    public partial class ImportDataForm : Form
    {
        private readonly bool[] inputValid = new bool[5];

        public ImportDataForm( )
        {
            InitializeComponent();

            Port = 0;
            IsMale = true;
            Server = string.Empty;
            Base = string.Empty;
            Username = string.Empty;
            Password = string.Empty;
            Connection = null;

            if( Settings.Default.RecentMySqlConnection != null )
            {
                var sqlToolStrip = new ToolStripMenuItem( Settings.Default.RecentMySqlConnection.Database );
                sqlToolStrip.Click += genericToolStripMenuItem_Click;
                connectToolStripMenuItem.DropDownItems.Add(sqlToolStrip);
                connectToolStripMenuItem.Enabled = true;
            }

            if( Settings.Default.RecentXmlPath == string.Empty ) return;

            var xmlToolStrip = new ToolStripMenuItem(Path.GetFileName(Settings.Default.RecentXmlPath));
            xmlToolStrip.Click += genericToolStripMenuItem_Click;
            connectToolStripMenuItem.DropDownItems.Add(xmlToolStrip);
            connectToolStripMenuItem.Enabled = true;
        }

        public int Port { get; private set; }
        public bool IsXml { get; private set; }
        public bool IsMale { get; private set; }
        public string Server { get; private set; }
        public string Base { get; private set; }
        public string Username { get; private set; }
        public string Password { get; private set; }
        public string FilePath { get; private set; }
        public string ParentNode { get; private set; }
        public MySqlConnection Connection { get; private set; }

        private void TbValidator( )
        {
            var ready = true;
            
            if( inputValid.Any( b => !b ) ) 
            { //TODO
                ready = false;
                return; //TODO
            }

            btnSubmit.Enabled = ready;
        }

        private void tbServer_TextChanged(object sender, EventArgs e)
        {
            if( tbServer.Text != string.Empty ) {
                inputValid[0] = true;
            }
            else
            {
                inputValid[0] = false;
            }

            TbValidator();
        }

        private void tbPort_TextChanged(object sender, EventArgs e)
        {
            if( tbPort.Text.IndexOfAny( new char[10] {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9'} ) >= 0 ) {
                inputValid[1] = true;
            }
            else
            {
                inputValid[1] = false;
            }

            TbValidator();
        }

        private void tbDb_TextChanged(object sender, EventArgs e)
        {
            if( tbDb.Text != string.Empty ) 
            {
                inputValid[2] = true;
            }
            else
            {
                inputValid[2] = false;
            }

            TbValidator();
        }

        private void tbUser_TextChanged(object sender, EventArgs e)
        {
            if( tbUser.Text != string.Empty ) 
            {
                inputValid[3] = true;
            }
            else
            {
                inputValid[3] = false;
            }

            TbValidator();
        }

        private void tbPass_TextChanged(object sender, EventArgs e)
        {
            if( tbPass.Text != string.Empty ) 
            {
                inputValid[4] = true;
            }
            else
            {
                inputValid[4] = false;
            }

            TbValidator();
        }

        private void btnSubmit_Click(object sender, EventArgs e)
        {
            Port = int.Parse( tbPort.Text );
            Server = tbServer.Text;
            Base = tbDb.Text;
            Username = tbUser.Text;
            Password = tbPass.Text;
            Close();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void rbMale_CheckedChanged(object sender, EventArgs e)
        {
            IsMale = rbMale.Checked;
        }

        private void genericToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if( ((ToolStripMenuItem) sender).Text == Path.GetFileName( Settings.Default.RecentXmlPath ) )
            {
                FilePath = Settings.Default.RecentXmlPath;
                IsXml = true;
                DialogResult = DialogResult.OK;
                Close();
            }
            else if( ((ToolStripMenuItem) sender).Text == Settings.Default.RecentMySqlConnection.Database ) 
            {
                Connection = Settings.Default.RecentMySqlConnection;
            }
            else
            {
                DialogResult = DialogResult.Abort;
                Close();
            }
        }

        private void btnXml_Click(object sender, EventArgs eventArgs)
        {
            var ofd = new OpenFileDialog
            {
                Filter = Util.GetLocalizedString( "XMLDocumentFilter" ),
                DefaultExt = Util.GetLocalizedString( "DotXml" ),
                AddExtension = true,
                CheckFileExists = true
            };

            var res = ofd.ShowDialog();
            if( res == DialogResult.OK )
            {
                FilePath = ofd.FileName;
                if( !File.Exists( ofd.FileName ) )
                {
                    ParentNode = Interaction.InputBox(Util.GetLocalizedString("PromptUSerForParentNodeName"),
                        Util.GetLocalizedString("ParentNodeName"),
                        Util.GetLocalizedString("MyTree"));
                }
                else
                {
                    var tmpDoc = new XmlDocument();
                    tmpDoc.Load( ofd.FileName );
                    ParentNode = tmpDoc.DocumentElement.Name;
                }
                Settings.Default.RecentXmlPath = FilePath;
            }

            IsXml = true;

            DialogResult = res;
            Close();
        }

        private void btnMysql_Click(object sender, EventArgs e)
        {
            Text = Util.GetLocalizedString("ConnectToDatabase");
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

            IsXml = false;
        }

        #region Assembly Attribute Accessors

        public string AssemblyTitle
        {
            get
            {
                var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes( typeof (AssemblyTitleAttribute), false );

                if( attributes.Length > 0 )
                {
                    var titleAttribute = (AssemblyTitleAttribute) attributes[0];
                    if( titleAttribute.Title != string.Empty )
                    {
                        return titleAttribute.Title;
                    }
                }

                return Path.GetFileNameWithoutExtension( Assembly.GetExecutingAssembly().CodeBase );
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
                var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes( typeof (AssemblyDescriptionAttribute), false );
                
                if( attributes.Length == 0 ) return string.Empty; 

                return ((AssemblyDescriptionAttribute) attributes[0]).Description;
            }
        }

        public string AssemblyProduct
        {
            get
            {
                var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes( typeof (AssemblyProductAttribute), false );
                
                if( attributes.Length == 0 ) return string.Empty; 

                return ((AssemblyProductAttribute) attributes[0]).Product;
            }
        }

        public string AssemblyCopyright
        {
            get
            {
                var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes( typeof (AssemblyCopyrightAttribute), false );
                
                if( attributes.Length == 0 ) { return string.Empty; }
                
                return ((AssemblyCopyrightAttribute) attributes[0]).Copyright;
            }
        }

        public string AssemblyCompany
        {
            get
            {
                var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes( typeof (AssemblyCompanyAttribute), false );

                if( attributes.Length == 0 ) { return string.Empty; }

                return ((AssemblyCompanyAttribute) attributes[0]).Company;
            }
        }

        #endregion
    }

}