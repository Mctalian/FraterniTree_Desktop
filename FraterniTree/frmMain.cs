using Microsoft.VisualBasic;
using MySql.Data.MySqlClient;
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

        static System.Timers.Timer AutoSave = new System.Timers.Timer();

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
        private FieldEdit SelectedEdits = FieldEdit.NONE;

        #endregion

        [Flags]
        private enum FieldEdit
        {
            NONE       = 0x0,
            FIRST_NAME = 0x1,
            LAST_NAME  = 0x2,
            BIG        = 0x4,
            LITTLES    = 0x8,
            INI_MONTH  = 0x10,
            INI_YEAR   = 0x11,
            ACTIVE     = 0x12,
            ALL_MASK   = 0xFF
        };

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
                        tmpBig.Label.ContextMenuStrip = cmNodeActions;
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
                        newB.Label.ContextMenuStrip = cmNodeActions;
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

            for (var i = b.GetNumberOfChildren() - 1; i >= 0; i--)
            {
                if (b == root)
                {
                    if (!lbNoRelation.Items.Contains( (Brother)b[i]) )
                    {
                        lbNoRelation.Items.Add( (Brother)b[i] );
                    }
                }

                if (!cbTreeParent.Items.Contains( (Brother)b[i]) )
                {
                    cbTreeParent.Items.Add( (Brother)b[i] );
                }

                if (!CurrentBrothers.Contains( ((Brother)b[i]).ToString()) )
                {
                    CurrentBrothers.Add( ((Brother)b[i]).ToString() );
                }

                RefreshNoBigListBox( (Brother)b[i] );
            }            
        }

        private void RemoveBrotherFromTree(Brother b)
        {
            var name = b.ToString();
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
                var horizPercentage = splitTreeInfo.Panel1.HorizontalScroll.Value / (float)splitTreeInfo.Panel1.HorizontalScroll.Maximum;
                var vertPercentage = splitTreeInfo.Panel1.VerticalScroll.Value / (float)splitTreeInfo.Panel1.VerticalScroll.Maximum;
                
                CreateTree();
                PostCreationShift();
                
                if (isScrollMaintained)
                {
                    splitTreeInfo.Panel1.HorizontalScroll.Value = (int)(horizPercentage * (float)splitTreeInfo.Panel1.HorizontalScroll.Maximum); //TODO
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
                var count = parent.GetNumberOfChildren();
                parent.Prelim = 0;
                parent.Modifier = 0;
                parent.Label.AutoSize = !FixedWidth;
                
                if (!parent.IsIgnored())
                {
                    pnlTree.Controls.Add(parent.Label);
                }
                
                if (parent.Label.AutoSize)
                {
                    MaximumWidth = Math.Max(MaximumWidth, parent.Label.Width);
                    parent.SetWidth(parent.Label.Width);
                }
                else
                {
                    parent.Label.AutoSize = true;
                    parent.Label.Parent.Refresh();
                    MaximumWidth = Math.Max(MaximumWidth, parent.Label.Width);
                    parent.Label.AutoSize = false;
                }

                for (var i = 0; i < count; i++)
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
            var leastPosX = 0;
            var leastPosY = 0;

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
            displayRootOfAllTreeToolStripMenuItem.Enabled = false;
            generationDownToolStripMenuItem.Enabled = false;
            generationUpToolStripMenuItem.Enabled = false;

            HideSelectedEdit();
            UnzoomControls();

            pnlTree.Visible = false;

            root.RecursiveClearIgnoreNode();

            if (Selected != null)
            {
                Selected.Label.Font = new Font(Selected.Label.Font, Selected.Label.Font.Style & ~FontStyle.Bold);
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
                displayRootOfAllTreeToolStripMenuItem.Enabled = true;
                TreeRoot = root;
                if (cbTreeParent.Text == "*Active Only*")
                {
                    displayRootOfAllTreeToolStripMenuItem.Enabled = false;
                    TreeRoot.RecursiveSetIgnoreNode();
                }
            }

            if (TreeRoot != null)
            {
                generationDownToolStripMenuItem.Enabled = true;
                generationUpToolStripMenuItem.Enabled = true;

                var s = TreeRoot.Label.Size;
                var p = new Point((splitTreeInfo.Panel1.Width / 2) - (s.Width / 2), 2);
                TreeRoot.SetXCoord(p.X);
                TreeRoot.SetYCoord(p.Y);
                var gens = (TreeRoot.GetNumGenerations());
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
            var leastPosX = 0;
            var leastPosY = 0;

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
                if (root.Label.Parent != null)
                {
                    root.Label.Parent = null;
                    leastPosY = root.Label.Height + (int)updwnVertSpace.Value;
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
                var count = parent.GetNumberOfChildren();

                parent.Label.Width = MaximumWidth;
                parent.SetWidth(parent.Label.Width);

                for (var i = 0; i < count; i++)
                {
                    SetLabelWidths((Brother)parent[i], g - 1);
                }
            }
        }

        private void UnzoomControls()
        {
            float zoomFactor = 1;
            
            if (ZoomLevel < 0)
            {
                zoomFactor = (float)Math.Pow(ZOOM_FACTOR, (-ZoomLevel));
            }
            else if (ZoomLevel > 0)
            {
                zoomFactor = (float)Math.Pow(1 / ZOOM_FACTOR, ZoomLevel);
            }
            else
            {
                return;
            }

            foreach (Control c in pnlTree.Controls)
            {
                var l = (Label)c;
                l.Font = new Font(l.Font.FontFamily, l.Font.Size * zoomFactor);
                var sf = new SizeF(zoomFactor, zoomFactor);
                c.Scale(sf);
            }

            ZoomLevel = 0;
        }

        #endregion

        #region Selected Member Information Edit

        private void PopulateBrotherEdit(Brother b)
        {
            SelectedEdits = FieldEdit.NONE;

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
                int oldWidth = Selected.Label.Width;
                Selected.Label.Font = new Font(Selected.Label.Font, Selected.Label.Font.Style & ~FontStyle.Bold);
                Selected.Label.Refresh();
                Selected.Label.Location = new Point(Selected.Label.Location.X + (oldWidth- Selected.Label.Width) / 2, Selected.Label.Location.Y);
            }

            Selected = b;
            tbSelectedFirst.Text = b.First;
            tbSelectedLast.Text = b.Last;
            tbSelectedBig.Text = b.HasParent() ? ((Brother)(b.Parent())).ToString() : "";
            tbSelectedLittles.Text = "";
            
            for (var i = 0; i < b.GetNumberOfChildren(); i++)
            {
                Brother l = (Brother)(b[i]);
                tbSelectedLittles.Text += (i == 0 ? "" : Environment.NewLine ) + l.ToString();
            }

            dtpSelectedYear.Value = new DateTime(b.IniYear, 1, 1);
            if (b.IniMonth != "")
            {
                cbSelectedTerm.SelectedItem = b.IniMonth;
            }

            chbActive.Checked = b.Active;

            btnEditSelected.Enabled = true;
        }

        private void HideSelectedEdit()
        {
            splitTreeInfo.Panel2Collapsed = true;
        }

        private bool IsSelectedDataEdited()
        {
            SelectedEdits = FieldEdit.NONE;
            
            if (tbSelectedFirst.Text != Selected.First)
            {
                SelectedEdits |= FieldEdit.FIRST_NAME;
            }

            if (tbSelectedLast.Text != Selected.Last)
            {
                SelectedEdits |= FieldEdit.LAST_NAME;
            }

            if (tbSelectedBig.Text != ((Brother)(Selected.Parent())).ToString())
            {
                SelectedEdits |= FieldEdit.BIG;
            }

            var littles = tbSelectedLittles.Text.Split(new Char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (littles.Length != Selected.GetNumberOfChildren())
            {
                SelectedEdits |= FieldEdit.LITTLES;
            }
            else
            {
                for (var i = Selected.GetNumberOfChildren() - 1; i >= 0; i--)
                {
                    if (!littles.Contains(((Brother)Selected[i]).ToString()))
                    {
                        SelectedEdits |= FieldEdit.LITTLES;
                    }
                }
            }

            if (dtpSelectedYear.Value.Year != Selected.IniYear)
            {
                SelectedEdits |= FieldEdit.INI_YEAR;
            }

            if (cbSelectedTerm.Text != Selected.IniMonth)
            {
                SelectedEdits |= FieldEdit.INI_MONTH;
            }

            if (chbActive.Checked != Selected.Active)
            {
                SelectedEdits |= FieldEdit.ACTIVE;
            }
            

            return ((SelectedEdits & FieldEdit.ALL_MASK) != 0);
        }

        #endregion

        #region MySql-Specific Methods

        private bool ConnectDB(string server, int port, string dbName, string uName, string pWord)
        {
            var ret = true;
            var cs = @"server=" + server + ";port=" + port + ";userid=" + uName + ";password=" + pWord + ";database=" + dbName + ";allow user variables=true";
            
            try
            {
                DbConnect = new MySqlConnection(cs);
                DbConnect.Open();
                if (DbConnect.State == ConnectionState.Open)
                {
                    DbConnect.Close();
                    Properties.Settings.Default.RecentMySqlConnection = DbConnect;
                }
                
            }
            catch (MySqlException ex)
            {
                MessageBox.Show(ex.Message);
                ret = false;
            }
            
            return ret;
        }

        private bool ConnectDB(MySqlConnection conn)
        {
            var ret = true;
            
            try
            {
                DbConnect = conn;
                DbConnect.Open();
                if (DbConnect.State == ConnectionState.Open)
                {
                    DbConnect.Close();
                    Properties.Settings.Default.RecentMySqlConnection = DbConnect;
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

        private string ConvertTreeToXml(Brother B) //TODO
        {
            // Begin the Tag
            var xmlData = "<Brother ";

            // Attributes
            xmlData += "Last=\"" + B.Last + "\" ";
            xmlData += "First=\"" + B.First + "\" ";
            xmlData += "IniTerm=\"" + B.IniMonth + "\" ";
            xmlData += "IniYear=\"" + B.IniYear + "\" ";
            xmlData += "Active=\"" + B.Active.ToString() + "\" ";

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
            var big = new Brother(currentParent.Attributes["Last"].Value,
                                        currentParent.Attributes["First"].Value,
                                        currentParent.Attributes["IniTerm"].Value,
                                        Int32.Parse(currentParent.Attributes["IniYear"].Value));

            if (currentParent.Attributes["Active"] != null)
            {
                var val = currentParent.Attributes["Active"].Value;
                switch (val.ToUpper())
                {
                    case "YES": //TODO
                    case "Y":
                    case "TRUE":
                    case "T":
                    case "1":
                        big.Active = true;
                        break;
                    default:
                        big.Active = false;
                        break;
                }
            }
            else
            {
                big.Active = false;
            }

            big.Label.ContextMenuStrip = cmNodeActions;
            ttTree.SetToolTip(big.Label, "Left click to select and edit");

            foreach (XmlNode child in currentParent.ChildNodes)
            {
                big.AddChild(ConvertXmlToTree(child));
            }
            
            return big;
        }

        private void ExportToXml(string filePath, string parentNodeName) //TODO
        {
            // Xml Document Header
            var xmlDoc = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" +
                               "<" + parentNodeName + ">";
            // Fill it with all the nodes
            xmlDoc += ConvertTreeToXml(root);

            // Close the Document Parent Node
            xmlDoc += "</" + parentNodeName + ">";

            var tmp = new XmlDocument();
            tmp.LoadXml(xmlDoc);
            var xWriter = new XmlTextWriter(filePath, Encoding.UTF8);
            xWriter.Formatting = Formatting.Indented;
            tmp.Save(xWriter);
            xWriter.Close();

            if (filePath == OpenedXmlFilePath)
            {
                if (File.Exists(OpenedXmlFilePath + ".sav"))
                {
                    File.Delete(OpenedXmlFilePath + ".sav");
                }
            }
        }

        private void ImportFromXml()
        {
            if (!File.Exists(OpenedXmlFilePath))
            {
                // Xml Document Header
                var xmlData = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n" +
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
                        var val = currentParent.Attributes["Active"].Value;
                        switch (val.ToUpper())
                        {
                            case "yes":
                            case "y":
                            case "true":
                            case "t":
                            case "1":
                                root.Active = true;
                                break;
                            default:
                                root.Active = false;
                                break;
                        }
                    }
                    else
                    {
                        root.Active = false;
                    }

                    root.Label.ContextMenuStrip = cmNodeActions;
                }

                foreach (XmlNode child in currentParent.ChildNodes)
                {
                    root.AddChild(ConvertXmlToTree(child));
                }
                saveXmlToolStripMenuItem.Enabled = true;

                if (XmlParentNodeName == null)
                {
                    XmlParentNodeName = XmlDoc.DocumentElement.Name;
                }

                ExportToXml(OpenedXmlFilePath + ".BAK", XmlParentNodeName);
                AutoSave.Start();
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
            {
                throw new ArgumentNullException("panel");
            }

            if (filePath == null)
            {
                throw new ArgumentNullException("filePath");
            }

            // get parent form (may not be a direct parent)
            var form = panel.FindForm();
            if (form == null)
                throw new ArgumentException(null, "panel");

            // remember form position
            var w = form.Width; //todo rename vars
            var h = form.Height;
            var l = form.Left;
            var t = form.Top;

            // get panel virtual size
            var display = panel.DisplayRectangle;

            // get panel position relative to parent form
            var panelLocation = panel.PointToScreen(panel.Location);
            var panelPosition = new Size(panelLocation.X - form.Location.X, panelLocation.Y - form.Location.Y);

            // resize form and move it outside the screen
            var neededWidth = panelPosition.Width + display.Width;
            var neededHeight = panelPosition.Height + display.Height;
            form.SetBounds(0, -neededHeight, neededWidth, neededHeight, BoundsSpecified.All);

            // resize panel (useless if panel has a dock)
            var pw = panel.Width;
            var ph = panel.Height;
            panel.SetBounds(0, 0, display.Width, display.Height, BoundsSpecified.Size);

            // render the panel on a bitmap
            try
            {
                var bmp = new Bitmap(display.Width, display.Height);
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
            // ------------- OLD WAY, KEEP FOR NOW ------------- // //TODO
            //Graphics myGraphics = pnlTree.CreateGraphics();
            //Image panelImage = new Bitmap(pnlTree.DisplayRectangle.Width, pnlTree.DisplayRectangle.Height, myGraphics);
            //Graphics memoryGraphics = Graphics.FromImage(panelImage);
            //IntPtr dc1 = myGraphics.GetHdc();
            //IntPtr dc2 = memoryGraphics.GetHdc();
            //BitBlt(dc2, 0, 0, pnlTree.DisplayRectangle.Width, pnlTree.DisplayRectangle.Height, dc1, 0, 0, 13369376);
            //myGraphics.ReleaseHdc(dc1);
            //memoryGraphics.ReleaseHdc(dc2);
            var old = pnlTree.Location;
            pnlTree.Location = new Point(0, 0);
            var panelImage = new Bitmap(pnlTree.Width, pnlTree.Height);
            pnlTree.DrawToBitmap(panelImage, new Rectangle(pnlTree.Location.X, pnlTree.Location.Y, pnlTree.Width, pnlTree.Height));
            pnlTree.Location = old;
            return panelImage;
        }

        #endregion

        #region GUI Event Handlers

        #region Main Form

        private void frmMain_Load(object sender, EventArgs e)
        {
            Brother.SelectCallback = PopulateBrotherEdit;
            Brother.ShiftCallback = BoundsCheckShift;

            var start = new StartUp();
            start.ShowDialog();
            if (start.DialogResult != System.Windows.Forms.DialogResult.OK)
            {
                Close();
                return;
            }

            DisplayApex = displayRootOfAllTreeToolStripMenuItem.Checked;
            bIsXml = start.m_bXML;

            if (bIsXml)
            {
                AutoSave.Elapsed += AutoSave_Elapsed;
                AutoSave.Interval = 30000;
                OpenedXmlFilePath = start.m_FilePath;
                XmlParentNodeName = start.m_ParentNode;
                saveXmlToolStripMenuItem.Enabled = true;
            }
            else
            {
                root = new Brother("Tonsor Jr.", "Charles A", "Winter", 1899);

                bIsMale = start.m_bIsMale;

                bool ret;

                if (start.Connection != null)
                {
                    ret = ConnectDB(start.Connection);
                }
                else
                {
                    var server = start.m_Server;
                    var db = start.m_DBase;
                    var user = start.m_UName;
                    var pword = start.m_PWord;
                    var portNum = start.m_Port;

                    ret = ConnectDB(server, portNum, db, user, pword);
                }

                if (!ret)
                {
                    Close();
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
            Text = GenderDependentName + "Tree" + (XmlParentNodeName != "" ? " - " + XmlParentNodeName : "");
            root.Label.ContextMenuStrip = cmNodeActions;

            if (!bIsXml)
            {
                WriteBackReady = true;
            }

            Properties.Settings.Default.Save();

        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (WriteBackReady)
            {
                WriteBackToDB(root);
            }

            Dispose();
        }

        #endregion

        #region Buttons

        #region Add Member Sub-Panel

        private void btnAdd_Click(object sender, EventArgs e)
        {
            var bigName = tbBig.Text;
            var littles = tbLittles.Text.Split(new Char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var last = tbLastName.Text;
            var first = tbFirstName.Text;
            var month = cbIniMonth.Text;
            var year = Int32.Parse(dtpIniYear.Text);

            if (bigName == "")
            {
                bigName = root.ToString();
            }

            var space = bigName.LastIndexOf(' ');
            Brother tmpBig = null;
            var tmp = root.FindBrotherByName(bigName);
            if (tmp != null)
            {
                tmpBig = tmp;
            }
            else
            {
                tmpBig = new Brother(bigName.Substring(space + 1), bigName.Substring(0, space), "Fall", 1920);
                tmpBig.Label.ContextMenuStrip = cmNodeActions;
                root.AddChild(tmpBig);
            }

            Brother newB;
            tmp = root.FindBrotherByName(first + " " + last);
            if (tmp != null)
            {
                newB = tmp;
                tmpBig.AddChild(newB);
            }
            else
            {
                newB = new Brother(last, first, month, year);
                newB.Label.ContextMenuStrip = cmNodeActions;
                tmpBig.AddChild(newB);
            }

            Brother litt;
            for (var i = 0; i < littles.Count(); i++)
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
                    litt.Label.ContextMenuStrip = cmNodeActions;
                    litt.Label.ContextMenuStrip = cmNodeActions;
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
            var parent = (Panel)((Control)(sender)).Parent;
            foreach (Control child in parent.Controls)
            {
                var t = child.GetType();
                if (t.Name == "Label")
                {
                    continue;
                }
                
                if (t.Name == "Button")
                {
                    continue;
                }
                
                if (t.Name == "TextBox")
                {
                    if (((TextBox)child).Multiline == true) //TODO
                    {
                        continue;
                    }

                    if (child.Text == "")
                    {
                        btnAdd.Enabled = false;
                        return;
                    }
                }
                
                if (t.Name == "ComboBox")
                {
                    if (((ComboBox)(child)).SelectedIndex < 0)
                    {
                        btnAdd.Enabled = false;
                        return;
                    }
                }
                
                if (t.Name == "DateTimePicker")
                {
                    if (((DateTimePicker)child).Text == "")
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
            if (cbSelectedTerm.SelectedIndex != -1 && ((SelectedEdits & FieldEdit.INI_MONTH) != 0))
            {
                Selected.IniMonth = cbSelectedTerm.SelectedItem.ToString();
            }

            if ((SelectedEdits & FieldEdit.INI_YEAR) != 0)
            {
                Selected.IniYear = dtpSelectedYear.Value.Year;
            }
            if ((SelectedEdits & FieldEdit.ACTIVE) != 0)
            {
                Selected.Active = chbActive.Checked;
            }

            if (tbSelectedFirst.Text != "" && ((SelectedEdits & FieldEdit.FIRST_NAME) != 0))
            {
                cbTreeParent.Items.Remove(Selected);
                Selected.First = tbSelectedFirst.Text;
                cbTreeParent.Items.Add(Selected);
                cbTreeParent.Sorted = true;
            }
            if (tbSelectedLast.Text != "" && ((SelectedEdits & FieldEdit.LAST_NAME) != 0))
            {
                cbTreeParent.Items.Remove(Selected);
                Selected.Last = tbSelectedLast.Text;
                cbTreeParent.Items.Add(Selected);
                cbTreeParent.Sorted = true;
            }
            if ((SelectedEdits & FieldEdit.BIG) != 0)
            {
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
                        tmp.Label.ContextMenuStrip = cmNodeActions;
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
            }
            if ((SelectedEdits & FieldEdit.LITTLES) != 0)
            {
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
                    for (var i = Selected.GetNumberOfChildren() - 1; i >= 0; i--)
                    {
                        root.AddChild((Brother)Selected[i]);
                    }
                    int space;
                    var littles = tbSelectedLittles.Text.Split(new Char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    Brother litt = null;
                    Brother tmp = null;
                    for (var i = 0; i < littles.Count(); i++)
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
                            litt.Label.ContextMenuStrip = cmNodeActions;
                            Selected.AddChild(litt);
                        }
                    }
                }
            }

            if ((SelectedEdits & (FieldEdit.INI_MONTH | FieldEdit.INI_YEAR)) != 0)
            {
                ((Brother)Selected.Parent()).RefreshLittleOrder();
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
            var tmp = (Brother)((Brother)cbTreeParent.SelectedItem).Parent();
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
            var b = (Brother)lbNoRelation.SelectedItem;
            var EditB = new EditBrotherNoBig(b);
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
                clicked.Label.Dispose();
                clicked.Label = null;
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
            if (firstChild.Label.Parent != null)
            {
                if (firstChild.Label.Visible)
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

        private void makeThisTreeParentToolStripMenuItem_Click(object sender, EventArgs e)
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

            cbTreeParent.SelectedItem = clicked;
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
            if (Selected == null)
            {
                return;
            }
            
            HideSelectedEdit();
            var oldWidth = Selected.Label.Width;
            Selected.Label.Font = new Font(Selected.Label.Font, Selected.Label.Font.Style & ~FontStyle.Bold);
            Selected.Label.Refresh();
            Selected.Label.Location = new Point(Selected.Label.Location.X + (oldWidth - Selected.Label.Width) / 2, Selected.Label.Location.Y);
            Selected = null;
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
                        if (((Brother)(b.Parent())).Label.Parent != null &&
                        ((Brother)(b.Parent())).Label.Visible &&
                        b.Label.Parent != null &&
                        b.Label.Capture == false &&
                        b.Label.Visible)
                        {
                            Pen blackP = new Pen(Color.Black, 1);
                            Point[] pt = 
                        {
                            new Point(((Brother)(b.Parent())).Label.Location.X + 
                                        ((Brother)(b.Parent())).Label.Width / 2,
                                        ((Brother)(b.Parent())).Label.Location.Y +
                                        ((Brother)(b.Parent())).Label.Height),
                            new Point(b.Label.Location.X + b.Label.Width / 2, b.Label.Location.Y)
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
            bool isSelectedEdit = IsSelectedDataEdited();
            if (isSelectedEdit)
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

        #region Timers

        void AutoSave_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            AutoSave.Stop();
            ExportToXml(OpenedXmlFilePath + ".sav", XmlParentNodeName);
            AutoSave.Start();
        }

        #endregion

        #region ToolStripMenuItems

        private void allTreeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cbTreeParent.SelectedItem = "*All*";
        }

        private void activesOnlyTreeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cbTreeParent.SelectedItem = "*Active Only*";
        }

        private void generationUpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (updwnNumGen.Value < updwnNumGen.Maximum)
            {
                updwnNumGen.Value++;
            }
        }

        private void generationDownToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (updwnNumGen.Value > updwnNumGen.Minimum)
            {
                updwnNumGen.Value--;
            }
        }

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

        #endregion

    }
}
