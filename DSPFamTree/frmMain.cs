using DataProtectionSimple;
using Microsoft.VisualBasic;
using MySql.Data.MySqlClient;
using RegistryAccess;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using TreeDisplay;

namespace FraterniTree
{
    public partial class frmMain : Form
    {

        #region Public Data
        public static Brother root;
        public static AutoCompleteStringCollection CurrentBrothers = new AutoCompleteStringCollection();
        #endregion

        #region Private Data

        #region Mysql-Specific Data
        private const string INSERT_INTO_STM = "INSERT INTO Brothers (Last, First, IniMonth, IniYear, Big, NextSibling, FirstLittle)" +
                                               "VALUES (@Last, @First, @IniMonth, @IniYear, @Big, @NextSibling, @FirstLittle)" +
                                               "ON DUPLICATE KEY UPDATE IniMonth=values(IniMonth), IniYear=values(IniYear), " +
                                               "Big=values(Big), NextSibling=values(NextSibling), FirstLittle=values(FirstLittle)";
        private MySqlConnection DbConnect    = null;
        #endregion

        #region XML-Specific Data
        private XmlDocument XmlDoc       = null;
        private string OpenedXmlFilePath = "";
        private string XmlParentNodeName = "";
        #endregion

        #region Gender-Dependent Strings
        private const string MALE_SIBLING    = "Brother";
        private const string FEMALE_SIBLING  = "Sister";
        private const string MALE_FRM_NAME   = "Fraterni";
        private const string FEMALE_FRM_NAME = "Sorori";
        private string GenderDependentName   = MALE_FRM_NAME;
        #endregion

        #region Tree Display Data
        private Brother Selected    = null;
        private Brother TreeRoot    = null;
        private int PrevSelectedInd = -1;

        private const float ZOOM_FACTOR = 1.5F;
        private int ZoomLevel           = 0;

        private int MaximumWidth      = 0;
        private int VerticalSpacing   = 25;
        private int HorizontalSpacing = 5;
        private int MaxGeneration     = 1;
        #endregion

        #region Flags
        private bool bIsMale        = true;
        private bool bIsXml         = false;
        private bool DisplayApex    = false;
        private bool FixedWidth     = false;
        private bool WriteBackReady = false;
        #endregion

        #endregion
        
        public frmMain()
        {
            InitializeComponent();
        }

        private void PopulateBrothers(bool IsXml)
        {
            if (IsXml)
            {
                XmlDoc = new XmlDocument();
                ImportFromXml();
                RefreshNoBigListBox(root);
            }
            else if (DbConnect != null)
            {
                string stm = "SELECT * FROM Brothers";
                MySqlCommand cmd = new MySqlCommand(stm, DbConnect);
                DbConnect.Open();
                MySqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    string bigName = rdr.GetString(4);
                    string sibling = rdr.GetString(5);
                    string little = rdr.GetString(6);
                    string last = rdr.GetString(0);
                    string first = rdr.GetString(1);
                    string month = rdr.GetString(2);
                    int year = rdr.GetInt32(3);
                    int space = bigName.LastIndexOf(' ');

                    Brother tmpBig = null;
                    Brother tmp = root.FindBrotherByName(bigName);
                    if (tmp != null)
                    {
                        tmpBig = tmp;
                        if (tmpBig.GetNodeRef().Parent() == root.GetNodeRef() || tmpBig.GetNodeRef().Parent() == null && tmpBig != root)
                        {
                            RefreshNoBigListBox(root);
                        }
                    }
                    else
                    {
                        tmpBig = new Brother(bigName.Substring(space + 1), bigName.Substring(0, space), "Fall", 1920);
                        tmpBig.m_SelectCallback = PopulateBrotherEdit;
                        tmpBig.m_DeleteCallback = RemoveBrotherFromTree;
                        root.GetNodeRef().AddChild(tmpBig.GetNodeRef());
                        if (!cbTreeParent.Items.Contains(tmpBig.GetFullName()))
                        {
                            cbTreeParent.Items.Add(tmpBig.GetFullName());
                        }
                        if (!CurrentBrothers.Contains(tmpBig.GetFullName()))
                        {
                            CurrentBrothers.Add(tmpBig.GetFullName());
                        }
                        RefreshNoBigListBox(root);
                    }

                    Brother newB = null;
                    tmp = root.FindBrotherByName(first + " " + last);
                    if (tmp != null)
                    {
                        newB = tmp;
                        if (newB.GetNodeRef().HasParent() && newB.GetNodeRef().Parent() == tmpBig.GetNodeRef())
                        {
                            // Do nothing
                        }
                        else
                        {
                            tmpBig.GetNodeRef().AddChild(newB.GetNodeRef());
                        }
                        newB.m_IniMonth = month;
                        newB.m_IniYear = year;
                    }
                    else
                    {
                        newB = new Brother(last, first, month, year);
                        newB.m_SelectCallback = PopulateBrotherEdit;
                        newB.m_DeleteCallback = RemoveBrotherFromTree;
                        tmpBig.GetNodeRef().AddChild(newB.GetNodeRef());
                    }

                    if (!cbTreeParent.Items.Contains(newB.GetFullName()))
                    {
                        cbTreeParent.Items.Add(newB.GetFullName());
                    }
                    if (!CurrentBrothers.Contains(newB.GetFullName()))
                    {
                        CurrentBrothers.Add(newB.GetFullName());
                    }
                    RefreshNoBigListBox(root);
                }
                rdr.Close();
                while (!rdr.IsClosed) ;
                DbConnect.Close();
                //while (root.GetNodeRef().HasParent())
                //{
                //    root = root.GetNodeRef().Parent();
                //}
            }
            if (root.GetNodeRef().HasChild())
            {
                updwnNumGen.Enabled = true;
                cbTreeParent.Enabled = true;
            }
            cbTreeParent.Sorted = true;
        }

