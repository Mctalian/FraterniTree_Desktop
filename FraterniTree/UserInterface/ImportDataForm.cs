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

        public ImportDataForm( )
        {
            ImportData_Initialize();

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
                sqlToolStrip.Click += ImportData_Menu_UseRecent_onClick;
                connectToolStripMenuItem.DropDownItems.Add(sqlToolStrip);
                connectToolStripMenuItem.Enabled = true;
            }

            if( Settings.Default.RecentXmlPath == string.Empty ) return;

            var xmlToolStrip = new ToolStripMenuItem(Path.GetFileName(Settings.Default.RecentXmlPath));
            xmlToolStrip.Click += ImportData_Menu_UseRecent_onClick;
            connectToolStripMenuItem.DropDownItems.Add(xmlToolStrip);
            connectToolStripMenuItem.Enabled = true;
        }

        private void DatabaseValidator( )
        {
            if( inputValid.Any(b => !b) ) return; 

            btnSubmit.Enabled = true;
        }

        private void ImportData_Server_onChange(object sender, EventArgs e)
        {
            inputValid[0] = tbServer.Text != string.Empty;

            DatabaseValidator();
        }

        private void ImportData_Port_onChange(object sender, EventArgs e)
        {
            if( tbPort.Text.IndexOfAny( new[] {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9'} ) >= 0 ) 
            {
                inputValid[1] = true;
            }
            else
            {
                inputValid[1] = false;
            }

            DatabaseValidator();
        }

        private void ImportData_Database_onChange(object sender, EventArgs e)
        {
            inputValid[2] = tbDb.Text != string.Empty;

            DatabaseValidator();
        }

        private void ImportData_Username_onChange(object sender, EventArgs e)
        {
            inputValid[3] = tbUser.Text != string.Empty;

            DatabaseValidator();
        }

        private void ImportData_Password_onChange(object sender, EventArgs e)
        {
            inputValid[4] = tbPass.Text != string.Empty;

            DatabaseValidator();
        }

        private void ImportData_Submit_onClick(object sender, EventArgs e)
        {
            Port = int.Parse( tbPort.Text );
            Server = tbServer.Text;
            Base = tbDb.Text;
            Username = tbUser.Text;
            Password = tbPass.Text;
            Close();
        }

        private void ImportData_Exit_onClick(object sender, EventArgs e)
        {
            Close();
        }

        private void ImportData_FraternityOrSorority_onChange(object sender, EventArgs e)
        {
            IsMale = rbMale.Checked;
        }

        private void ImportData_Menu_UseRecent_onClick(object sender, EventArgs e)
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

        private void ImportData_ChooseXml_onClick(object sender, EventArgs eventArgs)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = Util.GetLocalizedString( "XMLDocumentFilter" ),
                DefaultExt = Util.GetLocalizedString( "DotXml" ),
                AddExtension = true,
                CheckFileExists = true
            };

            var res = openFileDialog.ShowDialog();
            if( res == DialogResult.OK )
            {
                FilePath = openFileDialog.FileName;
                if( !File.Exists( openFileDialog.FileName ) )
                {
                    ParentNode = Interaction.InputBox(Util.GetLocalizedString("PromptUSerForParentNodeName"),
                        Util.GetLocalizedString("ParentNodeName"),
                        Util.GetLocalizedString("MyTree"));
                }
                else
                {
                    var tmpDoc = new XmlDocument();
                    tmpDoc.Load( openFileDialog.FileName );
                    ParentNode = tmpDoc.DocumentElement.Name;
                }
                Settings.Default.RecentXmlPath = FilePath;
            }

            IsXml = true;
            DialogResult = res;
            Close();
        }

        private void ImportData_ChooseSql_onClick(object sender, EventArgs e)
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

        private readonly bool[] inputValid = new bool[5];
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

    }

}