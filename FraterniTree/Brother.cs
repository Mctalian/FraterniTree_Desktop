﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TreeDisplay;

namespace FraterniTree
{
    /// <summary>
    /// Represents a Fraternity Brother in a Fraternity Family Tree.
    /// </summary>
    public class Brother : Node
    {
        private string  m_Last;
        private string  m_First;
        private InitiationTerm  m_IniMonth;
        private int     m_IniYear;
        public bool isActiveBrother = false;
        public Label   m_Label = new Label();
        public static Action<Brother> m_SelectCallback = null;
        public static Action m_ShiftCallback = null;
        private bool areChildrenHidden = false;
        private Point lastPos;

        private enum InitiationTerm
        {
            Winter = 0,
            Spring = 1,
            Fall = 2
        };

        #region Constructors

        /// <summary>
        /// Disallow non-parameterized creation of object
        /// </summary>
        private Brother()
        {
            
        }

        /// <summary>
        /// Allow a Brother to be created only by specifying the following parameters
        /// </summary>
        /// <param name="strLast">Last Name of the Brother</param>
        /// <param name="strFirst">First Name of the Brother</param>
        /// <param name="strMonth">Initiation Term of the Brother</param>
        /// <param name="iYear">Initiation Year of the Brother</param>
        public Brother(string strLast, string strFirst, string strMonth, int iYear)
        {
            // Initialize Brother object
            Last                = strLast;
            First               = strFirst;
            IniMonth            = strMonth;
            IniYear           = iYear;            

            // Initialize the label
            m_Label.Text        = ToString();
            m_Label.TextAlign   = System.Drawing.ContentAlignment.MiddleCenter;
            m_Label.Padding     = new Padding(4);
            m_Label.AutoSize    = true;
            m_Label.BorderStyle = BorderStyle.FixedSingle;
            m_Label.Tag         = this;
            m_Label.MouseClick += m_Label_MouseClick;
            m_Label.MouseDown  += m_Label_MouseDown;
            m_Label.MouseMove  += m_Label_MouseMove;
            m_Label.MouseUp    += m_Label_MouseUp;
            m_Label.Paint      += m_Label_Paint;
            m_Label.LocationChanged += m_Label_LocationChanged;
            m_Label.ParentChanged += m_Label_ParentChanged;

            SetWidth(m_Label.Width);
            SetHeight(m_Label.Height);
            SetCallback(ApplyNodeLocationsToLabel);
        }

        #endregion

        public string Last
        { 
            get
            { 
                return m_Last;
            }

            set
            {
                m_Last       = value;
                m_Label.Text = ToString();
            }
        }

        public string First
        {
            get
            {
                return m_First;
            }

            set
            {
                m_First      = value;
                m_Label.Text = ToString();
            }
        }

        public string IniMonth
        {
            get
            {
                return m_IniMonth.ToString();
            }

            set
            {
                m_IniMonth = (InitiationTerm)Enum.Parse(typeof(InitiationTerm), value);
            }
        }

        public int IniYear
        {
            get
            {
                return m_IniYear;
            }

            set
            {
                m_IniYear = value;
            }
        }

        public override string ToString()
        {
            return First + " " + Last;
        }