        private void RefreshNoBigListBox(Brother b)
        {
            if (b == root)
            {
                lbNoRelation.Items.Clear();
                RefreshNoBigListBox((Brother)(b.GetNodeRef().FirstChild().GetUserData()));
                return;
            }

            if (b.GetNodeRef().HasChild() || b.GetNodeRef().HasRightSibling())
            {
                if (b.GetNodeRef().Parent() == root.GetNodeRef())
                {
                    if (!lbNoRelation.Items.Contains(b.GetFullName()))
                    {
                        lbNoRelation.Items.Add(b.GetFullName());
                    }
                }
                if (b.GetNodeRef().HasRightSibling())
                {
                    RefreshNoBigListBox((Brother)(b.GetNodeRef().RightSibling().GetUserData()));
                }
                if (b.GetNodeRef().HasChild())
                {
                    RefreshNoBigListBox((Brother)(b.GetNodeRef().FirstChild().GetUserData()));
                }
            }
            if (!cbTreeParent.Items.Contains(b.GetFullName()))
            {
                cbTreeParent.Items.Add(b.GetFullName());
            }
            if (!CurrentBrothers.Contains(b.GetFullName()))
            {
                CurrentBrothers.Add(b.GetFullName());
            }
        }

        private void RemoveBrotherFromTree(Brother b)
        {
            string name = b.GetFullName();
            b = null;
            lbNoRelation.Items.Remove(name);
            cbTreeParent.Items.Remove(name);
            CurrentBrothers.Remove(name);
            RefreshNoBigListBox(root);
            CreateTree();
            PostCreationShift();
        }

        #region Graphical Tree Display

        private void AddLabelsToPanel(Brother parent, int g)
        {
            if (g >= 0)
            {
                int count = parent.GetNodeRef().GetNumberOfChildren();
                parent.GetNodeRef().m_Prelim = 0;
                parent.GetNodeRef().m_Modifier = 0;
                parent.m_Label.Visible = false;
                parent.m_Label.AutoSize = !FixedWidth;
                pnlTree.Controls.Add(parent.m_Label);
                if (parent.m_Label.AutoSize)
                {
                    MaximumWidth = Math.Max(MaximumWidth, parent.m_Label.Width);
                    parent.GetNodeRef().SetWidth(parent.m_Label.Width);
                }
                else
                {
                    parent.m_Label.AutoSize = true;
                    parent.m_Label.Parent.Refresh();
                    MaximumWidth = Math.Max(MaximumWidth, parent.m_Label.Width);
                    parent.m_Label.AutoSize = false;
                }

                for (int i = 0; i < count; i++)
                {
                    AddLabelsToPanel((Brother)parent.GetNodeRef()[i].GetUserData(), g - 1);
                }
            }
        }

        private void CreateTree()
        {
            HideSelectedEdit();
            UnzoomControls();

            if (Selected != null)
            {
                Selected.m_Label.BackColor = System.Drawing.Color.White;
            }

            Selected = null;

            pnlTree.Controls.Clear();

            if (cbTreeParent.Text != "*All*" && cbTreeParent.Text != "")
            {
                TreeRoot = root.FindBrotherByName(cbTreeParent.Text);
            }
            else if (cbTreeParent.Text == "")
            {
                return;
            }
            else
            {
                TreeRoot = root;
            }

            Size s = TreeRoot.m_Label.Size;
            Point p = new Point((splitTreeInfo.Panel1.Width / 2) - (s.Width / 2), 2);
            TreeRoot.GetNodeRef().SetXCoord(p.X);
            TreeRoot.GetNodeRef().SetYCoord(p.Y);
            int gens = (TreeRoot.GetNodeRef().GetNumGenerations());
            updwnNumGen.Maximum = gens;

            MaximumWidth = 0;
            AddLabelsToPanel(TreeRoot, MaxGeneration);
            if (FixedWidth)
            {
                SetLabelWidths(TreeRoot, MaxGeneration);
            }

            WalkerAlgorithmTree.LevelSeparation = (int)updwnVertSpace.Value;
            WalkerAlgorithmTree.MaxDepth = (int)updwnNumGen.Value;
            WalkerAlgorithmTree.SiblingSeparation = (int)updwnHorizSpace.Value;
            WalkerAlgorithmTree.SubtreeSeparation = (int)updwnSubTree.Value;
            WalkerAlgorithmTree.PositionTree(TreeRoot.GetNodeRef());
        }

        private void PostCreationShift()
        {
            int leastPosX = 0;
            int leastPosY = 0;

            foreach (Control c in pnlTree.Controls)
            {

                c.Location = new Point(c.Location.X - (c.Width / 2), c.Location.Y);
                if (c.Location.X < 0)
                {
                    leastPosX = Math.Max(leastPosX, -c.Location.X);
                }
                if (c.Location.Y < 0)
                {
                    leastPosY = Math.Max(leastPosY, -c.Location.Y);
                }
            }

            if (leastPosX > 0 || leastPosY > 0)
            {
                foreach (Control c in pnlTree.Controls)
                {
                    c.Location = new Point(c.Location.X + leastPosX, c.Location.Y + leastPosY);
                    c.Visible = true;
                }
            }
            else
            {
                foreach (Control c in pnlTree.Controls)
                {
                    c.Visible = true;
                }
            }

            if (!DisplayApex)
            {
                if (root.m_Label.Parent != null)
                {
                    root.m_Label.Parent = null;
                    leastPosY = root.m_Label.Height + VerticalSpacing;
                    foreach (Control c in pnlTree.Controls)
                    {
                        c.Location = new Point(c.Location.X, c.Location.Y - leastPosY);
                    }
                }
            }
        }

        private void SetLabelWidths(Brother parent, int g)
        {
            if (g >= 0)
            {
                int count = parent.GetNodeRef().GetNumberOfChildren();

                parent.m_Label.Width = MaximumWidth;
                parent.GetNodeRef().SetWidth(parent.m_Label.Width);

                for (int i = 0; i < count; i++)
                {
                    SetLabelWidths((Brother)parent.GetNodeRef()[i].GetUserData(), g - 1);
                }
            }
        }

        private void UnzoomControls()
        {
            float ZoomFactor = 1;
            if (ZoomLevel < 0)
            {
                ZoomFactor = (float)Math.Pow(ZOOM_FACTOR, (-ZoomLevel));
            }
            else if (ZoomLevel > 0)
            {
                ZoomFactor = (float)Math.Pow(1 / ZOOM_FACTOR, ZoomLevel);
            }
            else
            {
                return;
            }
            foreach (Control c in pnlTree.Controls)
            {
                Label l = (Label)(c);
                l.Font = new Font(l.Font.FontFamily, l.Font.Size * ZoomFactor);
                SizeF sf = new SizeF(ZoomFactor, ZoomFactor);
                c.Scale(sf);
            }
            ZoomLevel = 0;
        }

