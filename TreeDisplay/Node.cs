using System;

namespace TreeDisplay
{
    /// <summary>
    /// Representation of a Node in a Tree
    /// </summary>
    public class Node
    {
        
        #region Private Data

        #region Tree Navigation Data

        /// <summary>
        /// Parent Node of this Node.
        /// </summary>
        /// 
        private Node _parent;

        /// <summary>
        /// First Child Node of this Node.
        /// </summary>
        private Node _offspring;

        /// <summary>
        /// Left Sibling Node of this Node.
        /// </summary>
        private Node _leftSibling;

        /// <summary>
        /// Right Sibling Node of this Node.
        /// </summary>
        private Node _rightSibling;

        #endregion

        #region Descendant Data

        /// <summary>
        /// Number of children nodes of this Node.
        /// </summary>
        public int NumberOfChildren { get; private set; }

        #endregion

        #region Node Location and Appearance Data

        /// <summary>
        /// Node X Coordinate
        /// </summary>
        public int CoordinateX { get; set; } 

        /// <summary>
        /// Node Y Coordinate
        /// </summary>
        public int CoordinateY { get; set; }

        /// <summary>
        /// Node Width
        /// </summary>
        private int _width;

        /// <summary>
        /// Node Height
        /// </summary>
        private int _height;

        #endregion

        #region Implementation Specific Data
        /// <summary>
        /// Callback as needed based on user implementation
        /// </summary>
        private Action _callback;

        /// <summary>
        /// Ignore this node and consequently all descendants.
        /// </summary>
        private bool _ignoreNode;

        #endregion

        #endregion

        #region Public Data

        /// <summary>
        /// Left Neighbor to this node.
        /// </summary>
        public Node Prev;

        /// <summary>
        /// Preliminary location value
        /// </summary>
        public float Prelim;

        /// <summary>
        /// Modifier used to determine final location
        /// </summary>
        public float Modifier;

        #endregion

        #region Constructors

        public Node(Node p, Node o, Node l, Node r,
                    int nC, int coordinateX, int coordinateY, int w, int h,
                    Node prev, float prelim, float mod)
        {
            _parent = p;
            _offspring = o;
            _leftSibling = l;
            _rightSibling = r;
            NumberOfChildren = nC;
            CoordinateX = coordinateX;
            CoordinateY = coordinateY;
            _width = w;
            _height = h;
            Prev = prev;
            Prelim = prelim;
            Modifier = mod;
        }

        public Node() : this(null, null, null, null, 0, 0, 0, 0, 0, null, 0, 0)
        {
        }

        public Node(Node duplicate)
        {
            //TODO
        }

        #endregion

        #region Tree Navigation and Manipulation Methods

        public Node GetFirstChild(bool isIndex = false)
        {
            if (_offspring == null)
            {
                return null;
            }

            if (!_offspring.IsIgnored() || isIndex)
            {
                return _offspring;
            }

            var unignoredChild = _offspring;//TODO
            while (unignoredChild.HasRightSibling())
            {
                unignoredChild = unignoredChild.GetRightSibling();

                if (!unignoredChild.IsIgnored())
                {
                    return unignoredChild;
                }
            }
            return null;
        }

        protected void SetFirstChild(Node child)
        {
            _offspring = child;
        }

        public void AddChild(Node child)
        {
            if (child.HasParent())
            {
                child.Parent().RemoveChild(child);
            }

            if (IsLeaf())
            {
                SetFirstChild(child);
                child.SetLeftSibling(null);
            }
            else
            {
                Node siblingIter;
                for (siblingIter = _offspring; siblingIter != null && siblingIter.HasRightSibling(); siblingIter = siblingIter.GetRightSibling())
                {
                    // Nothing to do here
                    // TODO - why are we looping for no reason?
                }
                if (siblingIter != null)
                {
                    siblingIter.SetRightSibling(child);
                    child.SetLeftSibling(siblingIter);
                }
            }

            child.Parent(this);
            NumberOfChildren++;
        }

        public void RemoveNode()
        {
            for (var i = NumberOfChildren - 1; i >= 0; i--)
            {
                if (this[i] == null)
                {
                    continue;
                }

                Parent().AddChild(this[i]);
            }

            Parent().RemoveChild(this);
        }

        protected void RemoveChild(Node child)
        {
            if (child == GetFirstChild())
            {
                _offspring = child.GetRightSibling();
                if (_offspring != null)
                {
                    _offspring.SetLeftSibling(null);
                }
            }
            else
            {
                Node siblingIter;
                for (siblingIter = _offspring; siblingIter != child && siblingIter != null; siblingIter = siblingIter.GetRightSibling())
                {
                    // Nothing to do here
                    // TODO
                }
                if (siblingIter == child)
                {
                    if (child.HasLeftSibling()) //TODO
                    {
                        child.GetLeftSibling().SetRightSibling(child.GetRightSibling());
                    }
                    if (child.HasRightSibling())
                    {
                        child.GetRightSibling().SetLeftSibling(child.GetLeftSibling());
                    }
                }
            }

            child.Parent(null);
            child.SetLeftSibling(null);
            child.SetRightSibling(null);
            NumberOfChildren--;
        }

        public Node GetLeftSibling(bool isIndex = false) //TODO can this be merged with right?
        {
            if (_leftSibling == null)
            {
                return null;
            }

            if (!_leftSibling.IsIgnored() || isIndex)
            {
                return _leftSibling;
            }

            var unignoredLeftSib = _leftSibling;
            while (unignoredLeftSib.HasLeftSibling())
            {
                unignoredLeftSib = unignoredLeftSib.GetLeftSibling();

                if (!unignoredLeftSib.IsIgnored())
                {
                    return unignoredLeftSib;
                }
            }
            return null;
        }

