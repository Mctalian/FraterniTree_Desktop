using System;
using System.Drawing;
using System.Windows.Forms;
using TreeDisplay;

namespace FraterniTree
{

    /// <summary>
    ///     Represents a Fraternity Brother in a Fraternity Family Tree.
    /// </summary>
    public class Brother : Node
    {
        public static Action<Brother> SelectCallback = null;
        public static Action ShiftCallback = null;
        public bool Active = false;
        private string firstName;
        private bool hiddenChildren;
        private InitiationTerm initiationTerm;
        public Label Label = new Label();

        private string lastName;
        private Point lastPoint;

        public string Last
        {
            get
            {
                return lastName;
            }

            set
            {
                lastName = value;
                Label.Text = ToString();
            }
        }

        public string First
        {
            get
            {
                return firstName;
            }

            set
            {
                firstName = value;
                Label.Text = ToString();
            }
        }

        public string IniMonth
        {
            get
            {
                return initiationTerm.ToString();
            }

            set
            {
                initiationTerm = (InitiationTerm) Enum.Parse( typeof (InitiationTerm), value );
            }
        }

        public int IniYear { get; set; }

        public override string ToString( )
        {
            return First + " " + Last;
        }

        public Brother FindBrotherByName(string fullName)
        {
            if( ToString() == fullName ) { return this; }

            for ( var i = NumberOfChildren - 1; i >= 0; i-- )
            {
                var found = ((Brother) this[i]).FindBrotherByName( fullName );
                if( found != null ) { return found; }
            }

            return null;
        }

        public void SetDescendantsHidden(bool status)
        {
            hiddenChildren = status;
        }

        public bool RecursiveSetIgnoreNode( )
        {
            var isThisIgnored = !Active;
            for ( var i = NumberOfChildren - 1; i >= 0; i-- )
            {
                if( isThisIgnored ) 
                {
                    isThisIgnored = ((Brother) this[i]).RecursiveSetIgnoreNode();
                }
                else
                {
                    // Ignore return, but set descendant nodes accordingly //TODO
                    ((Brother) this[i]).RecursiveSetIgnoreNode();
                }
            }

            SetIgnore( isThisIgnored );
            return isThisIgnored;
        }

        public void RecursiveClearIgnoreNode( )
        {
            SetIgnore( false );
            for ( var i = NumberOfChildren - 1; i >= 0; i-- )
            {
                if( this[i] == null ) continue; 

                ((Brother) this[i]).RecursiveClearIgnoreNode();
            }
        }

        public void RefreshLittleOrder( )
        {
            bool isSorted;
            do
            {
                isSorted = true;
                for ( var i = 1; i < NumberOfChildren; i++ )
                {
                    var left = (Brother) this[i - 1];
                    var right = (Brother) this[i];

                    if( right.IniYear < left.IniYear )
                    {
                        SwapLittles( left, right );
                        isSorted = false;
                    }
                    else if( right.IniYear == left.IniYear )
                    {
                        if( right.initiationTerm < left.initiationTerm )
                        {
                            SwapLittles( left, right );
                            isSorted = false;
                        }
                    }
                }
            } while ( !isSorted );
        }

        private void SwapLittles(Brother left, Brother right)
        {
            if( !left.HasParent() ) return;
            if( !right.HasParent() ) return;
            if( (Brother) left.Parent( true ) != (Brother) right.Parent( true ) ) return;
            if( (Brother) left.Parent( true ) != this ) return;
            if( !left.HasRightSibling( true ) ) return;
            if( !right.HasLeftSibling( true ) ) return;
            if( (Brother) left.GetRightSibling( true ) != right ) return;
            if( (Brother) right.GetLeftSibling( true ) != left ) return; 

            if( left == (Brother) GetFirstChild( true ) )
            {
                SetFirstChild( right );
            }

            right.SetLeftSibling( left.GetLeftSibling( true ) );
            left.SetRightSibling( right.GetRightSibling( true ) );

            if( right.HasRightSibling( true ) )
            {
                ((Brother) right.GetRightSibling( true )).SetLeftSibling( left );
            }

            if( left.HasLeftSibling( true ) )
            {
                ((Brother) left.GetLeftSibling( true )).SetRightSibling( right );
            }

            left.SetLeftSibling( right );
            right.SetRightSibling( left );
        }

        public void AddChild(Brother child)
        {
            base.AddChild( child );
            RefreshLittleOrder();
        }

        private enum InitiationTerm
        {
            Winter = 0,
            Spring = 1,
            Fall = 2
        }

        #region Constructors

        private Brother( ) {}

