using System;

namespace TreeDisplay
{

    /// <summary>
    ///     Representation of a Graphical Node in a Tree
    /// </summary>
    public class LeafNode
    {

        public int GetGenerationsCount( )
        {
            if( !HasChild() ) return 0; 

            var generations = 0;
            for ( var i = 0; i < ChildCount; i++ )
            {
                var childObject = this[i];
                if (childObject != null)
                {
                    generations = Math.Max(generations, childObject.GetGenerationsCount());
                }
            }

            return ++generations;
        }

        #region Tree Navigation Data

        private LeafNode parent;
        private LeafNode child;
        private LeafNode leftSibling;
        private LeafNode rightSibling;

        #endregion

        /// <summary>
        ///     Number of children nodes of this Node.
        /// </summary>
        public int ChildCount { get; private set; }

        #region Node Location and Appearance Data

        public int HorizontalCoordinate { get; set; }
        public int VerticalCoordinate { get; set; }

        private int width;
        public int Width
        {
            get { return width; }

            set
            {
                if (value >= 0)
                {
                    width = value;
                }
            }
        }

        private int height;
        public int Height
        {
            get { return height; }

            set
            {
                if (value >= 0)
                {
                    height = value;
                }
            }
        }

        #endregion

        #region Implementation Specific Data

        private Action callback;
        public bool Ignored;

        #endregion

        /// <summary>
        ///     Left Neighbor to this node.
        /// </summary>
        public LeafNode LeftSibling;

        /// <summary>
        ///     Preliminary location value
        /// </summary>
        public float PreliminaryLocation;

        /// <summary>
        ///     Modifier used to determine final location
        /// </summary>
        public float Modifier;

        #region Constructors

        private LeafNode(LeafNode p,
            LeafNode o,
            LeafNode l,
            LeafNode r,
            int nC,
            int horizontalCoordinate,
            int verticalCoordinate,
            int w,
            int h,
            LeafNode prev,
            float preliminaryLocation,
            float mod)
        {
            parent = p;
            child = o;
            leftSibling = l;
            rightSibling = r;
            ChildCount = nC;
            HorizontalCoordinate = horizontalCoordinate;
            VerticalCoordinate = verticalCoordinate;
            width = w;
            height = h;
            LeftSibling = prev;
            PreliminaryLocation = preliminaryLocation;
            Modifier = mod;
        }

        protected LeafNode( ) : this( null, null, null, null, 0, 0, 0, 0, 0, null, 0, 0 ) {}

        #endregion

        #region Tree Navigation and Manipulation Methods

        public LeafNode GetFirstChild(bool isIndex = false)
        {
            if( isIndex ) return child; 
            if( child == null ) return null;
            if (!child.Ignored) return child; 

            var unignoredChild = child; 
            while ( unignoredChild.HasRightSibling() )
            {
                unignoredChild = unignoredChild.GetRightSibling();

                if( !unignoredChild.Ignored ) return unignoredChild;
            }

            return null;
        }

        protected void SetChild(LeafNode newChild)
        {
            child = newChild;
        }

        protected void AddChild(LeafNode newChild)
        {
            if( newChild.HasParent() )
            {
                newChild.GetParent().RemoveChild( newChild ); 
            }

            if( IsLeaf() )
            {
                SetChild( newChild );
                newChild.SetLeftSibling( null );
            }
            else
            {
                var siblingIter = child;
                while ( siblingIter != null && siblingIter.HasRightSibling() )
                {
                    siblingIter = siblingIter.GetRightSibling();
                }

                if( siblingIter != null )
                {
                    siblingIter.SetRightSibling( newChild );
                    newChild.SetLeftSibling( siblingIter );
                }
            }

            newChild.SetParent( this );
            ChildCount++;
        }

        public void RemoveNode( )
        {
            for ( var i = 0; i < ChildCount; i++ )
            {
                var childObject = this[i];

                if ( childObject == null ) continue;

                GetParent().AddChild( childObject );
            }

            GetParent().RemoveChild( this );
        }

        private void RemoveChild(LeafNode toBeRemoved)
        {
            if( toBeRemoved == null ) return; 
            
            if( toBeRemoved == GetFirstChild() )
            {
                child = toBeRemoved.GetRightSibling();
                if( child != null )
                {
                    child.SetLeftSibling( null );
                }
            }
            else
            {
                var siblingIter = child;
                while ( siblingIter != null && siblingIter != toBeRemoved ) {
                    siblingIter = siblingIter.GetRightSibling();
                }

                if( siblingIter == toBeRemoved )
                {
                    if( toBeRemoved.HasLeftSibling() ) 
                    {
                        toBeRemoved.GetLeftSibling().SetRightSibling( toBeRemoved.GetRightSibling() );
                    }

                    if( toBeRemoved.HasRightSibling() )
                    {
                        toBeRemoved.GetRightSibling().SetLeftSibling( toBeRemoved.GetLeftSibling() );
                    }
                }
            }

            toBeRemoved.SetParent( null );
            toBeRemoved.SetLeftSibling( null );
            toBeRemoved.SetRightSibling( null );
            ChildCount--;
        }

