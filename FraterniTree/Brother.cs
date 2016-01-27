using System;
using System.Drawing;
using System.Windows.Forms;
using TreeDisplay;

namespace FraterniTree
{
    /// <summary>
    /// Represents a Fraternity Brother in a Fraternity Family Tree.
    /// </summary>
    public class Brother : Node
    {
        private string  _lastName;
        private string  _firstName;
        private InitiationTerm  _initiationTerm;
        private int _initiationYear;
        public bool Active = false;
        public Label Label = new Label();
        public static Action<Brother> SelectCallback = null;
        public static Action ShiftCallback = null;
        private bool _hiddenChildren = false;
        private Point _lastPoint;

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
        public Brother(string strLastName, string strFirstName, string strMonth, int year)
        {
            // Initialize Brother object
            Last = strLastName;
            First = strFirstName;
            IniMonth            = strMonth; //TODO - where is this coming from?
            IniYear           = year;            

            // Initialize the label
            Label.Text        = ToString();
            Label.TextAlign   = System.Drawing.ContentAlignment.MiddleCenter;
            Label.Padding     = new Padding(4);
            Label.AutoSize    = true;
            Label.BorderStyle = BorderStyle.FixedSingle;
            Label.Tag         = this;
            Label.MouseClick += m_Label_MouseClick;
            Label.MouseDown  += m_Label_MouseDown;
            Label.MouseMove  += m_Label_MouseMove;
            Label.MouseUp    += m_Label_MouseUp;
            Label.Paint      += m_Label_Paint;
            Label.LocationChanged += m_Label_LocationChanged;
            Label.ParentChanged += m_Label_ParentChanged;

            SetWidth(Label.Width);
            SetHeight(Label.Height);
            SetCallback(ApplyNodeLocationsToLabel);
        }

        #endregion

        public string Last
        { 
            get
            { 
                return _lastName;
            }

            set
            {
                _lastName       = value;
                Label.Text = ToString();
            }
        }

        public string First
        {
            get
            {
                return _firstName;
            }

            set
            {
                _firstName      = value;
                Label.Text = ToString();
            }
        }

        public string IniMonth
        {
            get
            {
                return _initiationTerm.ToString();
            }

            set
            {
                _initiationTerm = (InitiationTerm)Enum.Parse(typeof(InitiationTerm), value);
            }
        }

        public int IniYear
        {
            get
            {
                return _initiationYear;
            }

            set
            {
                _initiationYear = value;
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
            _hiddenChildren = status;
        }

        public bool RecursiveSetIgnoreNode()
        {
            bool isThisIgnored = !Active;
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
                    if (R._initiationYear < L._initiationYear)
                    {
                        SwapLittles(L, R);
                        isSorted = false;
                    }
                    else if (R._initiationYear == L._initiationYear)
                    {
                        if (R._initiationTerm < L._initiationTerm)
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
            Label.Location = new Point(GetXCoord(), GetYCoord());
        }

        private void RecursiveLabelMove(Brother b, int dx, int dy)
        {
            if (b.Label.Parent != null)
            {
                b.Label.Location = new Point(b.Label.Left + dx, b.Label.Top + dy);
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
            if (b.Label.Parent != null)
            {
                b._lastPoint = e.Location;
                b.Label.BringToFront();
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
            if (b.Label.Parent != null)
            {
                if (((Brother)(b.Parent())).Label.Parent != null)
                {
                    b.Label.Visible = ((Brother)(b.Parent())).Label.Visible && !((Brother)(b.Parent()))._hiddenChildren;
                }
                else
                {
                    b.Label.Visible = !b.Label.Visible;
                }
            }
            else
            {
                b.Label.Visible = !((Brother)(b.Parent()))._hiddenChildren;
            }
            if (!b._hiddenChildren)
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
            if (Active)
            {
                Label.ForeColor = Color.White;
                Label.BackColor = Color.DarkGreen;
            }
            else
            {
                Label.ForeColor = Color.Empty;
                Label.BackColor = Color.Empty;
            }

            Brother parent = ((Brother)(this.Parent()));

            if (parent.Label.Parent != null && !parent.Label.Visible)
            {
                Label.Visible = parent.Label.Visible && !parent._hiddenChildren;
            }
        }

        private void m_Label_MouseUp(object sender, MouseEventArgs e)
        {
            if (Label != null)
            {
                Label.Capture = false;
                if (Label.Parent != null)
                {
                    Label.Parent.Invalidate();
                }
            }
            ShiftCallback();
        }

        private void m_Label_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int dx = e.X - _lastPoint.X;
                int dy = e.Y - _lastPoint.Y;
                Label.Location = new Point(Label.Left + dx, Label.Top + dy);
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
                _lastPoint = e.Location;
                Label.BringToFront();
                Label.Capture = true;
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
                int oldWidth = Label.Width;
                Label.Font = new Font(Label.Font, Label.Font.Style | FontStyle.Bold);
                Label.Refresh();
                Label.Location = new Point(Label.Location.X - (Label.Width - oldWidth) / 2, Label.Location.Y);
                if (SelectCallback != null)
                {
                    SelectCallback(this);
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