        public Brother FindBrotherByName(string fullName)
        {
            Brother found = null;

            if (this.ToString() == fullName)
            {
                return this;
            }

            for (int i = this.GetNumberOfChildren() - 1; i >= 0; i--)
            {
                found = ((Brother)this[i]).FindBrotherByName(fullName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        public void SetDescendantsHidden(bool status)
        {
            areChildrenHidden = status;
        }

        public bool RecursiveSetIgnoreNode()
        {
            bool isThisIgnored = !isActiveBrother;
            for (int i = this.GetNumberOfChildren() - 1; i >= 0; i--)
            {
                if (isThisIgnored)
                {
                    isThisIgnored = ((Brother)(this[i])).RecursiveSetIgnoreNode();
                }
                else
                {
                    // Ignore return, but set descendant nodes accordingly
                    ((Brother)(this[i])).RecursiveSetIgnoreNode();
                }
            }

            this.SetIgnore(isThisIgnored);
            return isThisIgnored;
        }

        public void RecursiveClearIgnoreNode()
        {
            this.SetIgnore(false);
            for (int i = this.GetNumberOfChildren() - 1; i >= 0; i--)
            {
                if (this[i] == null)
                {
                    continue;
                }
                ((Brother)(this[i])).RecursiveClearIgnoreNode();
            }
        }

        public void RefreshLittleOrder()
        {
            bool isSorted;
            do
            {
                isSorted = true;
                for (int i = 1; i < GetNumberOfChildren(); i++)
                {
                    Brother L = (Brother)this[i - 1];
                    Brother R = (Brother)this[i];
                    if (R.m_IniYear < L.m_IniYear)
                    {
                        SwapLittles(L, R);
                        isSorted = false;
                    }
                    else if (R.m_IniYear == L.m_IniYear)
                    {
                        if (R.m_IniMonth < L.m_IniMonth)
                        {
                            SwapLittles(L, R);
                            isSorted = false;
                        }
                    }
                    else
                    {
                        // Do nothing
                    }
                }
            } while (!isSorted);
        }

        private void SwapLittles(Brother L, Brother R)
        {
            if (L.HasParent(true) && R.HasParent(true))
            {
                if ((Brother)L.Parent(true) == (Brother)R.Parent(true) && (Brother)L.Parent(true) == this)
                {
                    if (L.HasRightSibling(true) && R.HasLeftSibling(true))
                    {
                        if ((Brother)L.RightSibling(true) == R && (Brother)R.LeftSibling(true) == L)
                        {
                            if (L == (Brother)this.FirstChild(true))
                            {
                                this.FirstChild(R);
                            }

                            R.LeftSibling(L.LeftSibling(true));
                            L.RightSibling(R.RightSibling(true));

                            if (R.HasRightSibling(true))
                            {
                                ((Brother)R.RightSibling(true)).LeftSibling(L);
                            }
                            if (L.HasLeftSibling(true))
                            {
                                ((Brother)L.LeftSibling(true)).RightSibling(R);
                            }

                            L.LeftSibling(R);
                            R.RightSibling(L);
                        }
                    }
                }
            }
        }

        public void AddChild(Brother Child)
        {
            base.AddChild(Child);
            RefreshLittleOrder();
        }

        #region GUI Label Methods

        private void ApplyNodeLocationsToLabel()
        {
            m_Label.Location = new Point(GetXCoord(), GetYCoord());
        }

        private void RecursiveLabelMove(Brother b, int dx, int dy)
        {
            if (b.m_Label.Parent != null)
            {
                b.m_Label.Location = new Point(b.m_Label.Left + dx, b.m_Label.Top + dy);
            }
            if (b.HasRightSibling())
            {
                RecursiveLabelMove((Brother)(b.RightSibling()), dx, dy);
            }
            if (b.HasChild())
            {
                RecursiveLabelMove((Brother)(b.FirstChild()), dx, dy);
            }
        }

        private void RecursiveLabelCapture(Brother b, MouseEventArgs e)
        {
            if (b.m_Label.Parent != null)
            {
                b.lastPos = e.Location;
                b.m_Label.BringToFront();
                if (b.HasRightSibling())
                {
                    RecursiveLabelCapture((Brother)(b.RightSibling()), e);
                }
                if (b.HasChild())
                {
                    RecursiveLabelCapture((Brother)(b.FirstChild()), e);
                }
            }
        }

        public void RecursiveLabelVisibleToggle(Brother b)
        {
            if (b.m_Label.Parent != null)
            {
                if (((Brother)(b.Parent())).m_Label.Parent != null)
                {
                    b.m_Label.Visible = ((Brother)(b.Parent())).m_Label.Visible && !((Brother)(b.Parent())).areChildrenHidden;
                }
                else
                {
                    b.m_Label.Visible = !b.m_Label.Visible;
                }
            }
            else
            {
                b.m_Label.Visible = !((Brother)(b.Parent())).areChildrenHidden;
            }
            if (!b.areChildrenHidden)
            {
                for (int i = b.GetNumberOfChildren() - 1; i >= 0; i--)
                {
                    RecursiveLabelVisibleToggle((Brother)(b[i]));
                }
            }
        }

        #region GUI Event Handlers

        private void m_Label_ParentChanged(object sender, EventArgs e)
        {
            // Does nothing
        }

        private void m_Label_LocationChanged(object sender, EventArgs e)
        {
            // Does nothing
        }

        private void m_Label_Paint(object sender, PaintEventArgs e)
        {
            if (isActiveBrother)
            {
                m_Label.ForeColor = Color.White;
                m_Label.BackColor = Color.DarkGreen;
            }
            else
            {
                m_Label.ForeColor = Color.Empty;
                m_Label.BackColor = Color.Empty;
            }

            Brother parent = ((Brother)(this.Parent()));

            if (parent.m_Label.Parent != null && !parent.m_Label.Visible)
            {
                m_Label.Visible = parent.m_Label.Visible && !parent.areChildrenHidden;
            }
        }

        private void m_Label_MouseUp(object sender, MouseEventArgs e)
        {
            if (m_Label != null)
            {
                m_Label.Capture = false;
                if (m_Label.Parent != null)
                {
                    m_Label.Parent.Invalidate();
                }
            }
            m_ShiftCallback();
        }

        private void m_Label_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int dx = e.X - lastPos.X;
                int dy = e.Y - lastPos.Y;
                m_Label.Location = new Point(m_Label.Left + dx, m_Label.Top + dy);
                if (this.HasChild())
                {
                    RecursiveLabelMove((Brother)this.FirstChild(), dx, dy);
                }
            }
        }

        private void m_Label_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                lastPos = e.Location;
                m_Label.BringToFront();
                m_Label.Capture = true;
                if (this.HasChild())
                {
                    RecursiveLabelCapture((Brother)this.FirstChild(), e);
                }
            }
        }

        private void m_Label_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int oldWidth = m_Label.Width;
                m_Label.Font = new Font(m_Label.Font, m_Label.Font.Style | FontStyle.Bold);
                m_Label.Refresh();
                m_Label.Location = new Point(m_Label.Location.X - (m_Label.Width - oldWidth) / 2, m_Label.Location.Y);
                if (m_SelectCallback != null)
                {
                    m_SelectCallback(this);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        #endregion

        #endregion

    }
}