        #endregion

        #region Selected Member Information Edit

        private void PopulateBrotherEdit(Brother b)
        {
            splitTreeInfo.Panel2Collapsed = false;

            cbSelectedTerm.Enabled = false;
            dtpSelectedYear.Enabled = false;
            tbSelectedFirst.Enabled = false;
            tbSelectedLast.Enabled = false;
            tbSelectedBig.Enabled = false;
            tbSelectedLittles.Enabled = false;
            btnApplySelected.Enabled = false;
            btnCancelSelected.Enabled = false;
            chbActive.Enabled = false;

            tbSelectedFirst.Text = b.m_First;
            tbSelectedLast.Text = b.m_Last;
            tbSelectedBig.Text = b.GetNodeRef().HasParent() ? b.GetNodeRef().Parent().GetText() : "";
            if (b.GetNodeRef().HasChild())
            {
                tbSelectedLittles.Text = b.GetNodeRef().FirstChild().GetText();

                Brother l = (Brother)(b.GetNodeRef().FirstChild().GetUserData());
                for (int i = 1; i < b.GetNodeRef().GetNumberOfChildren(); i++)
                {
                    tbSelectedLittles.Text += Environment.NewLine + l.GetNodeRef().RightSibling().GetText();
                    l = (Brother)(l.GetNodeRef().RightSibling().GetUserData());
                }
            }
            else
            {
                tbSelectedLittles.Text = "";
            }

            dtpSelectedYear.Value = new DateTime(b.m_IniYear, 1, 1);
            if (b.m_IniMonth != "" || b.m_IniMonth != null)
            {
                cbSelectedTerm.SelectedItem = b.m_IniMonth;
            }

            chbActive.Checked = b.isActiveBrother;

            if (Selected != null && Selected != b)
            {
                Selected.m_Label.BackColor = System.Drawing.Color.White;
            }
            Selected = b;

            btnEditSelected.Enabled = true;
        }

        private void HideSelectedEdit()
        {
            splitTreeInfo.Panel2Collapsed = true;
        }

        #endregion

        #region MySql-Specific Methods

        private void AddDBToReg(string server, int port, string dbName, string uName, string pWord)
        {
            string salt = "1950";

            RegistryAccess.RegAccess.SetStringRegistryValue("db", dbName, dbName);
            RegistryAccess.RegAccess.SetStringRegistryValue("port", port.ToString(), dbName);
            RegistryAccess.RegAccess.SetStringRegistryValue("server", server, dbName);
            RegistryAccess.RegAccess.SetStringRegistryValue("user", uName, dbName);
            RegistryAccess.RegAccess.SetStringRegistryValue("pass", DP.Encrypt(pWord, salt), dbName);
        }

