using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Timers;
using System.Windows.Forms;
using System.Xml;
using FraterniTree.Enums;
using FraterniTree.Properties;
using Microsoft.VisualBasic;
using MySql.Data.MySqlClient;
using TreeDisplay;
using Timer = System.Timers.Timer;

namespace FraterniTree.UserInterface
{

    public partial class FamilyTreeForm : Form
    {
        public FamilyTreeForm( )
        {
            InitializeComponent();
        }

        private void PopulateBrothers(bool isXml)
        {
            if( isXml )
            {
                xmlDocument = new XmlDocument();
                ImportFromXml();
                RefreshNoBigListBox( Root );
            }
            else if( databaseConnection != null )
            {
                var cmd = new MySqlCommand(Util.GetLocalizedString("SQLSelectAllBrothers"), databaseConnection);
                databaseConnection.Open();
                var rdr = cmd.ExecuteReader();
                while ( rdr.Read() )
                {
                    var bigName = rdr.GetString( 4 );
                    var last = rdr.GetString( 0 );
                    var first = rdr.GetString( 1 );
                    var month = rdr.GetString( 2 );
                    var year = rdr.GetInt32( 3 );
                    var space = bigName.LastIndexOf( ' ' );

                    Brother tmpBig;
                    var tmp = Root.FindDescendant( bigName );
                    if( tmp != null )
                    {
                        tmpBig = tmp;
                        if( tmpBig.GetParent() == Root || tmpBig.GetParent() == null && tmpBig != Root ) 
                        {
                            RefreshNoBigListBox( Root );
                        }
                    }
                    else
                    {
                        tmpBig = new Brother( bigName.Substring( space + 1 ), bigName.Substring( 0, space ), Util.DefaultInitiationTerm, Util.DefaultYear )
                        {
                            Label = {ContextMenuStrip = cmNodeActions}
                        };
                        Root.AddChild( tmpBig );
                        RefreshNoBigListBox( Root );
                    }

                    Brother newB;
                    var name = Util.FormatName( first, last );
                    tmp = Root.FindDescendant(name);
                    if( tmp != null )
                    {
                        newB = tmp;
                        if( !newB.HasParent() || newB.GetParent() != tmpBig )
                        {
                            tmpBig.AddChild(newB);
                        }

                        newB.InitiationTerm = Util.StringToInitiationTerm(month);
                        newB.InitiationYear = year;
                    }
                    else
                    {
                        newB = new Brother( last, first, month, year )
                        {
                            Label = {ContextMenuStrip = cmNodeActions}
                        };
                        tmpBig.AddChild( newB );
                    }

                    RefreshNoBigListBox( Root );
                }
                rdr.Close();

                databaseConnection.Close();
            }

            if( Root.HasChild() )
            {
                updwnNumGen.Enabled = true;
                cbTreeParent.Enabled = true;
            }

            cbTreeParent.Sorted = true;
        }

        private void RefreshNoBigListBox(Brother brother)
        {
            if( brother == null ) return;
            
            if( brother == Root )
            {
                lbNoRelation.Items.Clear();
            }

            for ( var i = 0; i < brother.ChildCount; i++ )
            {
                var little = (Brother) brother[i];
                if( little == null ) continue;
                
                if( brother == Root ) 
                {
                    if( !lbNoRelation.Items.Contains( little ) )
                    {
                        lbNoRelation.Items.Add( little );
                    }
                }

                if( !cbTreeParent.Items.Contains( little) )
                {
                    cbTreeParent.Items.Add( little );
                }

                if( !CurrentBrothers.Contains(little.ToString()) )
                {
                    CurrentBrothers.Add( little.ToString() );
                }

                RefreshNoBigListBox( little );
            }
        }

        private void RemoveBrotherFromTree(Brother brother)
        {
            var name = brother.ToString();
            lbNoRelation.Items.Remove( brother );
            cbTreeParent.Items.Remove( brother );
            CurrentBrothers.Remove( name );
            RefreshNoBigListBox( Root );
            DisplayTree( true );
        }

        #region Public Data

        public static Brother Root;
        public static readonly AutoCompleteStringCollection CurrentBrothers = new AutoCompleteStringCollection();

        #endregion

        #region Private Data

        private static readonly Timer AutoSave = new Timer();

        #region Mysql-Specific Data

        private MySqlConnection databaseConnection;

        #endregion

        #region XML-Specific Data

        private XmlDocument xmlDocument;
        private string openXmlFilePath = string.Empty;
        private string xmlParentNodeName = string.Empty;

        #endregion

        #region Gender-Dependent Strings

        private string genderDependentName = Util.GetLocalizedString("MaleName");

        #endregion

        #region Tree Display Data

        private Brother selected;
        private Brother treeRoot;
        private int previousSelectedIndex = -1;

        private const float ZoomFactor = 1.5F;
        private int zoomLevel;

        private int maximumWidth;
        private int maxGeneration = 1;

        #endregion

        #region Flags

        private bool bIsMale = true;
        private bool bIsXml;
        private bool displayApex;
        private bool fixedWidth;
        private bool writeBackReady;
        private FieldEdit selectedEdits = FieldEdit.None;

        #endregion

        #endregion

        #region Graphical Tree Display

        private void DisplayTree(bool isScrollMaintained = false)
        {
            if( cbTreeParent.SelectedIndex == -1 || cbTreeParent.Text == string.Empty )
            {
                btnUp.Visible = false;
                return;
            }

            var horizPercentage = splitTreeInfo.Panel1.HorizontalScroll.Value / (float)splitTreeInfo.Panel1.HorizontalScroll.Maximum;
            var vertPercentage = splitTreeInfo.Panel1.VerticalScroll.Value / (float)splitTreeInfo.Panel1.VerticalScroll.Maximum;

            CreateTree();
            PostCreationShift();

            if( isScrollMaintained )
            {
                splitTreeInfo.Panel1.HorizontalScroll.Value = (int)(horizPercentage * splitTreeInfo.Panel1.HorizontalScroll.Maximum);
                splitTreeInfo.Panel1.VerticalScroll.Value = (int)(vertPercentage * splitTreeInfo.Panel1.VerticalScroll.Maximum);
                splitTreeInfo.Panel1.PerformLayout();
            }
        }

        private void AddLabelsToPanel(Brother parent, int generations) 
        {
            if( generations < 0 ) return; 

            var count = parent.ChildCount;
            parent.PreliminaryLocation = 0;
            parent.Modifier = 0;
            parent.Label.AutoSize = !fixedWidth;

            if( !parent.Ignored )
            {
                pnlTree.Controls.Add( parent.Label );
            }

            if( parent.Label.AutoSize )
            {
                maximumWidth = Math.Max( maximumWidth, parent.Label.Width );
                parent.Width = parent.Label.Width;
            }
            else
            {
                parent.Label.AutoSize = true;
                parent.Label.Parent.Refresh();
                maximumWidth = Math.Max( maximumWidth, parent.Label.Width );
                parent.Label.AutoSize = false;
            }

            for ( var i = 0; i < count; i++ ) 
            {
                if( !parent[i].Ignored )
                {
                    AddLabelsToPanel( (Brother) parent[i], generations - 1 );
                }
            }
        }

