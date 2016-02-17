using System;
using System.Drawing;
using System.Windows.Forms;
using FraterniTree.Enums;
using TreeDisplay;

namespace FraterniTree
{

    /// <summary>
    ///     Represents a Fraternity Brother in a Fraternity Family Tree.
    /// </summary>
    public class Brother : LeafNode
    {
        public static Action<Brother> SelectCallback = null;
        public static Action ShiftCallback = null;

        private string lastName;
        private string firstName;

        public InitiationTerm InitiationTerm;
        public bool Active = false;
        public bool HideChildren;

        public Label Label = new Label();

        private Point lastPoint;

        public string LastName
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

        public string FirstName
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

        public int InitiationYear { get; set; }

        public override string ToString( )
        {
            if (FirstName == null)
            {
                FirstName = string.Empty;
            }

            if (LastName == null)
            {
                LastName = string.Empty;
            }

            return Util.FormatName(FirstName, LastName);
        }

        //TODO: Search  by something other then full names. If two brothers have the same full name then then it causes issues.
        public Brother FindDescendant(string fullName)
        {
            if( ToString() == fullName ) return this; 

            for ( var i = 0; i < ChildCount; i++ )
            {
                var searchResult = ((Brother) this[i]).FindDescendant( fullName );
                
                if( searchResult != null ) return searchResult; 
            }

            return null;
        }

        public bool SetIgnoreFlag( )
        {
            return SetIgnoreFlagRecursivly( !Active );
        }

        public void ClearIgnoreFlag( )
        {
            SetIgnoreFlagRecursivly( false );
        }

        private bool SetIgnoreFlagRecursivly( bool setTo )
        {
            SetIgnore( setTo );
            
            for ( var i = 0; i < ChildCount; i++ )
            {
                var child = this[i];
                if ( child == null ) continue;

                ((Brother)child).SetIgnoreFlagRecursivly( setTo );
            }

            return setTo;
        }

        public void RecalculateChildOrder( )
        {
            var isSorted = true;
            do
            {
                for ( var i = 1; i < ChildCount; i++ )
                {
                    var left = (Brother) this[i - 1];
                    var right = (Brother) this[i];

                    if( right.InitiationYear < left.InitiationYear )
                    {
                        SwapChildOrder( left, right );
                        isSorted = false;
                    }
                    else if( right.InitiationYear == left.InitiationYear )
                    {
                        if( right.InitiationTerm < left.InitiationTerm )
                        {
                            SwapChildOrder( left, right );
                            isSorted = false;
                        }
                    }
                }
            } while ( !isSorted );
        }