        /// <summary>
        ///     Allow a Brother to be created only by specifying the following parameters
        /// </summary>
        /// <param name="strLast">Last Name of the Brother</param>
        /// <param name="strFirst">First Name of the Brother</param>
        /// <param name="month">Initiation Term of the Brother</param>
        /// <param name="iYear">Initiation Year of the Brother</param>
        public Brother(string lastName, string firstName, string month, int year)
        {
            // Initialize Brother object
            Last = lastName;
            First = firstName;
            IniMonth = month;
            IniYear = year;

            // Initialize the label
            Label.Text = ToString();
            Label.TextAlign = ContentAlignment.MiddleCenter;
            Label.Padding = new Padding( 4 );
            Label.AutoSize = true;
            Label.BorderStyle = BorderStyle.FixedSingle;
            Label.Tag = this;
            Label.MouseClick += m_Label_MouseClick;
            Label.MouseDown += m_Label_MouseDown;
            Label.MouseMove += m_Label_MouseMove;
            Label.MouseUp += m_Label_MouseUp;
            Label.Paint += m_Label_Paint;

            SetWidth( Label.Width );
            SetHeight( Label.Height );
            SetCallback( ApplyNodeLocationsToLabel );
        }

        #endregion

        #region GUI Label Methods

        private void ApplyNodeLocationsToLabel( )
        {
            Label.Location = new Point( CoordinateX, CoordinateY );
        }

        private static void RecursiveLabelMove(Brother brother, int distanceInXDirection, int distanceInYDirection)
        {
            if( brother.Label.Parent != null )
            {
                brother.Label.Location = new Point( brother.Label.Left + distanceInXDirection,
                    brother.Label.Top + distanceInYDirection );
            }

            if( brother.HasRightSibling() ) {
                RecursiveLabelMove( (Brother) brother.GetRightSibling(), distanceInXDirection, distanceInYDirection );
            }

            if( brother.HasChild() ) {
                RecursiveLabelMove( (Brother) brother.GetFirstChild(), distanceInXDirection, distanceInYDirection );
            }
        }

        private void RecursiveLabelCapture(Brother brother, MouseEventArgs mouseEvent)
        {
            if( brother.Label.Parent == null ) return; 

            brother.lastPoint = mouseEvent.Location;
            brother.Label.BringToFront();

            if( brother.HasRightSibling() ) {
                RecursiveLabelCapture( (Brother) brother.GetRightSibling(), mouseEvent );
            }

            if( brother.HasChild() )
            {
                RecursiveLabelCapture( (Brother) brother.GetFirstChild(), mouseEvent );
            }
        }

        public void RecursiveLabelVisibleToggle(Brother brother)
        {
            if( brother.Label.Parent == null ) {
                brother.Label.Visible = !((Brother) brother.Parent()).hiddenChildren;
            }
            else
            {
                if( ((Brother) brother.Parent()).Label.Parent == null ) {
                    brother.Label.Visible = !brother.Label.Visible;
                }
                else
                {
                    brother.Label.Visible = ((Brother) brother.Parent()).Label.Visible && !((Brother) brother.Parent()).hiddenChildren;
                }
            }

            if( !brother.hiddenChildren )
            {
                for ( var i = brother.NumberOfChildren - 1; i >= 0; i-- ) {
                    RecursiveLabelVisibleToggle( (Brother) brother[i] );
                }
            }
        }

        #region GUI Event Handlers

        private void m_Label_Paint(object sender, PaintEventArgs e)
        {
            if( Active )
            {
                Label.ForeColor = Color.White; //TODO - Localize
                Label.BackColor = Color.DarkGreen;
            }
            else
            {
                Label.ForeColor = Color.Empty;
                Label.BackColor = Color.Empty;
            }

            var parent = (Brother) Parent();

            if( parent.Label.Parent != null && !parent.Label.Visible ) {
                Label.Visible = parent.Label.Visible && !parent.hiddenChildren;
            }
        }

        private void m_Label_MouseUp(object sender, MouseEventArgs e)
        {
            if( Label != null )
            {
                Label.Capture = false;
                if( Label.Parent != null )
                {
                    Label.Parent.Invalidate();
                }
            }

            ShiftCallback();
        }

        private void m_Label_MouseMove(object sender, MouseEventArgs e)
        {
            if( e.Button != MouseButtons.Left ) return; 

            var dx = e.X - lastPoint.X;
            var dy = e.Y - lastPoint.Y;
            Label.Location = new Point( Label.Left + dx, Label.Top + dy );
            if( HasChild() )
            {
                RecursiveLabelMove( (Brother) GetFirstChild(), dx, dy );
            }
        }

        private void m_Label_MouseDown(object sender, MouseEventArgs e) //TODO - Name
        {
            if( e.Button != MouseButtons.Left ) return; 

            lastPoint = e.Location;
            Label.BringToFront();
            Label.Capture = true;

            if( HasChild() )
            {
                RecursiveLabelCapture( (Brother) GetFirstChild(), e );
            }
        }

        private void m_Label_MouseClick(object sender, MouseEventArgs e)
        {
            if( e.Button != MouseButtons.Left ) return; 

            var oldWidth = Label.Width;
            Label.Font = new Font( Label.Font, Label.Font.Style | FontStyle.Bold );
            Label.Refresh();
            Label.Location = new Point( Label.Location.X - (Label.Width - oldWidth)/2, Label.Location.Y );

            if (SelectCallback == null) throw new NotImplementedException();
            SelectCallback( this );
        }

        #endregion

        #endregion
    }

}