        protected void SetLeftSibling(LeafNode sibling)
        {
            leftSibling = sibling;
        }

        public LeafNode GetLeftSibling(bool returnValueNoMatterWhat = false)
        {
            if( returnValueNoMatterWhat ) return leftSibling;
            if( leftSibling == null ) return null;
            if( !leftSibling.Ignored ) return leftSibling; 

            var leftmostSibling = leftSibling;
            while ( leftmostSibling.HasLeftSibling() )
            {
                leftmostSibling = leftmostSibling.GetLeftSibling();

                if( !leftmostSibling.Ignored ) return leftmostSibling; 
            }

            return null;
        }

        protected void SetRightSibling(LeafNode sibling)
        {
            rightSibling = sibling;
        }

        public LeafNode GetRightSibling(bool returnValueNoMatterWhat = false)
        {
            if( returnValueNoMatterWhat ) return rightSibling;
            if( rightSibling == null ) return null;
            if( !rightSibling.Ignored ) return rightSibling;

            var rightmostSibling = rightSibling;
            while (rightmostSibling.HasRightSibling())
            {
                rightmostSibling = rightmostSibling.GetRightSibling();

                if (!rightmostSibling.Ignored) return rightmostSibling; 
            }

            return null;
        }

        public LeafNode GetParent(bool returnValueNoMatterWhat = false)
        {
            if( returnValueNoMatterWhat ) return parent;
            if( parent == null ) return null;
            if( parent.Ignored ) return null;

            return parent;
        }

        private void SetParent(LeafNode newParent)
        {
            parent = newParent;
        }

        public LeafNode LeftNeighbor( )
        {
            var unignoredLeftNeighbor = LeftSibling;

            if( unignoredLeftNeighbor == null ) return null;
            if( !unignoredLeftNeighbor.Ignored ) return LeftSibling; 

            while ( unignoredLeftNeighbor.LeftNeighbor() != null )
            {
                unignoredLeftNeighbor = unignoredLeftNeighbor.LeftNeighbor();

                if( !unignoredLeftNeighbor.Ignored ) return unignoredLeftNeighbor; 
            }

            return null;
        }

        #endregion

        #region Check Associated Nodes

        public bool IsLeaf( )
        {
            if( child == null ) return true; 

            for ( var i = 0; i < ChildCount; i++ )
            {
                var childObject = this[i];

                if ( childObject == null ) continue;
                if ( childObject.Ignored ) continue; 

                return false;
            }

            return true;
        }

        public bool HasParent(bool returnValueNoMatterWhat = false)
        {
            if( parent == null ) return false;
            if( returnValueNoMatterWhat ) return true; 

            return !parent.Ignored;
        }

        public bool HasChild( )
        {
            if( child == null ) return false;

            for ( var i = 0; i < ChildCount; i++ )
            {
                var childItr = this[i];

                if( childItr == null ) continue;
                if( childItr.Ignored ) continue;

                return true;
            }

            return false;
        }

        public bool HasLeftSibling(bool returnValueNoMatterWhat = false)
        {
            if( leftSibling == null ) return false;
            if( returnValueNoMatterWhat ) return true;

            var tempLeftSib = leftSibling;
            if( !tempLeftSib.Ignored ) return true; 

            while ( tempLeftSib.Ignored )
            {
                if( tempLeftSib.HasLeftSibling() ) 
                {
                    tempLeftSib = tempLeftSib.GetLeftSibling();
                }
                else
                {
                    return false;
                }
            }

            return true; 
        }

        public bool HasRightSibling(bool returnValueNoMatterWhat = false)
        {
            if( rightSibling == null ) return false;
            if( returnValueNoMatterWhat ) return true;

            var tempRightSib = rightSibling;
            if ( !tempRightSib.Ignored ) return true; 

            while ( tempRightSib.Ignored )
            {
                if( tempRightSib.HasRightSibling() ) 
                {
                    tempRightSib = tempRightSib.GetRightSibling();
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Implementation Specific

        protected void SetCallback(Action newCallback)
        {
            callback = newCallback;
        }

        public void ExecuteCallback( )
        {
            callback();

            for ( var i = 0; i < ChildCount; i++ )
            {
                var childItr = this[i];
                if( childItr == null ) continue;
                this[i].ExecuteCallback();
            }
        }

        protected void SetIgnore(bool b)
        {
            Ignored = b;
        }

        #endregion

        #region Children Indexing

        public int GetChildIndex(LeafNode searchingFor)
        {
            var childIter = child;
            int index;

            for (index = 0; index < ChildCount; index++)
            {
                if (childIter == null) return -1;
                if (childIter == searchingFor) break;

                childIter = childIter.GetRightSibling();
            }

            return index;
        }

        public LeafNode this[int index]
        {
            get
            {
                if (index > ChildCount - 1 || index < 0) return null;

                var rightMostChildWithinIndex = child;

                for ( var i = 0; i < index; i++ )
                {
                    if( rightMostChildWithinIndex == null ) break;

                    rightMostChildWithinIndex = rightMostChildWithinIndex.GetRightSibling( true );
                }

                return rightMostChildWithinIndex;
            }
        }

        #endregion
    }

}