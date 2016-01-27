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
        public bool Active = false;
        public Label Label = new Label();
        public static Action<Brother> SelectCallback = null;
        public static Action ShiftCallback = null;
        private bool _hiddenChildren;
        private Point _lastPoint;

        private enum InitiationTerm
        {
            Winter = 0,
            Spring = 1,
            Fall = 2
        }

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
            Label.TextAlign   = ContentAlignment.MiddleCenter;
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

        public int IniYear { get; set; }

        public override string ToString()
        {
            return First + " " + Last;
        }

        public Brother FindBrotherByName(string fullName)
        {
            Brother found = null;

            if (ToString() == fullName)
            {
                return this;
            }

            for (var i = GetNumberOfChildren() - 1; i >= 0; i--)
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
            var isThisIgnored = !Active;
            for (var i = GetNumberOfChildren() - 1; i >= 0; i--)
            {
                if (isThisIgnored)
                {
                    isThisIgnored = ((Brother)this[i]).RecursiveSetIgnoreNode();
                }
                else
                {
                    // Ignore return, but set descendant nodes accordingly
                    ((Brother)this[i]).RecursiveSetIgnoreNode();
                }
            }

            SetIgnore(isThisIgnored);
            return isThisIgnored;
        }

        public void RecursiveClearIgnoreNode()
        {
            SetIgnore(false);
            for (var i = GetNumberOfChildren() - 1; i >= 0; i--)
            {
                if (this[i] == null)
                {
                    continue;
                }
                ((Brother)this[i]).RecursiveClearIgnoreNode();
            }
        }

        public void RefreshLittleOrder()
        {
            bool isSorted;
            do
            {
                isSorted = true;
                for (var i = 1; i < GetNumberOfChildren(); i++)
                {
                    var L = (Brother)this[i - 1];
                    var R = (Brother)this[i];
                    if (R.IniYear < L.IniYear)
                    {
                        SwapLittles(L, R);
                        isSorted = false;
                    }
                    else if (R.IniYear == L.IniYear)
                    {
                        if (R._initiationTerm < L._initiationTerm)
                        {
                            SwapLittles(L, R);
                            isSorted = false;
                        }
                    }
                }
            } while (!isSorted);
        }

        private void SwapLittles(Brother left, Brother right)
        {
            if (left.HasParent(false) || right.HasParent(false))
            {
                return;
            }


            if (left.HasParent(true) && right.HasParent(true))
            {
                if ((Brother)left.Parent(true) == (Brother)right.Parent(true) && (Brother)left.Parent(true) == this)
                {
                    if (left.HasRightSibling(true) && right.HasLeftSibling(true))
                    {
                        if ((Brother)left.GetRightSibling(true) == right && (Brother)right.GetLeftSibling(true) == left)
                        {
                            if (left == (Brother)GetFirstChild(true))
                            {
                                SetFirstChild(right);
                            }

                            right.SetLeftSibling(left.GetLeftSibling(true));
                            left.SetRightSibling(right.GetRightSibling(true));

                            if (right.HasRightSibling(true))
                            {
                                ((Brother)right.GetRightSibling(true)).SetLeftSibling(left);
                            }
                            if (left.HasLeftSibling(true))
                            {
                                ((Brother)left.GetLeftSibling(true)).SetRightSibling(right);
                            }

                            left.SetLeftSibling(right);
                            right.SetRightSibling(left);
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
            Label.Location = new Point(CoordinateX, CoordinateY);
        }

        private void RecursiveLabelMove(Brother b, int dx, int dy)
        {
            if (b.Label.Parent != null)
            {
                b.Label.Location = new Point(b.Label.Left + dx, b.Label.Top + dy);
            }
            if (b.HasRightSibling())
            {
                RecursiveLabelMove((Brother)b.GetRightSibling(), dx, dy);
            }
            if (b.HasChild())
            {
                RecursiveLabelMove((Brother)b.GetFirstChild(), dx, dy);
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
                    RecursiveLabelCapture((Brother)b.GetRightSibling(), e);
                }
                if (b.HasChild())
                {
                    RecursiveLabelCapture((Brother)b.GetFirstChild(), e);
                }
            }
        }

        public void RecursiveLabelVisibleToggle(Brother b)
        {
            if (b.Label.Parent != null)
            {
                if (((Brother)b.Parent()).Label.Parent != null)
                {
                    b.Label.Visible = ((Brother)b.Parent()).Label.Visible && !((Brother)b.Parent())._hiddenChildren;
                }
                else
                {
                    b.Label.Visible = !b.Label.Visible;
                }
            }
            else
            {
                b.Label.Visible = !((Brother)b.Parent())._hiddenChildren;
            }
            if (!b._hiddenChildren)
            {
                for (var i = b.GetNumberOfChildren() - 1; i >= 0; i--)
                {
                    RecursiveLabelVisibleToggle((Brother)b[i]);
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

            var parent = (Brother)Parent();

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
                var dx = e.X - _lastPoint.X;
                var dy = e.Y - _lastPoint.Y;
                Label.Location = new Point(Label.Left + dx, Label.Top + dy);
                if (HasChild())
                {
                    RecursiveLabelMove((Brother)GetFirstChild(), dx, dy);
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
                if (HasChild())
                {
                    RecursiveLabelCapture((Brother)GetFirstChild(), e);
                }
            }
        }

        private void m_Label_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var oldWidth = Label.Width;
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