        private bool ConnectDB(string server, int port, string dbName, string uName, string pWord)
        {
            bool ret = true;
            string cs = @"server=" + server + ";port=" + port + ";userid=" + uName + ";password=" + pWord + ";database=" + dbName + ";allow user variables=true";
            try
            {
                DbConnect = new MySqlConnection(cs);
                DbConnect.Open();
                if (DbConnect.State == ConnectionState.Open)
                {
                    DbConnect.Close();
                    AddDBToReg(server, port, dbName, uName, pWord);
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show(ex.Message);
                ret = false;
            }
            return ret;
        }

        private void WriteBackToDB(Brother currentParent)
        {
            MySqlCommand cmd = null;
            try
            {
                if (DbConnect != null)
                {
                    if (currentParent.GetNodeRef().HasChild())
                    {
                        WriteBackToDB((Brother)(currentParent.GetNodeRef().FirstChild().GetUserData()));
                    }

                    if (currentParent.GetNodeRef().HasRightSibling())
                    {
                        WriteBackToDB((Brother)(currentParent.GetNodeRef().RightSibling().GetUserData()));
                    }

                    if (currentParent != root)
                    {
                        DbConnect.Open();
                        cmd = new MySqlCommand(INSERT_INTO_STM, DbConnect);
                        cmd.Prepare();
                        cmd.Parameters.AddWithValue("@Last", currentParent.m_Last);
                        cmd.Parameters.AddWithValue("@First", currentParent.m_First);
                        cmd.Parameters.AddWithValue("@IniMonth", currentParent.m_IniMonth);
                        cmd.Parameters.AddWithValue("@IniYear", currentParent.m_IniYear);

                        if (currentParent.GetNodeRef().HasParent())
                        {
                            cmd.Parameters.AddWithValue("@Big", currentParent.GetNodeRef().Parent().GetText());
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@Big", "");
                        }
                        if (currentParent.GetNodeRef().HasRightSibling())
                        {
                            cmd.Parameters.AddWithValue("@NextSibling", currentParent.GetNodeRef().RightSibling().GetText());
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@NextSibling", "");
                        }
                        if (currentParent.GetNodeRef().HasChild())
                        {
                            cmd.Parameters.AddWithValue("@FirstLittle", currentParent.GetNodeRef().FirstChild().GetText());
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@FirstLittle", "");
                        }
                        cmd.ExecuteNonQuery();
                        DbConnect.Close();
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + '\n' + cmd.CommandText);
            }
        }

        #endregion

        #region XML-Specific Methods

        private string ConvertTreeToXml(Brother B)
        {
            // Begin the Tag
            string xmlData = "<Brother ";

            // Attributes
            xmlData += "Last=\"" + B.m_Last + "\" ";
            xmlData += "First=\"" + B.m_First + "\" ";
            xmlData += "IniTerm=\"" + B.m_IniMonth + "\" ";
            xmlData += "IniYear=\"" + B.m_IniYear + "\" ";

            xmlData += ">";

            for (int i = B.GetNodeRef().GetNumberOfChildren() - 1; i >= 0; i--)
            {
                xmlData += ConvertTreeToXml((Brother)B.GetNodeRef()[i].GetUserData());
            }

            // End the Tag
            xmlData += "</Brother>";

            return xmlData;
        }
        
        private Brother ConvertXmlToTree(XmlNode currentParent)
        {
            Brother big = new Brother(currentParent.Attributes["Last"].Value,
                                      currentParent.Attributes["First"].Value,
                                      currentParent.Attributes["IniTerm"].Value,
                                      Int32.Parse(currentParent.Attributes["IniYear"].Value));

            big.m_SelectCallback = PopulateBrotherEdit;
            big.m_DeleteCallback = RemoveBrotherFromTree;
            ttTree.SetToolTip(big.m_Label, "Left Click to Select, Right Click to hide descendents.");

            foreach (XmlNode child in currentParent.ChildNodes)
            {
                big.GetNodeRef().AddChild(ConvertXmlToTree(child).GetNodeRef());
            }

            return big;
        }

        private void ExportToXml(string filePath, string parentNodeName)
        {
            // Xml Document Header
            string xmlDoc = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" +
                               "<" + parentNodeName + ">";
            // Fill it with all the nodes
            xmlDoc += ConvertTreeToXml(root);

            // Close the Document Parent Node
            xmlDoc += "</" + parentNodeName + ">";

            XmlDocument tmp = new XmlDocument();
            tmp.LoadXml(xmlDoc);
            //File.WriteAllText(filePath, xmlDoc);
            XmlTextWriter xWriter = new XmlTextWriter(filePath, Encoding.UTF8);
            xWriter.Formatting = Formatting.Indented;
            tmp.Save(xWriter);
        }

        private void ImportFromXml()
        {
            if (!File.Exists(OpenedXmlFilePath))
            {
                // Xml Document Header
                string xmlData = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n" +
                                   "<" + XmlParentNodeName + ">\n";

                // Begin the Root Tag
                xmlData += "<Brother ";

                // Attributes
                xmlData += "Last=\"Tonsor Jr.\" ";
                xmlData += "First=\"Charles A.\" ";
                xmlData += "IniTerm=\"Winter\" ";
                xmlData += "IniYear=\"1899\" ";

                xmlData += ">\n";

                // End the Tag
                xmlData += "</Brother>\n";

                // Close the Document Parent Node
                xmlData += "</" + XmlParentNodeName + ">";

                File.WriteAllText(OpenedXmlFilePath, xmlData);
            }

            // Load XML Document
            XmlDoc.Load(OpenedXmlFilePath);

            if (XmlDoc.DocumentElement.ChildNodes.Count == 1)
            {
                // Should only be one, the root
                XmlNode currentParent = XmlDoc.DocumentElement.FirstChild;
                if (root == null)
                {
                    root = new Brother(currentParent.Attributes["Last"].Value,
                                       currentParent.Attributes["First"].Value,
                                       currentParent.Attributes["IniTerm"].Value,
                                       Int32.Parse(currentParent.Attributes["IniYear"].Value));
                    root.m_SelectCallback = PopulateBrotherEdit;
                    root.m_DeleteCallback = RemoveBrotherFromTree;
                }
                foreach (XmlNode child in currentParent.ChildNodes)
                {
                    root.GetNodeRef().AddChild(ConvertXmlToTree(child).GetNodeRef());
                }
                saveXmlToolStripMenuItem.Enabled = true;
            }
            else
            {
                throw new Exception("More than one root node, please check your XML and try again.");
            }
        }

        #endregion

        #region Screenshot Methods

        private void TakeScreenshot(Panel panel, string filePath)
        {
            if (panel == null)
                throw new ArgumentNullException("panel");

            if (filePath == null)
                throw new ArgumentNullException("filePath");

            // get parent form (may not be a direct parent)
            Form form = panel.FindForm();
            if (form == null)
                throw new ArgumentException(null, "panel");

            // remember form position
            int w = form.Width;
            int h = form.Height;
            int l = form.Left;
            int t = form.Top;

            // get panel virtual size
            Rectangle display = panel.DisplayRectangle;

            // get panel position relative to parent form
            Point panelLocation = panel.PointToScreen(panel.Location);
            Size panelPosition = new Size(panelLocation.X - form.Location.X, panelLocation.Y - form.Location.Y);

            // resize form and move it outside the screen
            int neededWidth = panelPosition.Width + display.Width;
            int neededHeight = panelPosition.Height + display.Height;
            form.SetBounds(0, -neededHeight, neededWidth, neededHeight, BoundsSpecified.All);

            // resize panel (useless if panel has a dock)
            int pw = panel.Width;
            int ph = panel.Height;
            panel.SetBounds(0, 0, display.Width, display.Height, BoundsSpecified.Size);

            // render the panel on a bitmap
            try
            {
                Bitmap bmp = new Bitmap(display.Width, display.Height);
                panel.DrawToBitmap(bmp, display);
                bmp.Save(filePath);
            }
            finally
            {
                // restore
                panel.SetBounds(0, 0, pw, ph, BoundsSpecified.Size);
                form.SetBounds(l, t, w, h, BoundsSpecified.All);
            }
        }

        private Image CaptureScreen()
        {
            // ------------- OLD WAY, KEEP FOR NOW ------------- //
            //Graphics myGraphics = pnlTree.CreateGraphics();
            //Image panelImage = new Bitmap(pnlTree.DisplayRectangle.Width, pnlTree.DisplayRectangle.Height, myGraphics);
            //Graphics memoryGraphics = Graphics.FromImage(panelImage);
            //IntPtr dc1 = myGraphics.GetHdc();
            //IntPtr dc2 = memoryGraphics.GetHdc();
            //BitBlt(dc2, 0, 0, pnlTree.DisplayRectangle.Width, pnlTree.DisplayRectangle.Height, dc1, 0, 0, 13369376);
            //myGraphics.ReleaseHdc(dc1);
            //memoryGraphics.ReleaseHdc(dc2);
            Point old = pnlTree.Location;
            pnlTree.Location = new Point(0, 0);
            Bitmap panelImage = new Bitmap(pnlTree.Width, pnlTree.Height);
            pnlTree.DrawToBitmap(panelImage, new Rectangle(pnlTree.Location.X, pnlTree.Location.Y, pnlTree.Width, pnlTree.Height));
            pnlTree.Location = old;
            return panelImage;
        }

        #endregion

        #region GUI Event Handlers

        #region Main Form

        private void frmMain_Load(object sender, EventArgs e)
        {
            StartUp start = new StartUp();
            start.ShowDialog();
            if (start.DialogResult == System.Windows.Forms.DialogResult.Abort)
            {
                this.Close();
                return;
            }

            DisplayApex = displayRootOfAllTreeToolStripMenuItem.Checked;

            bIsXml = start.m_bXML;
            if (bIsXml)
            {
                OpenedXmlFilePath = start.m_FilePath;
                XmlParentNodeName = start.m_ParentNode;
                saveXmlToolStripMenuItem.Enabled = true;
            }
            else
            {

                root = new Brother("Tonsor Jr.", "Charles A", "Winter", 1899);

                bIsMale = start.m_bIsMale;
                string server = start.m_Server;
                string db = start.m_DBase;
                string user = start.m_UName;
                string pword = start.m_PWord;
                int portNum = start.m_Port;

                bool ret;

                ret = ConnectDB(server, portNum, db, user, pword);

                if (!ret)
                {
                    this.Close();
                    return;
                }
            }

            if (bIsMale)
            {
                GenderDependentName = MALE_FRM_NAME;
            }
            else
            {
                GenderDependentName = FEMALE_FRM_NAME;
            }
            PopulateBrothers(bIsXml);
            tbBig.AutoCompleteCustomSource = CurrentBrothers;
            tbSelectedBig.AutoCompleteCustomSource = CurrentBrothers;
            tbSelectedLittles.AutoCompleteCustomSource = CurrentBrothers;
            tbLittles.AutoCompleteCustomSource = CurrentBrothers;
            this.Text = GenderDependentName + "Tree" + (XmlParentNodeName != "" ? " - " + XmlParentNodeName : "");
            root.m_SelectCallback = PopulateBrotherEdit;
            root.m_DeleteCallback = RemoveBrotherFromTree;
            if (!bIsXml)
            {
                WriteBackReady = true;
            }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (WriteBackReady)
            {
                WriteBackToDB(root);
            }
            this.Dispose();
        }

        #endregion

        #region Buttons

        #region Add Member Sub-Panel

        private void btnAdd_Click(object sender, EventArgs e)
        {
            string bigName = tbBig.Text;
            string[] littles = tbLittles.Text.Split(new Char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            string last = tbLastName.Text;
            string first = tbFirstName.Text;
            string month = cbIniMonth.Text;
            int year = Int32.Parse(dtpIniYear.Text);
            if (bigName == "")
            {
                bigName = root.GetFullName();
            }
            int space = bigName.LastIndexOf(' ');
            Brother tmpBig = null;
            Brother tmp = root.FindBrotherByName(bigName);
            if (tmp != null)
            {
                tmpBig = tmp;
            }
            else
            {
                tmpBig = new Brother(bigName.Substring(space + 1), bigName.Substring(0, space), "Fall", 1920);
                tmpBig.m_SelectCallback = PopulateBrotherEdit;
                tmpBig.m_DeleteCallback = RemoveBrotherFromTree;
                root.GetNodeRef().AddChild(tmpBig.GetNodeRef());
                if (!cbTreeParent.Items.Contains(tmpBig.GetFullName()))
                {
                    cbTreeParent.Items.Add(tmpBig.GetFullName());
                }
                if (!CurrentBrothers.Contains(tmpBig.GetFullName()))
                {
                    CurrentBrothers.Add(tmpBig.GetFullName());
                }
                RefreshNoBigListBox(root);
            }

            Brother newB = null;
            tmp = root.FindBrotherByName(first + " " + last);
            if (tmp != null)
            {
                newB = tmp;
                tmpBig.GetNodeRef().AddChild(newB.GetNodeRef());
            }
            else
            {
                newB = new Brother(last, first, month, year);
                newB.m_SelectCallback = PopulateBrotherEdit;
                newB.m_DeleteCallback = RemoveBrotherFromTree;
                tmpBig.GetNodeRef().AddChild(newB.GetNodeRef());
            }

            if (!cbTreeParent.Items.Contains(newB.GetFullName()))
            {
                cbTreeParent.Items.Add(newB.GetFullName());
            }
            if (!CurrentBrothers.Contains(newB.GetFullName()))
            {
                CurrentBrothers.Add(newB.GetFullName());
            }
            RefreshNoBigListBox(root);

            Brother litt = null;
            for (int i = 0; i < littles.Count(); i++)
            {
                space = littles[i].LastIndexOf(' ');
                tmp = root.FindBrotherByName(littles[i]);
                if (tmp != null)
                {
                    litt = tmp;
                    newB.GetNodeRef().AddChild(litt.GetNodeRef());
                    RefreshNoBigListBox(root);
                }
                else
                {
                    litt = new Brother(littles[i].Substring(space + 1), littles[i].Substring(0, space), "Fall", newB.m_IniYear + 1);
                    litt.m_SelectCallback = PopulateBrotherEdit;
                    litt.m_DeleteCallback = RemoveBrotherFromTree;
                    newB.GetNodeRef().AddChild(litt.GetNodeRef());
                }
                cbTreeParent.Items.Add(litt.GetFullName());
                CurrentBrothers.Add(litt.GetFullName());
            }

            ClearAddBrother();
            //if (cbTreeParent.SelectedIndex != -1 && cbTreeParent.Text != "*All*")
            //{
            //    if (Brother.AffectsCurrentTree(newB, root.FindBrotherByName(cbTreeParent.Text), (int)(updwnNumGen.Value)))
            //    {
            //        PopulateTree(root.FindBrotherByName(cbTreeParent.Text), maxGeneration);
            //    }
            //}
            //if (cbTreeParent.Text == "*All*")
            //{
            //    PopulateTree(root, maxGeneration);
            //}
            if (cbTreeParent.Enabled == false)
            {
                cbTreeParent.Enabled = true;
                updwnNumGen.Enabled = true;
            }
            if (cbTreeParent.SelectedIndex != -1)
            {
                CreateTree();
                PostCreationShift();
            }
            RefreshNoBigListBox(root);
            cbTreeParent.Sorted = true;
        }

        private void CheckForValidBrother(object sender, EventArgs e)
        {
            btnClear.Enabled = true;
            Panel parent = (Panel)((Control)(sender)).Parent;
            foreach (Control child in parent.Controls)
            {
                Type t = child.GetType();
                if (t.Name == "Label")
                {
                    continue;
                }
                else if (t.Name == "Button")
                {
                    continue;
                }
                else if (t.Name == "TextBox")
                {
                    if (((TextBox)(child)).Multiline == true)
                    {
                        continue;
                    }
                    else
                    {
                        if (child.Text == "")
                        {
                            btnAdd.Enabled = false;
                            return;
                        }
                    }
                }
                else if (t.Name == "ComboBox")
                {
                    if (((ComboBox)(child)).SelectedIndex < 0)
                    {
                        btnAdd.Enabled = false;
                        return;
                    }
                }
                else if (t.Name == "DateTimePicker")
                {
                    if (((DateTimePicker)(child)).Text == "")
                    {
                        btnAdd.Enabled = false;
                        return;
                    }
                }
            }
            btnAdd.Enabled = true;
        }

        private void ClearAddBrother(object sender = null, EventArgs e = null)
        {
            tbFirstName.Text = "";
            tbLastName.Text = "";
            tbLittles.Text = "";
            tbBig.Text = "";
            cbIniMonth.SelectedIndex = -1;
            dtpIniYear.Value = DateTime.Today;
        }

        #endregion

        #region Selected Node Sub-Panel

        private void btnApplySelected_Click(object sender, EventArgs e)
        {
            if (tbSelectedFirst.Text != "" && tbSelectedFirst.Text != Selected.m_First)
            {
                cbTreeParent.Items.Remove(Selected.GetFullName());
                Selected.m_First = tbSelectedFirst.Text;
                cbTreeParent.Items.Add(Selected.GetFullName());
                Selected.m_Label.Text = Selected.GetFullName();
            }
            if (tbSelectedLast.Text != "" && tbSelectedLast.Text != Selected.m_Last)
            {
                cbTreeParent.Items.Remove(Selected.GetFullName());
                Selected.m_Last = tbSelectedLast.Text;
                cbTreeParent.Items.Add(Selected.GetFullName());
                Selected.m_Label.Text = Selected.GetFullName();
            }
            if (tbSelectedBig.Text == "")
            {
                if (Selected.GetNodeRef().HasParent())
                {
                    if (Selected != root)
                    {
                        root.GetNodeRef().AddChild(Selected.GetNodeRef());
                    }
                    RefreshNoBigListBox(root);
                }
            }
            else
            {
                Brother tmp = root.FindBrotherByName(tbSelectedBig.Text);
                if (tmp == null)
                {
                    int space = tbSelectedBig.Text.LastIndexOf(' ');
                    tmp = new Brother(tbSelectedBig.Text.Substring(space + 1), tbSelectedBig.Text.Substring(0, space), "Fall", 1920);
                    tmp.m_SelectCallback = PopulateBrotherEdit;
                    tmp.m_DeleteCallback = RemoveBrotherFromTree;
                    root.GetNodeRef().AddChild(tmp.GetNodeRef());
                    tmp.GetNodeRef().AddChild(Selected.GetNodeRef());
                    RefreshNoBigListBox(root);
                }
                else
                {
                    if (Selected.GetNodeRef().HasParent())
                    {
                        tmp.GetNodeRef().AddChild(Selected.GetNodeRef());
                    }
                    else
                    {
                        tmp.GetNodeRef().AddChild(Selected.GetNodeRef());
                    }
                }
            }
            if (tbSelectedLittles.Text == "")
            {
                for (int i = Selected.GetNodeRef().GetNumberOfChildren() - 1; i >= 0; i--)
                {
                    root.GetNodeRef().AddChild(Selected.GetNodeRef()[i]);
                }
                RefreshNoBigListBox(root);
            }
            else
            {
                for (int i = Selected.GetNodeRef().GetNumberOfChildren() - 1; i >= 0; i--)
                {
                    root.GetNodeRef().AddChild(Selected.GetNodeRef()[i]);
                }
                int space;
                string[] littles = tbSelectedLittles.Text.Split(new Char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                Brother litt = null;
                Brother tmp = null;
                for (int i = 0; i < littles.Count(); i++)
                {
                    space = littles[i].LastIndexOf(' ');
                    tmp = root.FindBrotherByName(littles[i]);
                    if (tmp != null)
                    {
                        litt = tmp;
                        if (litt.GetNodeRef().HasParent())
                        {
                            Selected.GetNodeRef().AddChild(litt.GetNodeRef());
                        }
                        else
                        {
                            Selected.GetNodeRef().AddChild(litt.GetNodeRef());
                            RefreshNoBigListBox(root);
                        }
                    }
                    else
                    {
                        litt = new Brother(littles[i].Substring(space + 1), littles[i].Substring(0, space), "Fall", Selected.m_IniYear + 1);
                        litt.m_SelectCallback = PopulateBrotherEdit;
                        litt.m_DeleteCallback = RemoveBrotherFromTree;
                        Selected.GetNodeRef().AddChild(litt.GetNodeRef());
                    }
                    if (!cbTreeParent.Items.Contains(litt.GetFullName()))
                    {
                        cbTreeParent.Items.Add(litt.GetFullName());
                    }
                    if (!CurrentBrothers.Contains(litt.GetFullName()))
                    {
                        CurrentBrothers.Add(litt.GetFullName());
                    }
                }
            }
            if (cbSelectedTerm.SelectedIndex != -1)
            {
                Selected.m_IniMonth = cbSelectedTerm.SelectedItem.ToString();
            }

            Selected.m_IniYear = dtpSelectedYear.Value.Year;

            Selected.isActiveBrother = chbActive.Checked;

            PopulateBrotherEdit(Selected);
            RefreshNoBigListBox(root);
            cbTreeParent.Sorted = true;
            if (cbTreeParent.SelectedIndex != -1)
            {
                CreateTree();
                PostCreationShift();
            }
        }

        private void btnCancelSelected_Click(object sender, EventArgs e)
        {
            PopulateBrotherEdit(Selected);
        }

        private void btnEditSelected_Click(object sender, EventArgs e)
        {
            tbSelectedFirst.Enabled = true;
            tbSelectedLast.Enabled = true;
            tbSelectedBig.Enabled = true;
            tbSelectedLittles.Enabled = true;
            dtpSelectedYear.Enabled = true;
            cbSelectedTerm.Enabled = true;
            btnApplySelected.Enabled = true;
            btnCancelSelected.Enabled = true;
            chbActive.Enabled = true;
        }

        #endregion

        #region Tree Control Sub-Panel

        private void btnUp_Click(object sender, EventArgs e)
        {
            Brother tmp = ((Brother)(root.FindBrotherByName(cbTreeParent.Text).GetNodeRef().Parent().GetUserData()));
            if (tmp == root)
            {
                cbTreeParent.Text = "*All*";
            }
            else
            {
                cbTreeParent.Text = tmp.GetFullName();
            }
            updwnNumGen.Value++;
        }

        #endregion

        #region Top Node Members Sub-Panel

        private void btnEdit_Click(object sender, EventArgs e)
        {
            Brother b = root.FindBrotherByName(lbNoRelation.SelectedItem.ToString());
            EditBrotherNoBig EditB = new EditBrotherNoBig(b);
            EditB.ShowDialog();
            if (EditB.DialogResult == DialogResult.OK)
            {
                RefreshNoBigListBox(root);
            }
        }

        #endregion

        #endregion

        #region Comboboxes

        private void cbTreeParent_SelectedIndexChanged(object sender, EventArgs e)
        {
            saveAsToolStripMenuItem.Enabled = true;
            treeViewToolStripMenuItem.Enabled = true;
            zoomInToolStripMenuItem.Enabled = true;
            zoomOutToolStripMenuItem.Enabled = true;

            if (cbTreeParent.SelectedIndex != -1)
            {
                if (cbTreeParent.Text != "*All*" && cbTreeParent.Text != "")
                {
                    if (root.FindBrotherByName(cbTreeParent.Text).GetNodeRef().HasParent())
                    {
                        btnUp.Visible = true;
                    }
                    else
                    {
                        btnUp.Visible = false;
                    }
                }
                else
                {
                    btnUp.Visible = false;
                }
                CreateTree();
                PostCreationShift();
            }
        }

        #endregion

        #region List Boxes

        private void lbNoRelation_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (lbNoRelation.SelectedIndex != -1)
            {
                cbTreeParent.Text = root.FindBrotherByName(lbNoRelation.SelectedItem.ToString()).GetFullName();
            }
        }

        private void lbNoRelation_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbNoRelation.SelectedIndex != -1 && PrevSelectedInd != lbNoRelation.SelectedIndex)
            {
                PrevSelectedInd = lbNoRelation.SelectedIndex;
                btnEdit.Enabled = true;
            }
            else
            {
                lbNoRelation.SelectedIndex = -1;
                PrevSelectedInd = -1;
                btnEdit.Enabled = false;
            }
        }

        #endregion

        #region Numeric Up-Downs

        private void updwnNumGen_ValueChanged(object sender, EventArgs e)
        {
            MaxGeneration = (int)(updwnNumGen.Value);
            if (cbTreeParent.SelectedIndex != -1)
            {
                CreateTree();
                PostCreationShift();
            }
        }

        private void updwnVertSpace_ValueChanged(object sender, EventArgs e)
        {
            VerticalSpacing = (int)(updwnVertSpace.Value);
            if (cbTreeParent.SelectedIndex != -1)
            {
                CreateTree();
                PostCreationShift();
            }
        }

        private void updwnHorizSpace_ValueChanged(object sender, EventArgs e)
        {
            HorizontalSpacing = (int)(updwnHorizSpace.Value);
            if (cbTreeParent.SelectedIndex != -1)
            {
                CreateTree();
                PostCreationShift();
            }
        }

        private void updwnSubTree_ValueChanged(object sender, EventArgs e)
        {
            if (cbTreeParent.SelectedIndex != -1)
            {
                CreateTree();
                PostCreationShift();
            }
        }

        #endregion

        #region Panels

        private void pnlTree_Click(object sender, EventArgs e)
        {
            if (Selected != null)
            {
                HideSelectedEdit();
                Selected.m_Label.BackColor = System.Drawing.Color.White;
                Selected = null;
            }
        }

        private void pnlTree_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (cbTreeParent.SelectedIndex != -1)
            {
                treeViewToolStripMenuItem.Checked = !treeViewToolStripMenuItem.Checked;
            }
            treeViewToolStripMenuItem_Click(treeControlToolStripMenuItem, EventArgs.Empty);
        }