        protected void SetLeftSibling(Node sibling)
        {
            _leftSibling = sibling;
        }

        public Node GetRightSibling(bool isIndex = false)
        {
            if (_rightSibling == null)
            {
                return null;
            }

            if (!_rightSibling.IsIgnored() || isIndex)
            {
                return _rightSibling;
            }

            var unignoredRightSib = _rightSibling; //TODO change name
            while (unignoredRightSib.HasRightSibling())
            {
                unignoredRightSib = unignoredRightSib.GetRightSibling();

                if (!unignoredRightSib.IsIgnored())
                {
                    return unignoredRightSib;
                }
            }
            return null;
        }

        protected void SetRightSibling(Node sibling)
        {
            _rightSibling = sibling;
        }

        public Node Parent(bool isIndex = false)
        {
            if (_parent == null)
            {
                return null;
            }

            if (!_parent.IsIgnored() || isIndex)
            {
                return _parent;
            }

            return null;
            //TODO two return null
        }

        protected void Parent(Node parent)
        {
            _parent = parent;
        }

        public Node LeftNeighbor()
        {
            var unignoredLeftNeighbor = Prev;
            if (unignoredLeftNeighbor == null)
            {
                return null;
            }

            if (!unignoredLeftNeighbor.IsIgnored())
            {
                return Prev;
            }
            
            while (unignoredLeftNeighbor.LeftNeighbor() != null)
            {
                unignoredLeftNeighbor = unignoredLeftNeighbor.LeftNeighbor();

                if (!unignoredLeftNeighbor.IsIgnored())
                {
                    return unignoredLeftNeighbor;
                }
            }
            return null;
        }

        #endregion

        #region Check Associated Nodes

        public bool IsLeaf()
        {
            if (_offspring == null)
            {
                return true;
            }

            for (var i = NumberOfChildren - 1; i >= 0; i--)
            {
                if (this[i] == null)
                {
                    continue;
                }

                if (this[i].IsIgnored())
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        public bool HasParent(bool isIndex = false)
        {
            if (_parent == null)
            {
                return false;
            }

            if (isIndex)
            {
                return true;
            }

            return !_parent.IsIgnored();
        }

        public bool HasChild()
        {
            if (_offspring != null)
            {
                for (var i = NumberOfChildren - 1; i >= 0; i--)
                {
                    if (this[i] == null || this[i].IsIgnored())
                    {
                        continue;
                    }

                    return true;
                }
                return false;
            }

            return false;
        }

        public bool HasLeftSibling(bool isIndex = false)
        {
            if (_leftSibling != null)
            {
                var tempLeftSib = _leftSibling;
                if (!tempLeftSib.IsIgnored() || isIndex)
                {
                    return true;
                }

                while (tempLeftSib.IsIgnored())
                {
                    if (tempLeftSib.HasLeftSibling())
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

            return false;
        }

        public bool HasRightSibling(bool isIndex = false)
        {
            if (_rightSibling != null)
            {
                var tempRightSib = _rightSibling;
                if (!tempRightSib.IsIgnored() || isIndex)
                {
                    return true;
                }

                while (tempRightSib.IsIgnored())
                {
                    if (tempRightSib.HasRightSibling())
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

            return false;
        }

        #endregion

        #region Implementation Specific

        public void SetCallback(Action cb)
        {
            _callback = cb;
        }

        public void ExecuteCallback()
        {
            _callback();
            for (var i = 0; i < NumberOfChildren; i++)
            {
                if (this[i] != null)
                {
                    this[i].ExecuteCallback();
                }
            }
        }

        public void SetIgnore(bool b)
        {
            _ignoreNode = b;
        }

        public bool IsIgnored()
        {
            return _ignoreNode;
        }

        #endregion

        #region Node Sizing and Appearance

        public int GetWidth()
        {
            return _width;
        }

        public void SetWidth(int w)
        {
            if (w >= 0)
            {
                _width = w;
            }
        }

        public int GetHeight()
        {
            return _height;
        }

        public void SetHeight(int h)
        {
            if (h >= 0)
            {
                _height = h;
            }
        }

        #endregion

        #region Descendent Counting

        public int GetNumGenerations()
        {
            if (!HasChild())
            {
                return 0;
            }

            var gens = 0;
            for (var i = 0; i < NumberOfChildren; i++)
            {
                if (this[i] != null)
                {
                    gens = Math.Max(gens, this[i].GetNumGenerations());
                }
            }

            return gens + 1;
        }

        #endregion

        #region Children Indexing

        public int GetChildIndex(Node child)
        {
            Node childIter;
            uint i;
            for (childIter = _offspring, i = 0; //TODO determine if this does anything
                 childIter != child && childIter != null;
                 childIter = childIter.GetRightSibling(), i++)
            {
            }
            
            if (childIter != null)
            {
                return (int)i;
            }

            return -1;
        }

        public Node this[int ind]
        {
            get
            {
                if (ind > NumberOfChildren - 1 || ind < 0)
                {
                    return null;
                }
                Node childIter;
                uint i;
                for (childIter = _offspring, i = 0;
                     childIter != null && i < ind;
                     childIter = childIter.GetRightSibling(true), i++)
                {
                    // Nothing to do here //TODO
                }
                return childIter;
            }
        }

        #endregion

    }
}