        private void SwapChildOrder(Brother left, Brother right)
        {
            if( !left.HasParent() ) return;
            if( !right.HasParent() ) return;
            if( (Brother) left.GetParent( true ) != (Brother) right.GetParent( true ) ) return;
            if( (Brother) left.GetParent( true ) != this ) return;
            if( !left.HasRightSibling( true ) ) return;
            if( !right.HasLeftSibling( true ) ) return;
            if( (Brother) left.GetRightSibling( true ) != right ) return;
            if( (Brother) right.GetLeftSibling( true ) != left ) return; 

            if( left == (Brother) GetFirstChild( true ) )
            {
                SetChild( right );
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
            RecalculateChildOrder();
        }

        #region Constructors

        /// <summary>
        ///     Allow a Brother to be created only by specifying the following parameters
        /// </summary>
        /// <param name="lastName">Last Name of the Brother</param>
        /// <param name="firstName">First Name of the Brother</param>
        /// <param name="term">Initiation Term of the Brother</param>
        /// <param name="year">Initiation Year of the Brother</param>
        public Brother(string lastName, string firstName, string term, int year)
        {
            LastName = lastName;
            FirstName = firstName;
            InitiationTerm = Util.StringToInitiationTerm(term);
            InitiationYear = year;

            Label.Text = ToString();
            Label.TextAlign = ContentAlignment.MiddleCenter;
            Label.Padding = new Padding( 4 );
            Label.AutoSize = true;
            Label.BorderStyle = BorderStyle.FixedSingle;
            Label.Tag = this;
            Label.MouseClick += SelectLeaf;
            Label.MouseDown += GetCurrentLeafLocation;
            Label.MouseMove += MoveLeaf;
            Label.MouseUp += DetectLeafBeingDragged;
            Label.Paint += SetLeafBackgroundColor;

            Width = Label.Width;
            Height = Label.Height;
            SetCallback( SetLeafLocation );
        }

        #endregion

        #region GUI Label Methods

        private void SetLeafLocation( )
        {
            Label.Location = new Point( HorizontalCoordinate, VerticalCoordinate );
        }

        private static void RecursiveMoveLeaf(Brother brother, int distanceInXDirection, int distanceInYDirection)
        {
            if( brother.Label.Parent != null )
            {
                brother.Label.Location = new Point( brother.Label.Left + distanceInXDirection,
                    brother.Label.Top + distanceInYDirection );
            }

            if( brother.HasRightSibling() ) 
            {
                RecursiveMoveLeaf( (Brother) brother.GetRightSibling(), distanceInXDirection, distanceInYDirection );
            }

            if( brother.HasChild() ) 
            {
                RecursiveMoveLeaf( (Brother) brother.GetFirstChild(), distanceInXDirection, distanceInYDirection );
            }
        }

        private void RecursiveCaptureLeaf(Brother brother, MouseEventArgs mouseEvent)
        {
            if( brother.Label.Parent == null ) return; 

            brother.lastPoint = mouseEvent.Location;
            brother.Label.BringToFront();

            if( brother.HasRightSibling() ) 
            {
                RecursiveCaptureLeaf( (Brother) brother.GetRightSibling(), mouseEvent );
            }

            if( brother.HasChild() )
            {
                RecursiveCaptureLeaf( (Brother) brother.GetFirstChild(), mouseEvent );
            }
        }

        public static void RecursiveToggleLeafVisible(Brother currentBrother)
        {
            var bigBrother = (Brother) currentBrother.GetParent();
            var bigBrothersLabel = bigBrother.Label;
            var currentBrothersLabel = currentBrother.Label;

            if (currentBrothersLabel.Parent == null) 
            {
                currentBrothersLabel.Visible = !bigBrother.HideChildren; 
            }
            else
            {
                if ( bigBrothersLabel.Parent == null) 
                {
                    currentBrothersLabel.Visible = !currentBrothersLabel.Visible;
                }
                else
                {
                    currentBrothersLabel.Visible = bigBrothersLabel.Visible && !bigBrother.HideChildren;
                }
            }

            if( !currentBrother.HideChildren )
            {
                for ( var i = 0; i < currentBrother.ChildCount; i++ ) 
                {
                    RecursiveToggleLeafVisible( (Brother) currentBrother[i] );
                }
            }
        }

        #region GUI Event Handlers

        private void SetLeafBackgroundColor(object sender, PaintEventArgs eventArgs)
        {
            if( Active )
            {
                Label.ForeColor = Color.White;
                Label.BackColor = Color.DarkBlue;
            }
            else
            {
                Label.ForeColor = Color.Empty;
                Label.BackColor = Color.Empty;
            }

            var parent = (Brother) GetParent();

            if( parent.Label.Parent != null && !parent.Label.Visible ) 
            {
                Label.Visible = parent.Label.Visible && !parent.HideChildren;
            }
        }

        private void DetectLeafBeingDragged(object sender, MouseEventArgs eventArgs)
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

        private void MoveLeaf(object sender, MouseEventArgs eventArgs)
        {
            if (eventArgs.Button != MouseButtons.Left) return;

            var changeInX = eventArgs.X - lastPoint.X;
            var changeInY = eventArgs.Y - lastPoint.Y;

            Label.Location = new Point( Label.Left + changeInX, Label.Top + changeInY );
            if( HasChild() )
            {
                RecursiveMoveLeaf( (Brother) GetFirstChild(), changeInX, changeInY );
            }
        }

        private void GetCurrentLeafLocation(object sender, MouseEventArgs eventArgs)
        {
            if (eventArgs.Button != MouseButtons.Left) return;

            lastPoint = eventArgs.Location;
            Label.BringToFront();
            Label.Capture = true;

            if( HasChild() )
            {
                RecursiveCaptureLeaf((Brother)GetFirstChild(), eventArgs);
            }
        }

        private void SelectLeaf(object sender, MouseEventArgs eventArgs)
        {
            if( eventArgs.Button != MouseButtons.Left ) return; 

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