        #endregion

        #region Split Containers

        private void splitTreeInfo_Panel1_Click(object sender, EventArgs e)
        {
            pnlTree_Click((object)pnlTree, e);
        }

        private void splitTreeInfo_Panel1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            pnlTree_MouseDoubleClick((object)pnlTree, e);
        }

        private void splitTreeInfo_Panel1_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                foreach (string bName in cbTreeParent.Items)
                {
                    if (bName == "*All*")
                    {
                        continue;
                    }
                    Brother b = root.FindBrotherByName(bName);
                    if (((Brother)(b.GetNodeRef().Parent().GetUserData())).m_Label.Parent != null &&
                        ((Brother)(b.GetNodeRef().Parent().GetUserData())).m_Label.Visible &&
                        b.m_Label.Parent != null &&
                        b.m_Label.Capture == false &&
                        b.m_Label.Visible)
                    {
                        Pen blackP = new Pen(Color.Black, 1);
                        Point[] pt = {
                        new Point(((Brother)(b.GetNodeRef().Parent().GetUserData())).m_Label.Location.X + 
                                  ((Brother)(b.GetNodeRef().Parent().GetUserData())).m_Label.Width / 2,
                                  ((Brother)(b.GetNodeRef().Parent().GetUserData())).m_Label.Location.Y +
                                  ((Brother)(b.GetNodeRef().Parent().GetUserData())).m_Label.Height),
                        new Point(b.m_Label.Location.X + b.m_Label.Width / 2, b.m_Label.Location.Y)};
                        e.Graphics.DrawCurve(blackP, pt, 0.00F);
                    }
                }
            }
            catch (System.NullReferenceException exc)
            {
                MessageBox.Show(exc.Message + "\n" + exc.Source);
            }
        }

        #endregion

        #region ToolStripMenuItems

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "All Image Files|*.png;*.bmp;*.gif;*.jpg;*.jpeg;*.jpe;*.jfif;*.tif;*.tiff|PNG|*.png|Bitmap|*.bmp|GIF|*.gif|JPEG|*.jpg;*.jpeg;*.jpe;*.jfif|TIFF|*.tif;*.tiff|All Files|*.*";
            sfd.AddExtension = true;
            DialogResult ret = sfd.ShowDialog();
            if (ret == DialogResult.OK)
            {
                ImageFormat format = ImageFormat.Png;
                Image panelImage = CaptureScreen();
                string ext = System.IO.Path.GetExtension(sfd.FileName);
                switch (ext)
                {
                    case ".png":
                        format = ImageFormat.Png;
                        break;
                    case ".bmp":
                        format = ImageFormat.Bmp;
                        break;
                    case ".gif":
                        format = ImageFormat.Gif;
                        break;
                    case ".jpg":
                    case ".jpeg":
                    case ".jpe":
                    case ".jfif":
                        format = ImageFormat.Jpeg;
                        break;
                    case ".tif":
                    case ".tiff":
                        format = ImageFormat.Tiff;
                        break;
                    default:
                        format = ImageFormat.Png;
                        break;
                }

                panelImage.Save(sfd.FileName, format);
            }
        }

        private void treeViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (cbTreeParent.SelectedIndex != -1)
            {
                ToolStripMenuItem tmp = (ToolStripMenuItem)sender;
                if (tmp.Checked)
                {
                    splitContainer1.Panel2Collapsed = true;
                    splitTreeInfo.Panel2Collapsed = true;
                    splitTreeAdd.Panel2Collapsed = true;
                    membersWithoutBigsToolStripMenuItem.Checked = false;
                    addMemberToolStripMenuItem.Checked = false;
                    treeControlToolStripMenuItem.Checked = false;
                }
                else
                {
                    splitContainer1.Panel2Collapsed = false;
                    if (Selected != null)
                    {
                        splitTreeInfo.Panel2Collapsed = false;
                    }
                    splitTreeAdd.Panel2Collapsed = false;
                    splitEditView.Panel1Collapsed = false;
                    splitEditView.Panel2Collapsed = false;
                    membersWithoutBigsToolStripMenuItem.Checked = true;
                    addMemberToolStripMenuItem.Checked = true;
                    treeControlToolStripMenuItem.Checked = true;
                }
            }
        }

        private void zoomInToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Control c in pnlTree.Controls)
            {
                Label l = (Label)(c);
                l.Font = new Font(l.Font.FontFamily, l.Font.Size * ZOOM_FACTOR);
                SizeF sf = new SizeF(ZOOM_FACTOR, ZOOM_FACTOR);
                c.Scale(sf);
            }
            ZoomLevel++;
        }

        private void zoomOutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Control c in pnlTree.Controls)
            {
                Label l = (Label)(c);
                l.Font = new Font(l.Font.FontFamily, l.Font.Size / ZOOM_FACTOR);
                SizeF sf = new SizeF(1 / ZOOM_FACTOR, 1 / ZOOM_FACTOR);
                c.Scale(sf);
            }
            ZoomLevel--;
        }

        private void displayRootOfAllTreeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem tsmi = (ToolStripMenuItem)sender;

            if (tsmi.Checked)
            {
                tsmi.Checked = false;
            }
            else
            {
                tsmi.Checked = true;
            }
            DisplayApex = tsmi.Checked;
            if (cbTreeParent.SelectedIndex != -1)
            {
                CreateTree();
                PostCreationShift();
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox ab = new AboutBox();
            ab.ShowDialog();
        }

        private void supportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://yitbosoft.com/?contact");
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "XML Document|*.xml|All Files|*.*";
            sfd.AddExtension = true;
            sfd.FileName = System.IO.Path.GetFileName(OpenedXmlFilePath);
            sfd.OverwritePrompt = true;
            sfd.DefaultExt = ".xml";
            sfd.Title = "Save the exported xml as...";
            DialogResult res = sfd.ShowDialog();
            if (res == DialogResult.OK)
            {
                string parentNodeName = Interaction.InputBox("Please enter a name for the parent XML node...\n" +
                                                             "Example: \"DeltaSigmaPhi-AlphaEta\"",
                                                             "Parent Node Name",
                                                             XmlParentNodeName != "" ? XmlParentNodeName : "MyTree");
                if (parentNodeName != "")
                {
                    ExportToXml(sfd.FileName, parentNodeName);
                }
            }
        }

        private void addMemberToolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeViewToolStripMenuItem.Checked = false;
            splitTreeAdd.Panel2Collapsed = !splitTreeAdd.Panel2Collapsed;
            if (!addMemberToolStripMenuItem.Checked &&
                !membersWithoutBigsToolStripMenuItem.Checked &&
                !treeControlToolStripMenuItem.Checked)
            {
                treeViewToolStripMenuItem.Checked = true;
            }
        }

        private void membersWithoutBigsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeViewToolStripMenuItem.Checked = false;
            if (treeControlToolStripMenuItem.Checked)
            {
                splitEditView.Panel1Collapsed = !splitEditView.Panel1Collapsed;
            }
            else
            {
                splitContainer1.Panel2Collapsed = !splitContainer1.Panel2Collapsed;
                splitEditView.Panel1Collapsed = !membersWithoutBigsToolStripMenuItem.Checked;
                splitEditView.Panel2Collapsed = !treeControlToolStripMenuItem.Checked;
            }
            if (!addMemberToolStripMenuItem.Checked &&
                !membersWithoutBigsToolStripMenuItem.Checked &&
                !treeControlToolStripMenuItem.Checked)
            {
                treeViewToolStripMenuItem.Checked = true;
            }
        }

        private void treeControlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeViewToolStripMenuItem.Checked = false;
            if (membersWithoutBigsToolStripMenuItem.Checked)
            {
                splitEditView.Panel2Collapsed = !splitEditView.Panel2Collapsed;
            }
            else
            {
                splitContainer1.Panel2Collapsed = !splitContainer1.Panel2Collapsed;
                splitEditView.Panel1Collapsed = !membersWithoutBigsToolStripMenuItem.Checked;
                splitEditView.Panel2Collapsed = !treeControlToolStripMenuItem.Checked;
            }
            if (!addMemberToolStripMenuItem.Checked &&
                !membersWithoutBigsToolStripMenuItem.Checked &&
                !treeControlToolStripMenuItem.Checked)
            {
                treeViewToolStripMenuItem.Checked = true;
            }
        }

        private void fixedLabelWidthsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FixedWidth = fixedLabelWidthsToolStripMenuItem.Checked;
            CreateTree();
            PostCreationShift();
        }

        private void saveXmlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportToXml(OpenedXmlFilePath, XmlParentNodeName);
        }

        #endregion

        #endregion
    }
}
