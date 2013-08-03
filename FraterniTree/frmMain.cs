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
        private int MaxGeneration     = 1;

        #endregion

        #region Flags
        private bool bIsMale        = true;
        private bool bIsXml         = false;
        private bool DisplayApex    = false;
        private bool FixedWidth     = false;
        private bool WriteBackReady = false;
        private bool IsSelectedEdit = false;

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
                        if (tmpBig.Parent() == root || tmpBig.Parent() == null && tmpBig != root)
                        {
                            RefreshNoBigListBox(root);
                        }
                    }
                    else
                    {
                        tmpBig = new Brother(bigName.Substring(space + 1), bigName.Substring(0, space), "Fall", 1920);
                        tmpBig.m_Label.ContextMenuStrip = cmNodeActions;
                        root.AddChild(tmpBig);
                        RefreshNoBigListBox(root);
                    }

                    Brother newB = null;
                    tmp = root.FindBrotherByName(first + " " + last);
                    if (tmp != null)
                    {
                        newB = tmp;
                        if (newB.HasParent() && newB.Parent() == tmpBig)
                        {
                            // Do nothing
                        }
                        else
                        {
                            tmpBig.AddChild(newB);
                        }
                        newB.IniMonth = month;
                        newB.IniYear = year;
                    }
                    else
                    {
                        newB = new Brother(last, first, month, year);
                        newB.m_Label.ContextMenuStrip = cmNodeActions;
                        tmpBig.AddChild(newB);
                    }

                    RefreshNoBigListBox(root);
                }
                rdr.Close();
                while (!rdr.IsClosed) ;
                DbConnect.Close();
            }
            if (root.HasChild())
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
            }
            for (int i = b.GetNumberOfChildren() - 1; i >= 0; i--)
            {
                if (b == root)
                {
                    if (!lbNoRelation.Items.Contains(((Brother)b[i])))
                    {
                        lbNoRelation.Items.Add(((Brother)b[i]));
                    }
                }
                if (!cbTreeParent.Items.Contains(((Brother)b[i])))
                {
                    cbTreeParent.Items.Add(((Brother)b[i]));
                }
                if (!CurrentBrothers.Contains(((Brother)b[i]).ToString()))
                {
                    CurrentBrothers.Add(((Brother)b[i]).ToString());
                }
                RefreshNoBigListBox((Brother)(b[i]));
            }            
        }

        private void RemoveBrotherFromTree(Brother b)
        {
            string name = b.ToString();
            lbNoRelation.Items.Remove(b);
            cbTreeParent.Items.Remove(b);
            b = null;
            CurrentBrothers.Remove(name);
            RefreshNoBigListBox(root);
            DisplayTree(true);
        }

        #region Graphical Tree Display

        private void DisplayTree(bool isScrollMaintained = false)
        {
            if (cbTreeParent.SelectedIndex != -1 && cbTreeParent.Text != "")
            {

                float horizPercentage = ((float)splitTreeInfo.Panel1.HorizontalScroll.Value) / ((float)splitTreeInfo.Panel1.HorizontalScroll.Maximum);
                float vertPercentage = ((float)splitTreeInfo.Panel1.VerticalScroll.Value) / ((float)splitTreeInfo.Panel1.VerticalScroll.Maximum);
                CreateTree();
                PostCreationShift();
                if (isScrollMaintained)
                {
                    splitTreeInfo.Panel1.HorizontalScroll.Value = (int)(horizPercentage * (float)splitTreeInfo.Panel1.HorizontalScroll.Maximum);
                    splitTreeInfo.Panel1.VerticalScroll.Value = (int)(vertPercentage * (float)splitTreeInfo.Panel1.VerticalScroll.Maximum);
                    splitTreeInfo.Panel1.PerformLayout();
                }
            }
            else
            {
                btnUp.Visible = false;
            }
        }

        private void AddLabelsToPanel(Brother parent, int g)
        {
            if (g >= 0)
            {
                int count = parent.GetNumberOfChildren();
                parent.m_Prelim = 0;
                parent.m_Modifier = 0;
                parent.m_Label.AutoSize = !FixedWidth;
                if (!parent.IsIgnored())
                {
                    pnlTree.Controls.Add(parent.m_Label);
                }
                if (parent.m_Label.AutoSize)
                {
                    MaximumWidth = Math.Max(MaximumWidth, parent.m_Label.Width);
                    parent.SetWidth(parent.m_Label.Width);
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
                    if (!parent[i].IsIgnored())
                    {
                        AddLabelsToPanel((Brother)parent[i], g - 1);
                    }
                }
            }
        }

        private void BoundsCheckShift()
        {
            int leastPosX = 0;
            int leastPosY = 0;

            foreach (Control c in pnlTree.Controls)
            {
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
                }
            }
        }

        private void CreateTree()
        {
            HideSelectedEdit();
            UnzoomControls();

            pnlTree.Visible = false;

            root.RecursiveClearIgnoreNode();

            if (Selected != null)
            {
                Selected.m_Label.Font = new Font(Selected.m_Label.Font, Selected.m_Label.Font.Style & ~FontStyle.Bold);
            }

            Selected = null;

            pnlTree.Controls.Clear();

            if (cbTreeParent.Text != "*All*" && cbTreeParent.Text != "*Active Only*" && cbTreeParent.Text != "")
            {
                TreeRoot = (Brother)cbTreeParent.SelectedItem;
            }
            else if (cbTreeParent.Text == "")
            {
                return;
            }
            else
            {
                TreeRoot = root;
                if (cbTreeParent.Text == "*Active Only*")
                {
                    TreeRoot.RecursiveSetIgnoreNode();
                }
            }

            if (TreeRoot != null)
            {
                Size s = TreeRoot.m_Label.Size;
                Point p = new Point((splitTreeInfo.Panel1.Width / 2) - (s.Width / 2), 2);
                TreeRoot.SetXCoord(p.X);
                TreeRoot.SetYCoord(p.Y);
                int gens = (TreeRoot.GetNumGenerations());
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
                WalkerAlgorithmTree.PositionTree(TreeRoot);
            }
            else
            {
                pnlTree.Controls.Clear();
            }
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
                }
            }

            if (!DisplayApex)
            {
                if (root.m_Label.Parent != null)
                {
                    root.m_Label.Parent = null;
                    leastPosY = root.m_Label.Height + (int)(updwnVertSpace.Value);
                    foreach (Control c in pnlTree.Controls)
                    {
                        c.Location = new Point(c.Location.X, c.Location.Y - leastPosY);
                    }
                }
            }
            pnlTree.Visible = true;
        }

        private void SetLabelWidths(Brother parent, int g)
        {
            if (g >= 0)
            {
                int count = parent.GetNumberOfChildren();

                parent.m_Label.Width = MaximumWidth;
                parent.SetWidth(parent.m_Label.Width);

                for (int i = 0; i < count; i++)
                {
                    SetLabelWidths((Brother)parent[i], g - 1);
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
            IsSelectedEdit = false;

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

            if (Selected != null && Selected != b)
            {
                int oldWidth = Selected.m_Label.Width;
                Selected.m_Label.Font = new Font(Selected.m_Label.Font, Selected.m_Label.Font.Style & ~FontStyle.Bold);
                Selected.m_Label.Refresh();
                Selected.m_Label.Location = new Point(Selected.m_Label.Location.X + (oldWidth- Selected.m_Label.Width) / 2, Selected.m_Label.Location.Y);
            }
            Selected = b;

            tbSelectedFirst.Text = b.First;
            tbSelectedLast.Text = b.Last;
            tbSelectedBig.Text = b.HasParent() ? ((Brother)(b.Parent())).ToString() : "";
            tbSelectedLittles.Text = "";
            for (int i = 0; i < b.GetNumberOfChildren(); i++)
            {
                Brother l = (Brother)(b[i]);
                tbSelectedLittles.Text += (i == 0 ? "" : Environment.NewLine ) + l.ToString();
            }

            dtpSelectedYear.Value = new DateTime(b.IniYear, 1, 1);
            if (b.IniMonth != "")
            {
                cbSelectedTerm.SelectedItem = b.IniMonth;
            }

            chbActive.Checked = b.isActiveBrother;

            btnEditSelected.Enabled = true;
        }

        private void HideSelectedEdit()
        {
            splitTreeInfo.Panel2Collapsed = true;
        }

        private bool IsSelectedDataEdited()
        {
            if (tbSelectedFirst.Text != Selected.First)
            {
                return true;
            }

            if (tbSelectedLast.Text != Selected.Last)
            {
                return true;
            }

            if (tbSelectedBig.Text != ((Brother)(Selected.Parent())).ToString())
            {
                return true;
            }

            string[] littles = tbSelectedLittles.Text.Split(new Char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (littles.Length != Selected.GetNumberOfChildren())
            {
                return true;
            }
            else
            {
                for (int i = Selected.GetNumberOfChildren() - 1; i >= 0; i--)
                {
                    if (!littles.Contains(((Brother)(Selected[i])).ToString()))
                    {
                        return true;
                    }
                }
            }

            if (dtpSelectedYear.Value.Year != Selected.IniYear)
            {
                return true;
            }

            if (cbSelectedTerm.Text != Selected.IniMonth)
            {
                return true;
            }

            if (chbActive.Checked != Selected.isActiveBrother)
            {
                return true;
            }
            

            return false;
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
                    if (currentParent.HasChild())
                    {
                        WriteBackToDB((Brother)(currentParent.FirstChild()));
                    }

                    if (currentParent.HasRightSibling())
                    {
                        WriteBackToDB((Brother)(currentParent.RightSibling()));
                    }

                    if (currentParent != root)
                    {
                        DbConnect.Open();
                        cmd = new MySqlCommand(INSERT_INTO_STM, DbConnect);
                        cmd.Prepare();
                        cmd.Parameters.AddWithValue("@Last", currentParent.Last);
                        cmd.Parameters.AddWithValue("@First", currentParent.First);
                        cmd.Parameters.AddWithValue("@IniMonth", currentParent.IniMonth);
                        cmd.Parameters.AddWithValue("@IniYear", currentParent.IniYear);

                        if (currentParent.HasParent())
                        {
                            cmd.Parameters.AddWithValue("@Big", ((Brother)(currentParent.Parent())).ToString());
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@Big", "");
                        }
                        if (currentParent.HasRightSibling())
                        {
                            cmd.Parameters.AddWithValue("@NextSibling", ((Brother)(currentParent.RightSibling())).ToString());
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@NextSibling", "");
                        }
                        if (currentParent.HasChild())
                        {
                            cmd.Parameters.AddWithValue("@FirstLittle", ((Brother)(currentParent.FirstChild())).ToString());
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
            xmlData += "Last=\"" + B.Last + "\" ";
            xmlData += "First=\"" + B.First + "\" ";
            xmlData += "IniTerm=\"" + B.IniMonth + "\" ";
            xmlData += "IniYear=\"" + B.IniYear + "\" ";
            xmlData += "Active=\"" + B.isActiveBrother.ToString() + "\" ";

            xmlData += ">";

            for (int i = B.GetNumberOfChildren() - 1; i >= 0; i--)
            {
                xmlData += ConvertTreeToXml((Brother)B[i]);
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

            if (currentParent.Attributes["Active"] != null)
            {
                string val = currentParent.Attributes["Active"].Value;
                switch (val.ToUpper())
                {
                    case "YES":
                    case "Y":
                    case "TRUE":
                    case "T":
                    case "1":
                        big.isActiveBrother = true;
                        break;
                    default:
                        big.isActiveBrother = false;
                        break;
                }
            }
            else
            {
                big.isActiveBrother = false;
            }

            big.m_Label.ContextMenuStrip = cmNodeActions;
            ttTree.SetToolTip(big.m_Label, "Left Click to Select, Right Click to Delete, Middle Click to Hide Children.");

            foreach (XmlNode child in currentParent.ChildNodes)
            {
                big.AddChild(ConvertXmlToTree(child));
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
                xmlData += "Active=\"" + false.ToString() + "\" ";
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
                    if (currentParent.Attributes["Active"] != null)
                    {
                        string val = currentParent.Attributes["Active"].Value;
                        switch (val.ToUpper())
                        {
                            case "yes":
                            case "y":
                            case "true":
                            case "t":
                            case "1":
                                root.isActiveBrother = true;
                                break;
                            default:
                                root.isActiveBrother = false;
                                break;
                        }
                    }
                    else
                    {
                        root.isActiveBrother = false;
                    }
                    root.m_Label.ContextMenuStrip = cmNodeActions;
                }
                foreach (XmlNode child in currentParent.ChildNodes)
                {
                    root.AddChild(ConvertXmlToTree(child));
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
            Brother.m_SelectCallback = PopulateBrotherEdit;
            Brother.m_ShiftCallback = BoundsCheckShift;

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
            root.m_Label.ContextMenuStrip = cmNodeActions;

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
                bigName = root.ToString();
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
                tmpBig.m_Label.ContextMenuStrip = cmNodeActions;
                root.AddChild(tmpBig);
            }

            Brother newB = null;
            tmp = root.FindBrotherByName(first + " " + last);
            if (tmp != null)
            {
                newB = tmp;
                tmpBig.AddChild(newB);
            }
            else
            {
                newB = new Brother(last, first, month, year);
                newB.m_Label.ContextMenuStrip = cmNodeActions;
                tmpBig.AddChild(newB);
            }

            Brother litt = null;
            for (int i = 0; i < littles.Count(); i++)
            {
                space = littles[i].LastIndexOf(' ');
                tmp = root.FindBrotherByName(littles[i]);
                if (tmp != null)
                {
                    litt = tmp;
                    newB.AddChild(litt);
                }
                else
                {
                    litt = new Brother(littles[i].Substring(space + 1), littles[i].Substring(0, space), "Fall", newB.IniYear + 1);
                    litt.m_Label.ContextMenuStrip = cmNodeActions;
                    litt.m_Label.ContextMenuStrip = cmNodeActions;
                    newB.AddChild(litt);
                }
            }

            ClearAddBrother();

            if (cbTreeParent.Enabled == false)
            {
                cbTreeParent.Enabled = true;
                updwnNumGen.Enabled = true;
            }
            RefreshNoBigListBox(root);
            DisplayTree(true);
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
            if (cbSelectedTerm.SelectedIndex != -1)
            {
                Selected.IniMonth = cbSelectedTerm.SelectedItem.ToString();
            }

            Selected.IniYear = dtpSelectedYear.Value.Year;

            Selected.isActiveBrother = chbActive.Checked;

            if (tbSelectedFirst.Text != "" && tbSelectedFirst.Text != Selected.First)
            {
                cbTreeParent.Items.Remove(Selected);
                Selected.First = tbSelectedFirst.Text;
                cbTreeParent.Items.Add(Selected);
                cbTreeParent.Sorted = true;
            }
            if (tbSelectedLast.Text != "" && tbSelectedLast.Text != Selected.Last)
            {
                cbTreeParent.Items.Remove(Selected);
                Selected.Last = tbSelectedLast.Text;
                cbTreeParent.Items.Add(Selected);
                cbTreeParent.Sorted = true;
            }
            if (tbSelectedBig.Text == "")
            {
                if (Selected.HasParent())
                {
                    if (Selected != root)
                    {
                        root.AddChild(Selected);
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
                    tmp.m_Label.ContextMenuStrip = cmNodeActions;
                    root.AddChild(tmp);
                    tmp.AddChild(Selected);
                    RefreshNoBigListBox(root);
                }
                else
                {
                    if (Selected.HasParent())
                    {
                        tmp.AddChild(Selected);
                    }
                    else
                    {
                        tmp.AddChild(Selected);
                    }
                }
            }
            if (tbSelectedLittles.Text == "")
            {
                for (int i = Selected.GetNumberOfChildren() - 1; i >= 0; i--)
                {
                    root.AddChild((Brother)Selected[i]);
                }
                RefreshNoBigListBox(root);
            }
            else
            {
                for (int i = Selected.GetNumberOfChildren() - 1; i >= 0; i--)
                {
                    root.AddChild((Brother)Selected[i]);
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
                        if (litt.HasParent())
                        {
                            Selected.AddChild(litt);
                        }
                        else
                        {
                            Selected.AddChild(litt);
                            RefreshNoBigListBox(root);
                        }
                    }
                    else
                    {
                        litt = new Brother(littles[i].Substring(space + 1), littles[i].Substring(0, space), "Fall", Selected.IniYear + 1);
                        litt.m_Label.ContextMenuStrip = cmNodeActions;
                        Selected.AddChild(litt);
                    }
                }
            }

            if (TreeRoot == Selected && cbTreeParent.Text == "")
            {
                cbTreeParent.SelectedItem = TreeRoot;
                PopulateBrotherEdit(TreeRoot);
            }
            else
            {
                PopulateBrotherEdit(Selected);
            }

            RefreshNoBigListBox(root);
            cbTreeParent.Sorted = true;
            DisplayTree(true);
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
            btnApplySelected.Enabled = false;
            btnCancelSelected.Enabled = true;
            chbActive.Enabled = true;
        }

        #endregion

        #region Tree Control Sub-Panel

        private void btnUp_Click(object sender, EventArgs e)
        {
            Brother tmp = (Brother)((Brother)cbTreeParent.SelectedItem).Parent();
            if (tmp == root)
            {
                cbTreeParent.Text = "*All*";
            }
            else
            {
                cbTreeParent.Text = tmp.ToString();
            }
            updwnNumGen.Value++;
        }

        #endregion

        #region Top Node Members Sub-Panel

        private void btnEdit_Click(object sender, EventArgs e)
        {
            //Brother b = root.FindBrotherByName(lbNoRelation.SelectedItem.ToString());
            Brother b = (Brother)lbNoRelation.SelectedItem;
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
                if (cbTreeParent.Text != "*All*" && cbTreeParent.Text != "*Active Only*" && cbTreeParent.Text != "")
                {
                    if (((Brother)cbTreeParent.SelectedItem).HasParent())
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
                //cbTreeParent.Text = root.FindBrotherByName(lbNoRelationToString().ToString()).ToString();
                cbTreeParent.Text = ((Brother)lbNoRelation.SelectedItem).ToString();
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
                float horizPercentage = ((float)splitTreeInfo.Panel1.HorizontalScroll.Value) / ((float)splitTreeInfo.Panel1.HorizontalScroll.Maximum);
                float vertPercentage = ((float)splitTreeInfo.Panel1.VerticalScroll.Value) / ((float)splitTreeInfo.Panel1.VerticalScroll.Maximum);
                CreateTree();
                PostCreationShift();
                splitTreeInfo.Panel1.HorizontalScroll.Value = (int)(horizPercentage * (float)splitTreeInfo.Panel1.HorizontalScroll.Maximum);
                splitTreeInfo.Panel1.VerticalScroll.Value = (int)(vertPercentage * (float)splitTreeInfo.Panel1.VerticalScroll.Maximum);
                splitTreeInfo.Panel1.PerformLayout();
            }
        }

        private void updwnVertSpace_ValueChanged(object sender, EventArgs e)
        {
            DisplayTree(true);
        }

        private void updwnHorizSpace_ValueChanged(object sender, EventArgs e)
        {
            DisplayTree(true);
        }

        private void updwnSubTree_ValueChanged(object sender, EventArgs e)
        {
            DisplayTree(true);
        }

        #endregion

        #region Panels

        private void pnlTree_Click(object sender, EventArgs e)
        {
            if (Selected != null)
            {
                HideSelectedEdit();
                int oldWidth = Selected.m_Label.Width;
                Selected.m_Label.Font = new Font(Selected.m_Label.Font, Selected.m_Label.Font.Style & ~FontStyle.Bold);
                Selected.m_Label.Refresh();
                Selected.m_Label.Location = new Point(Selected.m_Label.Location.X + (oldWidth - Selected.m_Label.Width) / 2, Selected.m_Label.Location.Y);
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
            foreach (Control c in pnlTree.Controls)
            {
                if (c is Label)
                {
                    Label l = (Label)c;
                    Brother b = root.FindBrotherByName(l.Text);
                    if (b.HasParent())
                    {
                        if (((Brother)(b.Parent())).m_Label.Parent != null &&
                        ((Brother)(b.Parent())).m_Label.Visible &&
                        b.m_Label.Parent != null &&
                        b.m_Label.Capture == false &&
                        b.m_Label.Visible)
                        {
                            Pen blackP = new Pen(Color.Black, 1);
                            Point[] pt = 
                        {
                            new Point(((Brother)(b.Parent())).m_Label.Location.X + 
                                        ((Brother)(b.Parent())).m_Label.Width / 2,
                                        ((Brother)(b.Parent())).m_Label.Location.Y +
                                        ((Brother)(b.Parent())).m_Label.Height),
                            new Point(b.m_Label.Location.X + b.m_Label.Width / 2, b.m_Label.Location.Y)
                        };
                            e.Graphics.DrawCurve(blackP, pt, 0.00F);
                        }
                    }
                }
            }
        }

        #region Selected Edit Panel

        private void SelectedEdit_ValueChanged(object sender, EventArgs e)
        {
            IsSelectedEdit = IsSelectedDataEdited();
            if (IsSelectedEdit)
            {
                btnApplySelected.Enabled = true;
            }
            else
            {
                btnApplySelected.Enabled = false;
            }
        }

        #endregion

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
            DisplayTree();
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
            DisplayTree();
        }

        private void saveXmlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportToXml(OpenedXmlFilePath, XmlParentNodeName);
        }

        #endregion

        #region Node Context Menu

        private void removeNodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Label lbl = new Label();
            ToolStripItem tsi = sender as ToolStripItem;

            if (tsi != null)
            {
                ContextMenuStrip cm = tsi.Owner as ContextMenuStrip;
                if (cm != null)
                {
                    lbl = cm.SourceControl as Label;
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }

            Brother clicked = (Brother)lbl.Tag;

            DialogResult res = MessageBox.Show("Are you sure you want to delete this node: " + clicked.ToString() + "?\n\n" +
                                               "All its children nodes will be re-assigned to the parent.",
                                               "Node Removal Confirmation",
                                               MessageBoxButtons.YesNo,
                                               MessageBoxIcon.Warning);
            if (res == DialogResult.Yes)
            {
                if (Selected == clicked)
                {
                    Selected = null;
                }
                clicked.RemoveNode();
                clicked.m_Label.Dispose();
                clicked.m_Label = null;
                RemoveBrotherFromTree(clicked);
            }
            else
            {
                // Do Nothing
            }
        }

        private void toggleHideDescendantsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Label lbl = new Label();
            ToolStripItem tsi = sender as ToolStripItem;

            if (tsi != null)
            {
                ContextMenuStrip cm = tsi.Owner as ContextMenuStrip;
                if (cm != null)
                {
                    lbl = cm.SourceControl as Label;
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }

            Brother clicked = (Brother)lbl.Tag;
            if (clicked.FirstChild() == null)
            {
                return;
            }
            Brother firstChild = ((Brother)(clicked.FirstChild()));
            if (firstChild.m_Label.Parent != null)
            {
                if (firstChild.m_Label.Visible)
                {
                    lbl.Font = new Font(lbl.Font, lbl.Font.Style | FontStyle.Italic);
                    clicked.SetDescendantsHidden(true);
                }
                else
                {
                    lbl.Font = new Font(lbl.Font, lbl.Font.Style & ~FontStyle.Italic);
                    clicked.SetDescendantsHidden(false);
                }
                for (int i = clicked.GetNumberOfChildren() - 1; i >= 0; i--)
                {
                    clicked.RecursiveLabelVisibleToggle((Brother)(clicked[i]));
                }
            }
        }

        #endregion

        #endregion

    }
}