        private void BoundsCheckShift( )
        {
            var leastPosX = 0;
            var leastPosY = 0;

            foreach ( Control control in pnlTree.Controls )
            {
                if( control.Location.X < 0 )
                {
                    leastPosX = Math.Max( leastPosX, -control.Location.X );
                }

                if( control.Location.Y < 0 )
                {
                    leastPosY = Math.Max( leastPosY, -control.Location.Y );
                }
            }

            if( leastPosX <= 0 && leastPosY <= 0 ) return;

            foreach (Control control in pnlTree.Controls)
            {
                control.Location = new Point(control.Location.X + leastPosX, control.Location.Y + leastPosY);
            }
        }

        private void CreateTree( )
        {
            displayRootOfAllTreeToolStripMenuItem.Enabled = false;
            generationDownToolStripMenuItem.Enabled = false;
            generationUpToolStripMenuItem.Enabled = false;

            HideSelectedEdit();
            UnzoomControls();

            pnlTree.Visible = false;

            Root.ClearIgnoreFlag();

            if( selected != null ) 
            {
                selected.Label.Font = new Font( selected.Label.Font, selected.Label.Font.Style & ~FontStyle.Bold );
            }

            selected = null;

            pnlTree.Controls.Clear();

            if( cbTreeParent.Text == string.Empty ) return;

            if( cbTreeParent.Text != Util.GetLocalizedString("AllFilter") && cbTreeParent.Text != Util.GetLocalizedString("ActiveOnlyFilter") )
            {
                treeRoot = (Brother) cbTreeParent.SelectedItem;
            }
            else
            {
                displayRootOfAllTreeToolStripMenuItem.Enabled = true;
                treeRoot = Root;

                if( cbTreeParent.Text == Util.GetLocalizedString("ActiveOnlyFilter") )
                {
                    displayRootOfAllTreeToolStripMenuItem.Enabled = false;
                    treeRoot.SetIgnoreFlag();
                }
            }

            if( treeRoot == null )
            {
                pnlTree.Controls.Clear();
            }
            else
            {
                generationDownToolStripMenuItem.Enabled = true;
                generationUpToolStripMenuItem.Enabled = true;
                var size = treeRoot.Label.Size;
                var point = new Point( splitTreeInfo.Panel1.Width/2 - size.Width/2, 2 );
                treeRoot.HorizontalCoordinate = point.X;
                treeRoot.VerticalCoordinate = point.Y;
                var generations = treeRoot.GetGenerationsCount();
                updwnNumGen.Maximum = generations;
                maximumWidth = 0;
                AddLabelsToPanel( treeRoot, maxGeneration );
                
                if( fixedWidth )
                {
                    SetLabelWidths( treeRoot, maxGeneration );
                }

                WalkerAlgorithmTree.VerticalLevelSeparation = (int) updwnVertSpace.Value;
                WalkerAlgorithmTree.MaxLevelDepth = (int) updwnNumGen.Value;
                WalkerAlgorithmTree.HorizontalSiblingSeparationFine = (int) updwnHorizSpace.Value;
                WalkerAlgorithmTree.HorizontalSubtreeSeparationCoarse = (int) updwnSubTree.Value;
                WalkerAlgorithmTree.PositionTree( treeRoot );
            }
        }

        private void PostCreationShift( )
        {
            var leastPosX = 0;
            var leastPosY = 0;

            foreach ( Control control in pnlTree.Controls )
            {
                control.Location = new Point( control.Location.X - control.Width/2, control.Location.Y );

                if( control.Location.X < 0 )
                {
                    leastPosX = Math.Max( leastPosX, -control.Location.X );
                }

                if( control.Location.Y < 0 )
                {
                    leastPosY = Math.Max( leastPosY, -control.Location.Y );
                }
            }

            if( leastPosX > 0 || leastPosY > 0 )
            {
                foreach ( Control c in pnlTree.Controls ) 
                {
                    c.Location = new Point( c.Location.X + leastPosX, c.Location.Y + leastPosY );
                }
            }

            if( !displayApex )
            {
                if( Root.Label.Parent != null )
                {
                    Root.Label.Parent = null;
                    leastPosY = Root.Label.Height + (int) updwnVertSpace.Value;

                    foreach ( Control control in pnlTree.Controls ) 
                    {
                        control.Location = new Point( control.Location.X, control.Location.Y - leastPosY );
                    }
                }
            }
            pnlTree.Visible = true;
        }

        private void SetLabelWidths(Brother parent, int generations) 
        {
            if( generations < 0 ) return; 

            var count = parent.ChildCount;
            parent.Label.Width = maximumWidth;
            parent.Width = parent.Label.Width;

            for ( var i = 0; i < count; i++ )
            {
                SetLabelWidths( (Brother) parent[i], generations - 1 );
            }
        }

        private void UnzoomControls( )
        {
            float zoomFactor;

            if( zoomLevel == 0 ) return;
            if( zoomLevel < 0 ) 
            {
                zoomFactor = (float) Math.Pow( ZoomFactor, -zoomLevel );
            }
            else
            {
                zoomFactor = (float) Math.Pow( 1/ZoomFactor, zoomLevel );
            }

            foreach ( Control control in pnlTree.Controls )
            {
                var label = (Label) control;
                label.Font = new Font( label.Font.FontFamily, label.Font.Size*zoomFactor );
                var sf = new SizeF( zoomFactor, zoomFactor );
                control.Scale( sf );
            }

            zoomLevel = 0;
        }

        #endregion

        #region Selected Member Information Edit

        private void PopulateBrotherEdit(Brother brother)
        {
            selectedEdits = FieldEdit.None;
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

            if( selected != null && selected != brother )
            {
                var oldWidth = selected.Label.Width;
                selected.Label.Font = new Font( selected.Label.Font, selected.Label.Font.Style & ~FontStyle.Bold );
                selected.Label.Refresh();
                selected.Label.Location = new Point( selected.Label.Location.X + (oldWidth - selected.Label.Width) / 2,
                    selected.Label.Location.Y );
            }

            selected = brother;

            if( selected != null )
            {
                tbSelectedFirst.Text = brother.FirstName;
                tbSelectedLast.Text = brother.LastName;
                tbSelectedBig.Text = brother.HasParent()
                    ? ((Brother)brother.GetParent()).ToString()
                    : string.Empty;
                tbSelectedLittles.Text = string.Empty;

                for (var i = 0; i < brother.ChildCount; i++)
                {
                    var littleBrother = (Brother)brother[i];
                    tbSelectedLittles.Text += (i == 0 ? string.Empty : Environment.NewLine) + littleBrother;
                }

                dtpSelectedYear.Value = new DateTime(brother.InitiationYear, 1, 1);
                if( brother.InitiationTerm.ToString() != string.Empty )
                {
                    cbSelectedTerm.SelectedItem = brother.InitiationTerm.ToString();
                }

                chbActive.Checked = brother.Active;
            }
            
            btnEditSelected.Enabled = true;
        }

