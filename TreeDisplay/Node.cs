using System;

namespace TreeDisplay
{

    /// <summary>
    ///     Representation of a Node in a Tree
    /// </summary>
    public class Node
    {
        #region Descendent Counting

        public int GetNumGenerations( )
        {
            if( !HasChild() ) return 0; 

            var gens = 0;
            for ( var i = 0; i < DirectChildCount; i++ ) {
                if( this[i] != null )
                {
                    gens = Math.Max( gens, this[i].GetNumGenerations() );
                }
            }

            return ++gens;
        }

        #endregion

        #region Private Data

        #region Tree Navigation Data

        private Node parent;
        private Node offspring;
        private Node leftSibling;
        private Node rightSibling;

        #endregion

        #region Descendant Data

        /// <summary>
        ///     Number of children nodes of this Node.
        /// </summary>
        public int DirectChildCount { get; private set; }

        #endregion

        #region Node Location and Appearance Data

        /// <summary>
        ///     Node X Coordinate
        /// </summary>
        public int CoordinateX { get; set; }

        /// <summary>
        ///     Node Y Coordinate
        /// </summary>
        public int CoordinateY { get; set; }

        private int width;
        private int height;

        #endregion

        #region Implementation Specific Data

        private Action callback;
        private bool ignoreNode;

        #endregion

        #endregion

        #region Public Data

        /// <summary>
        ///     Left Neighbor to this node.
        /// </summary>
        public Node Prev;

        /// <summary>
        ///     Preliminary location value
        /// </summary>
        public float Prelim;

        /// <summary>
        ///     Modifier used to determine final location
        /// </summary>
        public float Modifier;

        #endregion

        #region Constructors

        private Node(Node p,
            Node o,
            Node l,
            Node r,
            int nC,
            int coordinateX,
            int coordinateY,
            int w,
            int h,
            Node prev,
            float prelim,
            float mod)
        {
            parent = p;
            offspring = o;
            leftSibling = l;
            rightSibling = r;
            DirectChildCount = nC;
            CoordinateX = coordinateX;
            CoordinateY = coordinateY;
            width = w;
            height = h;
            Prev = prev;
            Prelim = prelim;
            Modifier = mod;
        }

        protected Node( ) : this( null, null, null, null, 0, 0, 0, 0, 0, null, 0, 0 ) {}

        #endregion

        #region Tree Navigation and Manipulation Methods

        public Node GetFirstChild(bool isIndex = false)
        {
            if( isIndex ) return offspring; 
            if( offspring == null ) return null; 
            if( !offspring.IsIgnored() ) return offspring; 

            var unignoredChild = offspring; 
            while ( unignoredChild.HasRightSibling() )
            {
                unignoredChild = unignoredChild.GetRightSibling();

                if( !unignoredChild.IsIgnored() ) return unignoredChild;
            }

            return null;
        }

        protected void SetFirstChild(Node child)
        {
            offspring = child;
        }

        protected void AddChild(Node child)
        {
            if( child.HasParent() )
            {
                child.Parent().RemoveChild( child ); 
            }

            if( IsLeaf() )
            {
                SetFirstChild( child );
                child.SetLeftSibling( null );
            }
            else
            {
                Node siblingIter;
                for ( siblingIter = offspring; siblingIter != null && siblingIter.HasRightSibling();
                                                    siblingIter = siblingIter.GetRightSibling() )
                {
                    // Nothing to do here
                    // TODO - why are we looping for no reason?
                }

                if( siblingIter != null )
                {
                    siblingIter.SetRightSibling( child );
                    child.SetLeftSibling( siblingIter );
                }
            }

            child.Parent( this );
            DirectChildCount++;
        }

        public void RemoveNode( )
        {
            for ( var i = DirectChildCount - 1; i >= 0; i-- )
            {
                if( this[i] == null ) continue; 

                Parent().AddChild( this[i] );
            }

            Parent().RemoveChild( this );
        }

        private void RemoveChild(Node child)
        {
            if( child == GetFirstChild() )
            {
                offspring = child.GetRightSibling();
                if( offspring != null )
                {
                    offspring.SetLeftSibling( null );
                }
            }
            else
            {
                Node siblingIter;
                for ( siblingIter = offspring; siblingIter != child && siblingIter != null; siblingIter = siblingIter.GetRightSibling() )
                {
                    // Nothing to do here
                    // TODO
                }

                if( siblingIter == child )
                {
                    if( child.HasLeftSibling() ) 
                    {
                        child.GetLeftSibling().SetRightSibling( child.GetRightSibling() );
                    }

                    if( child.HasRightSibling() )
                    {
                        child.GetRightSibling().SetLeftSibling( child.GetLeftSibling() );
                    }
                }
            }

            child.Parent( null );
            child.SetLeftSibling( null );
            child.SetRightSibling( null );
            DirectChildCount--;
        }

