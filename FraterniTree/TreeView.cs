using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;

namespace FraterniTree
{
    public partial class TreeView : Form
    {
        public Brother m_Root;
        private List<Image> _pages = new List<Image>();
        private int pageIndex = 0;

        public TreeView(Control[] ctrls, Brother root)
        {
            InitializeComponent();
            pnlTree.Controls.AddRange(ctrls);
            m_Root = root;
        }

        private void FullScreen_Load(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Maximized;
            this.MaximumSize = this.Size;
            this.MinimumSize = this.Size;
        }

        private void pd_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            Image panelImage = CaptureScreen();
            //panelImage.Save("C:\\tmp.bmp", System.Drawing.Imaging.ImageFormat.Bmp);
            if (panelImage != null)
            {
                
                e.Graphics.DrawImage(panelImage, pnlTree.Location.X, pnlTree.Location.Y, pnlTree.DisplayRectangle.Width, pnlTree.DisplayRectangle.Height);
                
                //m_NumOfPages--;
                //if (m_NumOfPages > 0)
                //{
                //    e.HasMorePages = true;
                //}
                //else
                //{
                //    e.HasMorePages = false;
                //}
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

        private void pnlTree_Paint(object sender, PaintEventArgs e)
        {
            //m_NumOfPages = 1;
            foreach (Label l in pnlTree.Controls)
            {
                Brother b = m_Root.FindBrotherByName(l.Text);
                if (b.GetNodeRef().HasParent())
                {
                    if (((Brother)(b.GetNodeRef().Parent().GetUserData())).m_Label.Parent != null && b.m_Label.Parent != null && b.m_Label.Capture == false)
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
                //else if (b.m_Big.m_Label.Parent == null)
                //{
                //    if (l.Location.X + l.Size.Width > 800)
                //    {
                //        m_NumOfPages++;
                //    }
                //    if (l.Location.Y + l.Size.Height > 1000)
                //    {
                //        m_NumOfPages++;
                //    }
                //}
            }
        }

        private void pnlTree_Click(object sender, EventArgs e)
        {
            foreach (Label l in pnlTree.Controls)
            {
                l.BackColor = System.Drawing.Color.White;
            }
        }

        private void printToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (printDialog.ShowDialog() == DialogResult.OK)
            {
                pd.Print();
            }
        }

        private void previewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            printPreview.WindowState = FormWindowState.Maximized;
            printPreview.ShowDialog();
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

        private void panel1_Click(object sender, EventArgs e)
        {
            pnlTree_Click((Object)(pnlTree), e);
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Abort;
            this.Close();
        }
    }
}