        private void HideSelectedEdit( )
        {
            splitTreeInfo.Panel2Collapsed = true;
        }

        private bool IsSelectedDataEdited( )
        {
            selectedEdits = FieldEdit.None;

            if( tbSelectedFirst.Text != selected.FirstName )
            {
                selectedEdits |= FieldEdit.FirstName;
            }

            if( tbSelectedLast.Text != selected.LastName )
            {
                selectedEdits |= FieldEdit.LastName;
            }

            if( tbSelectedBig.Text != ((Brother) selected.GetParent()).ToString() )
            {
                selectedEdits |= FieldEdit.Big;
            }

            var littles = tbSelectedLittles.Text.Split( new[] {'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries );
            
            if( littles.Length != selected.ChildCount ) 
            {
                selectedEdits |= FieldEdit.Littles;
            }
            else
            {
                for ( var i = 0; i < selected.ChildCount; i++ ) 
                {
                    if( !littles.Contains( ((Brother) selected[i]).ToString() ) )
                    {
                        selectedEdits |= FieldEdit.Littles;
                    }
                }
            }

            if( dtpSelectedYear.Value.Year != selected.InitiationYear )
            {
                selectedEdits |= FieldEdit.IniYear;
            }

            if( cbSelectedTerm.Text != selected.InitiationTerm.ToString() )
            {
                selectedEdits |= FieldEdit.IniMonth;
            }

            if( chbActive.Checked != selected.Active )
            {
                selectedEdits |= FieldEdit.Active;
            }

            return (selectedEdits & FieldEdit.AllMask) != 0;
        }

        #endregion

        #region MySql-Specific Methods

        private bool ConnectToDatabase(string server, int port, string dbName, string uName, string pWord)
        {
            var ret = true;
            var connectionString = string.Format(Util.GetLocalizedString("ConnectionString"), server, port, uName, pWord, dbName);

            try
            {
                databaseConnection = new MySqlConnection( connectionString );
                databaseConnection.Open();
                if( databaseConnection.State == ConnectionState.Open )
                {
                    databaseConnection.Close();
                    Settings.Default.RecentMySqlConnection = databaseConnection;
                }
            }
            catch ( MySqlException ex )
            {
                MessageBox.Show( ex.Message );
                ret = false;
            }

            return ret;
        }

        private bool ConnectToDatabase(MySqlConnection conn)
        {
            var ret = true;

            try
            {
                databaseConnection = conn;
                databaseConnection.Open();
                if( databaseConnection.State == ConnectionState.Open )
                {
                    databaseConnection.Close();
                    Settings.Default.RecentMySqlConnection = databaseConnection;
                }
            }
            catch ( MySqlException ex )
            {
                MessageBox.Show( ex.Message );
                ret = false;
            }

            return ret;
        }

        private void WriteBackToDatabase(Brother currentParent)
        {
            MySqlCommand sqlCommand = null;
            try
            {
                if( databaseConnection == null ) return;

                if( currentParent.HasChild() )
                {
                    WriteBackToDatabase( (Brother) currentParent.GetFirstChild() );
                }

                if( currentParent.HasRightSibling() ) 
                {
                    WriteBackToDatabase( (Brother) currentParent.GetRightSibling() );
                }

                if( currentParent == Root ) return; 
               
                databaseConnection.Open();
                sqlCommand = new MySqlCommand(Util.GetLocalizedString("SQLInsertIntoBrothers"), databaseConnection);

                sqlCommand.Prepare();
                sqlCommand.Parameters.AddWithValue( "@Last", currentParent.LastName );
                sqlCommand.Parameters.AddWithValue( "@First", currentParent.FirstName );
                sqlCommand.Parameters.AddWithValue( "@IniMonth", currentParent.InitiationTerm.ToString() );
                sqlCommand.Parameters.AddWithValue( "@IniYear", currentParent.InitiationYear );

                sqlCommand.Parameters.AddWithValue( "@Big",
                    currentParent.HasParent() 
                        ? ((Brother) currentParent.GetParent()).ToString() 
                        : string.Empty );

                sqlCommand.Parameters.AddWithValue( "@NextSibling",
                    currentParent.HasRightSibling()
                        ? ((Brother) currentParent.GetRightSibling()).ToString()
                        : string.Empty );

                sqlCommand.Parameters.AddWithValue( "@FirstLittle", 
                    currentParent.HasChild() 
                        ? ((Brother) currentParent.GetFirstChild()).ToString() 
                        : string.Empty );

                sqlCommand.ExecuteNonQuery();
                databaseConnection.Close();
            }
            catch ( Exception exception )
            {
                var message = exception.Message;

                if( sqlCommand != null )
                {
                    message += '\n';
                    message += sqlCommand.CommandText;
                }

                MessageBox.Show(message);
            }
        }

        #endregion

        #region XML-Specific Methods

        private static XmlNode ConvertTreeToXml( XmlDocument xml, Brother brother) 
        {
            XmlNode node = xml.CreateElement("Brother");
            if( node.Attributes == null ) return null;

            var last = xml.CreateAttribute("Last");
            last.Value = brother.LastName;
            node.Attributes.Append(last);

            var first = xml.CreateAttribute("First");
            first.Value = brother.FirstName;
            node.Attributes.Append(first);

            var term = xml.CreateAttribute("IniTerm");
            term.Value = brother.InitiationTerm.ToString();
            node.Attributes.Append(term);

            var year = xml.CreateAttribute("IniYear");
            year.Value = brother.InitiationYear.ToString();
            node.Attributes.Append(year);

            var active = xml.CreateAttribute("Active");
            active.Value = brother.Active.ToString();
            node.Attributes.Append(active);
            
            for ( var i = 0; i < brother.ChildCount; i++ )
            {
                var result = ConvertTreeToXml( xml, (Brother) brother[i] );
                node.AppendChild(result);
            }

            return node;
        }

        private Brother ConvertXmlToTree(XmlNode currentParent)
        {
            if( currentParent == null ) return null;
            if( currentParent.Attributes == null ) return null;
            
            var big = new Brother( currentParent.Attributes["Last"].Value,
                currentParent.Attributes["First"].Value,
                currentParent.Attributes["IniTerm"].Value,
                int.Parse( currentParent.Attributes["IniYear"].Value ) )
            {
                Active = Util.ConvertStringToBool( currentParent.Attributes["Active"].Value ),
                Label = {ContextMenuStrip = cmNodeActions}
            };

            ttTree.SetToolTip(big.Label, Util.GetLocalizedString("LeftClickSelectEdit")); 

            foreach ( XmlNode child in currentParent.ChildNodes )
            {
                big.AddChild( ConvertXmlToTree( child ) );
            }

            return big;
        }

        private void ExportToXml(string filePath, string parentNodeName)
        {
            var xmlDoc = new XmlDocument();
            XmlNode rootNode = xmlDoc.CreateElement(parentNodeName);

            var convertTreeResult = ConvertTreeToXml( xmlDoc, Root );
            rootNode.AppendChild(convertTreeResult);

            XmlNode xmlHeader = xmlDoc.CreateXmlDeclaration(Util.GetLocalizedString("XmlVersion"), Util.GetLocalizedString("XmlEncoding"), null);
            xmlDoc.AppendChild(xmlHeader);

            xmlDoc.AppendChild(rootNode);
            xmlDoc.Save( filePath );

            if( filePath != openXmlFilePath ) return;
            if( File.Exists(openXmlFilePath + Util.GetLocalizedString("DotSav")) )
            {
                File.Delete(openXmlFilePath + Util.GetLocalizedString("DotSav"));
            }
        }

        private void GenerateSampleXml( string filePath )
        {
            var xmlDoc = new XmlDocument();
            XmlNode rootNode = xmlDoc.CreateElement("Sample");

            XmlNode brotherNode = xmlDoc.CreateElement("Brother");

            var last = xmlDoc.CreateAttribute("Last");
            last.Value = Util.DefaultLastName;
            var first = xmlDoc.CreateAttribute("First");
            first.Value = Util.DefaultFirstName;
            var term = xmlDoc.CreateAttribute("IniTerm");
            term.Value = Util.DefaultInitiationTerm;
            var year = xmlDoc.CreateAttribute("IniYear");
            year.Value = Util.DefaultYear.ToString();
            var active = xmlDoc.CreateAttribute("Active");
            active.Value = "true";

            brotherNode.Attributes.Append(last);
            brotherNode.Attributes.Append(first);
            brotherNode.Attributes.Append(term);
            brotherNode.Attributes.Append(year);
            brotherNode.Attributes.Append(active);

            rootNode.AppendChild( brotherNode );

            XmlNode xmlHeader = xmlDoc.CreateXmlDeclaration(Util.GetLocalizedString("XmlVersion"), Util.GetLocalizedString("XmlEncoding"), null);
            xmlDoc.AppendChild(xmlHeader);

            xmlDoc.AppendChild(rootNode);
            xmlDoc.Save(filePath);
        }

        private void ImportFromXml( )
        {
            if( !File.Exists( openXmlFilePath ) )
            {
                GenerateSampleXml( openXmlFilePath );
            }

            xmlDocument.Load( openXmlFilePath );

            if( xmlDocument.DocumentElement.ChildNodes.Count != 1 ) throw new Exception("More than one root node, please check your XML and try again.");

            var currentParent = xmlDocument.DocumentElement.FirstChild;
            if( Root == null )
            {
                var last = currentParent.Attributes["Last"].Value;
                var first = currentParent.Attributes["First"].Value;
                var term = currentParent.Attributes["IniTerm"].Value;
                var year = int.Parse( currentParent.Attributes["IniYear"].Value );

                Root = new Brother( last, first, term, year )
                {
                    Active = Util.ConvertStringToBool( currentParent.Attributes["Active"].Value ),
                    Label =
                    {
                        ContextMenuStrip = cmNodeActions
                    }
                };
            }

            foreach (XmlNode child in currentParent.ChildNodes)
            {
                Root.AddChild(ConvertXmlToTree(child));
            }

            saveXmlToolStripMenuItem.Enabled = true;

            if( xmlParentNodeName == null )
            {
                xmlParentNodeName = xmlDocument.DocumentElement.Name;
            }

            ExportToXml(openXmlFilePath + Util.GetLocalizedString("DotBak"), xmlParentNodeName); 
            AutoSave.Start();
        }

        #endregion

        #region Screenshot Methods

        private void TakeScreenshot(Panel panel, string filePath)
        {
            if( panel == null ) throw new ArgumentNullException( "panel" ); 
            if( filePath == null ) throw new ArgumentNullException( "filePath" ); 

            var form = panel.FindForm();
            if( form == null ) throw new ArgumentException( null, "panel" );

            // remember form position
            var width = form.Width;
            var height = form.Height;
            var left = form.Left;
            var top = form.Top;

            // get panel virtual size
            var display = panel.DisplayRectangle;

            // get panel position relative to parent form
            var panelLocation = panel.PointToScreen( panel.Location );
            var panelPosition = new Size( panelLocation.X - form.Location.X, panelLocation.Y - form.Location.Y );

            // resize form and move it outside the screen
            var neededWidth = panelPosition.Width + display.Width;
            var neededHeight = panelPosition.Height + display.Height;
            form.SetBounds( 0, -neededHeight, neededWidth, neededHeight, BoundsSpecified.All );

            // resize panel (useless if panel has a dock)
            var pw = panel.Width;
            var ph = panel.Height;
            panel.SetBounds( 0, 0, display.Width, display.Height, BoundsSpecified.Size );

            // render the panel on a bitmap
            try
            {
                var bmp = new Bitmap( display.Width, display.Height );
                panel.DrawToBitmap( bmp, display );
                bmp.Save( filePath );
            }
            finally
            {
                // restore
                panel.SetBounds( 0, 0, pw, ph, BoundsSpecified.Size );
                form.SetBounds( left, top, width, height, BoundsSpecified.All );
            }
        }

        private Image CaptureScreen( )
        {
            var old = pnlTree.Location;
            pnlTree.Location = new Point( 0, 0 );
            var panelImage = new Bitmap( pnlTree.Width, pnlTree.Height );
            pnlTree.DrawToBitmap( panelImage,
                new Rectangle( pnlTree.Location.X, pnlTree.Location.Y, pnlTree.Width, pnlTree.Height ) );
            pnlTree.Location = old;
            return panelImage;
        }

        #endregion

        #region GUI Event Handlers

        #region Main Form

        private void frmMain_Load(object sender, EventArgs eventArguments)
        {
            Brother.SelectCallback = PopulateBrotherEdit;
            Brother.ShiftCallback = BoundsCheckShift;

            var start = new ImportDataForm();
            start.ShowDialog();
            if( start.DialogResult != DialogResult.OK )
            {
                Close();
                return;
            }

            displayApex = displayRootOfAllTreeToolStripMenuItem.Checked;
            bIsXml = start.IsXml;

            if( bIsXml )
            {
                AutoSave.Elapsed += AutoSave_Elapsed;
                AutoSave.Interval = 30000;
                openXmlFilePath = start.FilePath;
                xmlParentNodeName = start.ParentNode;
                saveXmlToolStripMenuItem.Enabled = true;
            }
            else
            {
                Root = new Brother( Util.DefaultLastName, Util.DefaultFirstName, Util.DefaultInitiationTerm, Util.DefaultYear );

                bIsMale = start.IsMale;

                bool ret;

                if( start.Connection != null ) 
                {
                    ret = ConnectToDatabase( start.Connection );
                }
                else
                {
                    var server = start.Server;
                    var db = start.Base;
                    var user = start.Username;
                    var pword = start.Password;
                    var portNum = start.Port;

                    ret = ConnectToDatabase( server, portNum, db, user, pword );
                }

                if( !ret )
                {
                    Close();
                    return;
                }
            }

            genderDependentName = bIsMale 
                ? Util.GetLocalizedString("MaleName") 
                : Util.GetLocalizedString("FemaleName");

            PopulateBrothers( bIsXml );
            tbBig.AutoCompleteCustomSource = CurrentBrothers;
            tbSelectedBig.AutoCompleteCustomSource = CurrentBrothers;
            tbSelectedLittles.AutoCompleteCustomSource = CurrentBrothers;
            tbLittles.AutoCompleteCustomSource = CurrentBrothers;
            Text = genderDependentName + Util.GetLocalizedString("Tree") + (xmlParentNodeName != string.Empty ? " - " + xmlParentNodeName : string.Empty);
            Root.Label.ContextMenuStrip = cmNodeActions;

            if( !bIsXml )
            {
                writeBackReady = true;
            }

            Settings.Default.Save();
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if( writeBackReady )
            {
                WriteBackToDatabase( Root );
            }

            Dispose();
        }

        #endregion

        #region Buttons

        #region Add Member Sub-Panel

        private void btnAdd_Click(object sender, EventArgs eventArguments)
        {
            var bigName = tbBig.Text;
            var littles = tbLittles.Text.Split( new[] {'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries );
            var last = tbLastName.Text;
            var first = tbFirstName.Text;
            var month = cbIniMonth.Text;
            var year = int.Parse( dtpIniYear.Text );

            if( bigName == string.Empty )
            {
                bigName = Root.ToString();
            }

            var space = bigName.LastIndexOf(' ');
            Brother tmpBig;
            var tmp = Root.FindDescendant( bigName );
            if( tmp != null ) 
            {
                tmpBig = tmp;
            }
            else
            {
                tmpBig = new Brother( bigName.Substring( space + 1 ), bigName.Substring( 0, space ), Util.DefaultInitiationTerm, Util.DefaultYear )
                {
                    Label = {ContextMenuStrip = cmNodeActions}
                };
                Root.AddChild( tmpBig );
            }

            Brother newB;
            tmp = Root.FindDescendant( Util.FormatName(first, last) );
            
            if( tmp == null )
            {
                newB = new Brother( last, first, month, year )
                {
                    Label = {ContextMenuStrip = cmNodeActions}
                };
                tmpBig.AddChild(newB);
            }
            else
            {
                newB = tmp;
                tmpBig.AddChild(newB);
            }

            for ( var i = 0; i < littles.Length; i++ ) 
            {
                space = littles[i].LastIndexOf( ' ' );
                tmp = Root.FindDescendant(littles[i]);

                Brother littleBrother;
                if( tmp == null )
                {
                    littleBrother = new Brother(littles[i].Substring(space + 1), littles[i].Substring(0, space), Util.DefaultInitiationTerm, newB.InitiationYear + 1)
                    {
                        Label = {ContextMenuStrip = cmNodeActions}
                    };
                    littleBrother.Label.ContextMenuStrip = cmNodeActions;
                    newB.AddChild(littleBrother);
                }
                else
                {
                    littleBrother = tmp;
                    newB.AddChild(littleBrother);
                }
            }

            ClearAddBrother();

            if( cbTreeParent.Enabled == false )
            {
                cbTreeParent.Enabled = true;
                updwnNumGen.Enabled = true;
            }

            RefreshNoBigListBox( Root );
            DisplayTree( true );
            cbTreeParent.Sorted = true;
        }

        private void CheckForValidBrother(object sender, EventArgs eventArguments)
        {
            btnClear.Enabled = true;
            var parent = (Panel) ((Control) sender).Parent;
            foreach ( Control child in parent.Controls )
            {
                var t = child.GetType();

                if( t.Name == "Label" ) continue; 
                if( t.Name == "Button" ) continue; 
                if( t.Name == "TextBox" )
                {
                    if( ((TextBox) child).Multiline ) continue;

                    if( child.Text == string.Empty )
                    {
                        btnAdd.Enabled = false;
                        return;
                    }
                }

                if( t.Name == "ComboBox" )
                {
                    if( ((ComboBox) child).SelectedIndex < 0 )
                    {
                        btnAdd.Enabled = false;
                        return;
                    }
                }

                if( t.Name == "DateTimePicker" )
                {
                    if( ((DateTimePicker) child).Text == string.Empty )
                    {
                        btnAdd.Enabled = false;
                        return;
                    }
                }
            }
            btnAdd.Enabled = true;
        }

        private void ClearAddBrother(object sender = null, EventArgs eventArguments = null)
        {
            tbFirstName.Text = string.Empty;
            tbLastName.Text = string.Empty;
            tbLittles.Text = string.Empty;
            tbBig.Text = string.Empty;
            cbIniMonth.SelectedIndex = -1;
            dtpIniYear.Value = DateTime.Today;
        }

        #endregion

        #region Selected Node Sub-Panel

        private void btnApplySelected_Click(object sender, EventArgs eventArguments)
        {
            if( cbSelectedTerm.SelectedIndex != -1 && ((selectedEdits & FieldEdit.IniMonth) != 0) ) 
            {
                selected.InitiationTerm = Util.StringToInitiationTerm(cbSelectedTerm.SelectedItem.ToString());
            }

            if( (selectedEdits & FieldEdit.IniYear) != 0 )
            {
                selected.InitiationYear = dtpSelectedYear.Value.Year;
            }

            if( (selectedEdits & FieldEdit.Active) != 0 )
            {
                selected.Active = chbActive.Checked;
            }

            if( tbSelectedFirst.Text != string.Empty && ((selectedEdits & FieldEdit.FirstName) != 0) )
            {
                cbTreeParent.Items.Remove( selected );
                selected.FirstName = tbSelectedFirst.Text;
                cbTreeParent.Items.Add( selected );
                cbTreeParent.Sorted = true;
            }

            if( tbSelectedLast.Text != string.Empty && ((selectedEdits & FieldEdit.LastName) != 0) )
            {
                cbTreeParent.Items.Remove( selected );
                selected.LastName = tbSelectedLast.Text;
                cbTreeParent.Items.Add( selected );
                cbTreeParent.Sorted = true;
            }

            if( (selectedEdits & FieldEdit.Big) != 0 )
            {
                if( tbSelectedBig.Text == string.Empty )
                {
                    if( selected.HasParent() )
                    {
                        if( selected != Root )
                        {
                            Root.AddChild( selected );
                        }

                        RefreshNoBigListBox( Root );
                    }
                }
                else
                {
                    var tmp = Root.FindDescendant( tbSelectedBig.Text );
                    if( tmp == null )
                    {
                        var space = tbSelectedBig.Text.LastIndexOf( ' ' );
                        tmp = new Brother( tbSelectedBig.Text.Substring( space + 1 ), tbSelectedBig.Text.Substring( 0, space ), Util.DefaultInitiationTerm, Util.DefaultYear )
                        {
                            Label = {ContextMenuStrip = cmNodeActions}
                        };
                        Root.AddChild( tmp );
                        tmp.AddChild( selected );
                        RefreshNoBigListBox( Root );
                    }
                    else
                    {
                        if( selected.HasParent() ) 
                        {
                            tmp.AddChild( selected );
                        }
                        else
                        {
                            tmp.AddChild( selected );
                        }
                    }
                }
            }

            if( (selectedEdits & FieldEdit.Littles) != 0 )
            {
                if( tbSelectedLittles.Text == string.Empty )
                {
                    for ( var i = 0; i < selected.ChildCount; i ++ ) 
                    {
                        Root.AddChild( (Brother) selected[i] );
                    }

                    RefreshNoBigListBox( Root );
                }
                else
                {
                    for ( var i = 0; i < selected.ChildCount; i++ ) 
                    {
                        Root.AddChild( (Brother) selected[i] );
                    }

                    var littles = tbSelectedLittles.Text.Split( new[] {'\n', '\r'},
                        StringSplitOptions.RemoveEmptyEntries );

                    for ( var i = 0; i < littles.Length; i++ )
                    {
                        var space = littles[0].LastIndexOf( ' ' );
                        var tmp = Root.FindDescendant(littles[0]);
                        Brother littleBrother;
                        if( tmp != null )
                        {
                            littleBrother = tmp;
                            if( littleBrother.HasParent() ) 
                            {
                                selected.AddChild( littleBrother );
                            }
                            else
                            {
                                selected.AddChild( littleBrother );
                                RefreshNoBigListBox( Root );
                            }
                        }
                        else
                        {
                            littleBrother = new Brother(littles[0].Substring(space + 1),
                                littles[0].Substring(0, space),
                                Util.DefaultInitiationTerm, selected.InitiationYear + 1 )
                            {
                                Label = {ContextMenuStrip = cmNodeActions}
                            };
                            selected.AddChild( littleBrother );
                        }
                    }
                }
            }

            if( (selectedEdits & (FieldEdit.IniMonth | FieldEdit.IniYear)) != 0 ) 
            {
                if( selected !=null )
                {
                    ((Brother)selected.GetParent()).RecalculateChildOrder();
                }
            }

            if( treeRoot == selected && cbTreeParent.Text == string.Empty)
            {
                cbTreeParent.SelectedItem = treeRoot;
                PopulateBrotherEdit( treeRoot );
            }
            else
            {
                PopulateBrotherEdit( selected );
            }

            RefreshNoBigListBox( Root );
            cbTreeParent.Sorted = true;
            DisplayTree( true );
        }

        private void btnCancelSelected_Click(object sender, EventArgs e)
        {
            PopulateBrotherEdit( selected );
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
            var tmp = (Brother) ((Brother) cbTreeParent.SelectedItem).GetParent();
            cbTreeParent.Text = tmp == Root 
                ? Util.GetLocalizedString("AllFilter") 
                : tmp.ToString();

            updwnNumGen.Value++;
        }

        #endregion

        #region Top Node Members Sub-Panel

        private void btnEdit_Click(object sender, EventArgs e)
        {
            var b = (Brother) lbNoRelation.SelectedItem; 
            var editBrother = new EditBrotherWithoutBigDialog( b );
            editBrother.ShowDialog();

            if( editBrother.DialogResult == DialogResult.OK )
            {
                RefreshNoBigListBox( Root );
            }
        }

        #endregion

        #endregion

        #region Comboboxes

        private void cbTreeParent_SelectedIndexChanged(object sender, EventArgs eventArguments)
        {
            saveAsToolStripMenuItem.Enabled = true;
            treeViewToolStripMenuItem.Enabled = true;
            zoomInToolStripMenuItem.Enabled = true;
            zoomOutToolStripMenuItem.Enabled = true;

            if( cbTreeParent.SelectedIndex != -1 )
            {
                if( cbTreeParent.Text != Util.GetLocalizedString("AllFilter") && cbTreeParent.Text != Util.GetLocalizedString("ActiveOnlyFilter")
                    && cbTreeParent.Text != string.Empty )
                {
                    btnUp.Visible = ((Brother) cbTreeParent.SelectedItem).HasParent();
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

        private void lbNoRelation_MouseDoubleClick(object sender, MouseEventArgs eventArguments)
        {
            if( lbNoRelation.SelectedIndex != -1 )
            {
                cbTreeParent.Text = ((Brother) lbNoRelation.SelectedItem).ToString();
            }
        }

        private void lbNoRelation_SelectedIndexChanged(object sender, EventArgs eventArguments) 
        {
            if( lbNoRelation.SelectedIndex != -1
                && previousSelectedIndex != lbNoRelation.SelectedIndex )
            {
                previousSelectedIndex = lbNoRelation.SelectedIndex;
                btnEdit.Enabled = true;
            }
            else
            {
                lbNoRelation.SelectedIndex = -1;
                previousSelectedIndex = -1;
                btnEdit.Enabled = false;
            }
        }

        #endregion

        #region Node Context Menu

        private void removeNodeToolStripMenuItem_Click(object sender, EventArgs eventArguments)
        {
            var toolStripItem = sender as ToolStripItem;
            if( toolStripItem == null ) return;

            var contextMenuStrip = toolStripItem.Owner as ContextMenuStrip;
            if( contextMenuStrip == null ) return;

            var label = contextMenuStrip.SourceControl as Label;
            if( label == null ) return;

            var clicked = (Brother) label.Tag;

            var res = MessageBox.Show(Util.GetLocalizedString("DeleteNodeConfirmation"),
                Util.GetLocalizedString("NodeRemovalConfirmation"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning );
            
            if( res == DialogResult.Yes )
            {
                if( selected == clicked )
                {
                    selected = null;
                }

                clicked.RemoveNode();
                clicked.Label.Dispose();
                clicked.Label = null;
                RemoveBrotherFromTree( clicked );
            }
        }

        private void toggleHideDescendantsToolStripMenuItem_Click(object sender, EventArgs eventArguments)
        {
            var toolStripItem = sender as ToolStripItem;
            if( toolStripItem == null ) return;

            var contextMenuStrip = toolStripItem.Owner as ContextMenuStrip;
            if( contextMenuStrip == null ) return;

            var label = contextMenuStrip.SourceControl as Label;
            if( label == null ) return;

            var clicked = (Brother) label.Tag;

            if( clicked.GetFirstChild() == null ) return; 

            var firstChild = (Brother) clicked.GetFirstChild();
            if( firstChild.Label.Parent == null ) return; 

            if( firstChild.Label.Visible )
            {
                label.Font = new Font( label.Font, label.Font.Style | FontStyle.Italic );
                clicked.HideChildren = true;
            }
            else
            {
                label.Font = new Font( label.Font, label.Font.Style & ~FontStyle.Italic );
                clicked.HideChildren = false;
            }

            for (var i = 0; i < clicked.ChildCount; i++)
            {
                Brother.RecursiveToggleLeafVisible( (Brother) clicked[i] );
            }
        }

        private void makeThisTreeParentToolStripMenuItem_Click(object sender, EventArgs eventArguments)
        {
            var tsi = sender as ToolStripItem;
            if( tsi == null ) return;

            var cm = tsi.Owner as ContextMenuStrip;
            if( cm == null ) return;

            var label = cm.SourceControl as Label;
            if( label == null ) return;

            var clicked = (Brother) label.Tag;

            cbTreeParent.SelectedItem = clicked;
        }

        #endregion

        #region Numeric Up-Downs

        private void updwnNumGen_ValueChanged(object sender, EventArgs eventArguments)
        {
            maxGeneration = (int) updwnNumGen.Value;
            if( cbTreeParent.SelectedIndex == -1 ) return; 

            var horizPercentage = splitTreeInfo.Panel1.HorizontalScroll.Value / (float) splitTreeInfo.Panel1.HorizontalScroll.Maximum;
            var vertPercentage = splitTreeInfo.Panel1.VerticalScroll.Value / (float) splitTreeInfo.Panel1.VerticalScroll.Maximum;
                
            CreateTree();
            PostCreationShift();

            splitTreeInfo.Panel1.HorizontalScroll.Value = (int) (horizPercentage*splitTreeInfo.Panel1.HorizontalScroll.Maximum);
            splitTreeInfo.Panel1.VerticalScroll.Value = (int) (vertPercentage*splitTreeInfo.Panel1.VerticalScroll.Maximum);
            splitTreeInfo.Panel1.PerformLayout();
        }

        private void updwnVertSpace_ValueChanged(object sender, EventArgs eventArguments)
        {
            DisplayTree( true );
        }

        private void updwnHorizSpace_ValueChanged(object sender, EventArgs eventArguments)
        {
            DisplayTree( true );
        }

        private void updwnSubTree_ValueChanged(object sender, EventArgs eventArguments)
        {
            DisplayTree( true );
        }

        #endregion

        #region Panels

        private void pnlTree_Click(object sender, EventArgs eventArguments)
        {
            if( selected == null ) return; 

            HideSelectedEdit();
            var oldWidth = selected.Label.Width;
            selected.Label.Font = new Font( selected.Label.Font, selected.Label.Font.Style & ~FontStyle.Bold );
            selected.Label.Refresh();
            selected.Label.Location = new Point( selected.Label.Location.X + (oldWidth - selected.Label.Width)/2, selected.Label.Location.Y );
            selected = null;
        }

        private void pnlTree_MouseDoubleClick(object sender, MouseEventArgs eventArguments)
        {
            if( cbTreeParent.SelectedIndex != -1 ) 
            {
                treeViewToolStripMenuItem.Checked = !treeViewToolStripMenuItem.Checked;
            }

            treeViewToolStripMenuItem_Click( treeControlToolStripMenuItem, EventArgs.Empty );
        }

        #endregion

        #region Split Containers

        private void splitTreeInfo_Panel1_Click(object sender, EventArgs eventArguments)
        {
            pnlTree_Click( pnlTree, eventArguments );
        }

        private void splitTreeInfo_Panel1_MouseDoubleClick(object sender, MouseEventArgs eventArguments)
        {
            pnlTree_MouseDoubleClick( pnlTree, eventArguments );
        }

        private void splitTreeInfo_Panel1_Paint(object sender, PaintEventArgs paintEventArguments)
        {
            foreach ( Control control in pnlTree.Controls )
            {
                var label1 = control as Label;
                if( label1 == null ) continue; 

                var label = label1;
                var brother = Root.FindDescendant( label.Text );

                if( !brother.HasParent() ) continue;
                if( ((Brother) brother.GetParent()).Label.Parent == null ) continue;
                if( !((Brother) brother.GetParent()).Label.Visible ) continue;
                if( brother.Label.Parent == null ) continue;
                if( !brother.Label.Visible ) continue;
                    
                var blackP = new Pen( Color.Black, 1 );
                Point[] pt =
                {
                    new Point( ((Brother) brother.GetParent()).Label.Location.X 
                        + ((Brother) brother.GetParent()).Label.Width/2,
                        ((Brother) brother.GetParent()).Label.Location.Y 
                        + ((Brother) brother.GetParent()).Label.Height ),
                    new Point( brother.Label.Location.X + brother.Label.Width/2, brother.Label.Location.Y )
                };
                paintEventArguments.Graphics.DrawCurve( blackP, pt, 0.00F );
            }
        }

        #region Selected Edit Panel

        private void SelectedEdit_ValueChanged(object sender, EventArgs eventArguments)
        {
            var isSelectedEdit = IsSelectedDataEdited();
            btnApplySelected.Enabled = isSelectedEdit;
        }

        #endregion

        #endregion

        #region Timers

        private void AutoSave_Elapsed(object sender, ElapsedEventArgs eventArguments)
        {
            AutoSave.Stop();
            ExportToXml(openXmlFilePath + Util.GetLocalizedString("AutoSaveFileExtension"), xmlParentNodeName);
            AutoSave.Start();
        }

        #endregion

        #region ToolStripMenuItems

        private void allTreeToolStripMenuItem_Click(object sender, EventArgs eventArguments)
        {
            cbTreeParent.SelectedItem = Util.GetLocalizedString("AllFilter");
        }

        private void activesOnlyTreeToolStripMenuItem_Click(object sender, EventArgs eventArguments)
        {
            cbTreeParent.SelectedItem = Util.GetLocalizedString("ActiveOnlyFilter");
        }

        private void generationUpToolStripMenuItem_Click(object sender, EventArgs eventArguments)
        {
            if( updwnNumGen.Value < updwnNumGen.Maximum )
            {
                updwnNumGen.Value++;
            }
        }

        private void generationDownToolStripMenuItem_Click(object sender, EventArgs eventArguments)
        {
            if( updwnNumGen.Value > updwnNumGen.Minimum )
            {
                updwnNumGen.Value--;
            }
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs eventArguments)
        {
            Close();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs eventArguments)
        {
            var sfd = new SaveFileDialog
            {
                Filter = Util.GetLocalizedString( "AllImagesFilter" ),
                AddExtension = true
            };
            var ret = sfd.ShowDialog();
            if( ret == DialogResult.OK )
            {
                var panelImage = CaptureScreen();
                var fileExtension = Path.GetExtension( sfd.FileName );

                panelImage.Save(sfd.FileName, Util.GetImageFormatFromFileExtension(fileExtension) );
            }
        }

        private void treeViewToolStripMenuItem_Click(object sender, EventArgs eventArguments)
        {
            if( cbTreeParent.SelectedIndex == -1 ) return; 

            var tmp = (ToolStripMenuItem) sender;
            if( tmp.Checked )
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
                if( selected != null )
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

        private void zoomInToolStripMenuItem_Click(object sender, EventArgs eventArguments)
        {
            foreach ( Control control in pnlTree.Controls )
            {
                var label = (Label) control;
                label.Font = new Font( label.Font.FontFamily, label.Font.Size*ZoomFactor );
                var sf = new SizeF( ZoomFactor, ZoomFactor );
                control.Scale( sf );
            }

            zoomLevel++;
        }

        private void zoomOutToolStripMenuItem_Click(object sender, EventArgs eventArguments)
        {
            foreach ( Control control in pnlTree.Controls )
            {
                var label = (Label) control;
                label.Font = new Font( label.Font.FontFamily, label.Font.Size/ZoomFactor );
                var sf = new SizeF( 1/ZoomFactor, 1/ZoomFactor );
                control.Scale( sf );
            }

            zoomLevel--;
        }

        private void displayRootOfAllTreeToolStripMenuItem_Click(object sender, EventArgs eventArguments)
        {
            var tsmi = (ToolStripMenuItem) sender;

            tsmi.Checked = !tsmi.Checked;
            displayApex = tsmi.Checked;
            DisplayTree();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs eventArguments)
        {
            var ab = new AboutCompanyDialog();
            ab.ShowDialog();
        }

        private void supportToolStripMenuItem_Click(object sender, EventArgs eventArguments)
        {
            Process.Start( Util.GetLocalizedString("CompanyWebsite") );
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs eventArguments)
        {
            var sfd = new SaveFileDialog
            {
                Filter = Util.GetLocalizedString("XMLDocumentFilter"),
                AddExtension = true,
                FileName = Path.GetFileName( openXmlFilePath ),
                OverwritePrompt = true,
                DefaultExt = Util.GetLocalizedString("DotXml"),
                Title = Util.GetLocalizedString("SaveAsXml")
            };

            var res = sfd.ShowDialog();
            if( res == DialogResult.OK )
            {
                var parentNodeName = Interaction.InputBox(Util.GetLocalizedString("PromptUSerForParentNodeName"),
                   Util.GetLocalizedString("ParentNodeName"),
                    xmlParentNodeName != string.Empty 
                        ? xmlParentNodeName : Util.GetLocalizedString("MyTree") );

                if( parentNodeName != string.Empty )
                {
                    ExportToXml( sfd.FileName, parentNodeName );
                }
            }
        }

        private void addMemberToolStripMenuItem_Click(object sender, EventArgs eventArguments)
        {
            treeViewToolStripMenuItem.Checked = false;
            splitTreeAdd.Panel2Collapsed = !splitTreeAdd.Panel2Collapsed;
            
            if( !addMemberToolStripMenuItem.Checked 
                && !membersWithoutBigsToolStripMenuItem.Checked
                && !treeControlToolStripMenuItem.Checked ) 
            {
                treeViewToolStripMenuItem.Checked = true;
            }
        }

        private void membersWithoutBigsToolStripMenuItem_Click(object sender, EventArgs eventArguments)
        {
            treeViewToolStripMenuItem.Checked = false;
            if( treeControlToolStripMenuItem.Checked ) 
            {
                splitEditView.Panel1Collapsed = !splitEditView.Panel1Collapsed;
            }
            else
            {
                splitContainer1.Panel2Collapsed = !splitContainer1.Panel2Collapsed;
                splitEditView.Panel1Collapsed = !membersWithoutBigsToolStripMenuItem.Checked;
                splitEditView.Panel2Collapsed = !treeControlToolStripMenuItem.Checked;
            }

            if( !addMemberToolStripMenuItem.Checked 
                && !membersWithoutBigsToolStripMenuItem.Checked
                && !treeControlToolStripMenuItem.Checked ) 
            {
                treeViewToolStripMenuItem.Checked = true;
            }
        }

        private void treeControlToolStripMenuItem_Click(object sender, EventArgs eventArguments)
        {
            treeViewToolStripMenuItem.Checked = false;

            if( membersWithoutBigsToolStripMenuItem.Checked ) 
            {
                splitEditView.Panel2Collapsed = !splitEditView.Panel2Collapsed;
            }
            else
            {
                splitContainer1.Panel2Collapsed = !splitContainer1.Panel2Collapsed;
                splitEditView.Panel1Collapsed = !membersWithoutBigsToolStripMenuItem.Checked;
                splitEditView.Panel2Collapsed = !treeControlToolStripMenuItem.Checked;
            }

            if( !addMemberToolStripMenuItem.Checked 
                && !membersWithoutBigsToolStripMenuItem.Checked
                && !treeControlToolStripMenuItem.Checked ) 
            {
                treeViewToolStripMenuItem.Checked = true;
            }
        }

        private void fixedLabelWidthsToolStripMenuItem_Click(object sender, EventArgs eventArguments)
        {
            fixedWidth = fixedLabelWidthsToolStripMenuItem.Checked;
            DisplayTree();
        }

        private void saveXmlToolStripMenuItem_Click(object sender, EventArgs eventArguments)
        {
            ExportToXml( openXmlFilePath, xmlParentNodeName );
        }

        #endregion

        #endregion
    }

}