        public Node GetLeftSibling(bool isIndex = false)
        {
            if( leftSibling == null ) return null;
            if( isIndex ) return leftSibling;
            if( !leftSibling.IsIgnored() ) return leftSibling; 

            var leftmostSibling = leftSibling;
            while ( leftmostSibling.HasLeftSibling() )
            {
                leftmostSibling = leftmostSibling.GetLeftSibling();

                if( !leftmostSibling.IsIgnored() ) return leftmostSibling; 
            }

            return null;
        }

        public Node GetRightSibling(bool isIndex = false)
        {
            if( rightSibling == null ) return null;
            if( isIndex ) return rightSibling;
            if( !rightSibling.IsIgnored() ) return rightSibling;

            var rightmostSibling = rightSibling;
            while (rightmostSibling.HasRightSibling())
            {
                rightmostSibling = rightmostSibling.GetRightSibling();

                if (!rightmostSibling.IsIgnored()) return rightmostSibling; 
            }

            return null;
        }

        protected void SetLeftSibling(Node sibling)
        {
            leftSibling = sibling;
        }

        protected void SetRightSibling(Node sibling)
        {
            rightSibling = sibling;
        }

        public Node Parent(bool isIndex = false)
        {
            if( isIndex ) return parent;
            if( parent == null ) return null; 
            if( !parent.IsIgnored() ) return parent; 

            return null;
        }

        private void Parent(Node newParent)
        {
            parent = newParent;
        }

        public Node LeftNeighbor( )
        {
            var unignoredLeftNeighbor = Prev;

            if( unignoredLeftNeighbor == null ) return null;
            if( !unignoredLeftNeighbor.IsIgnored() ) return Prev; 

            while ( unignoredLeftNeighbor.LeftNeighbor() != null )
            {
                unignoredLeftNeighbor = unignoredLeftNeighbor.LeftNeighbor();

                if( !unignoredLeftNeighbor.IsIgnored() ) return unignoredLeftNeighbor; 
            }

            return null;
        }

        #endregion

        #region Check Associated Nodes

        public bool IsLeaf( )
        {
            if( offspring == null ) return true; 

            for ( var i = DirectChildCount - 1; i >= 0; i-- )
            {
                if( this[i] == null ) continue; 
                if( this[i].IsIgnored() ) continue; 

                return false;
            }

            return true;
        }

        public bool HasParent(bool isIndex = false)
        {
            if( parent == null ) return false;
            if( isIndex ) return true; 

            return !parent.IsIgnored();
        }

        public bool HasChild( )
        {
            if( offspring == null ) return false;

            for ( var i = DirectChildCount - 1; i >= 0; i-- )
            {
                if( this[i] == null || this[i].IsIgnored() ) continue;

                return true;
            }

            return false;
        }

        public bool HasLeftSibling(bool isIndex = false)
        {
            if( leftSibling == null ) return false; 

            var tempLeftSib = leftSibling;
            if( !tempLeftSib.IsIgnored() || isIndex ) return true; 

            while ( tempLeftSib.IsIgnored() )
            {
                if( tempLeftSib.HasLeftSibling() ) {
                    tempLeftSib = tempLeftSib.GetLeftSibling();
                }
                else
                {
                    return false;
                }
            }

            return true; //TODO
        }

        public bool HasRightSibling(bool isIndex = false)
        {
            if( rightSibling == null ) return false;

            var tempRightSib = rightSibling;
            if( !tempRightSib.IsIgnored() || isIndex ) return true; 

            while ( tempRightSib.IsIgnored() )
            {
                if( tempRightSib.HasRightSibling() ) {
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
            for ( var i = 0; i < DirectChildCount; i++ )
            {
                if( this[i] != null )
                {
                    this[i].ExecuteCallback();
                }
            }
        }

        protected void SetIgnore(bool b)
        {
            ignoreNode = b;
        }

        public bool IsIgnored( )
        {
            return ignoreNode;
        }

        #endregion

        #region Node Sizing and Appearance

        public int GetWidth( )
        {
            return width;
        }

        public void SetWidth(int w)
        {
            if( w >= 0 )
            {
                width = w;
            }
        }

        public int GetHeight( )
        {
            return height;
        }

        protected void SetHeight(int h)
        {
            if( h >= 0 ) 
            {
                height = h;
            }
        }

        #endregion

        #region Children Indexing

        public int GetChildIndex(Node child)
        {
            Node childIter;
            uint i;
            for ( childIter = offspring, i = 0; childIter != child && childIter != null; childIter = childIter.GetRightSibling(), i++ )
            {
                
            }

            return childIter == null 
                ? -1 
                : (int) i;
        }

        public Node this[int ind]
        {
            get
            {
                if( ind > DirectChildCount - 1 || ind < 0 ) return null;

                Node childIter;
                uint i;
                for ( childIter = offspring, i = 0; childIter != null && i < ind; childIter = childIter.GetRightSibling( true ), i++ )
                {
                    // Nothing to do here //TODO
                }
                return childIter;
            }
        }

        #endregion
    }

}