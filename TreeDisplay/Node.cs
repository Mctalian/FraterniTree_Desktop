using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

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
        private Node _parent;
        /// <summary>
        /// Offspring, or First Child of this Node.
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
        private int _numChildren;

        #endregion

        #region Node Location and Appearance Data

        /// <summary>
        /// Node X Location
        /// </summary>
        private int _xCoord;
        /// <summary>
        /// Node Y Location
        /// </summary>
        private int _yCoord;
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
        /// If a callback is needed based on user implementation
        /// </summary>
        private Action _callback;
        /// <summary>
        /// Ignore this node and consequently all descendants
        /// </summary>
        private bool _ignoreNode;

        #endregion

        #endregion

        #region Public Data

        /// <summary>
        /// Left Neigbor to this node.
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
                    int nC, int x, int y, int w, int h,
                    Node prev, float prelim, float mod)
        {
            _parent = p;
            _offspring = o;
            _leftSibling = l;
            _rightSibling = r;
            _numChildren = nC;
            _xCoord = x;
            _yCoord = y;
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

        public Node FirstChild(bool isIndex = false)
        {
            var unignoredChild = _offspring;
            if (unignoredChild == null)
            {
                return null;
            }

            if (!unignoredChild.IsIgnored() || isIndex)
            {
                return _offspring;
            }

            while (unignoredChild.HasRightSibling())
            {
                unignoredChild = unignoredChild.RightSibling();

                if (!unignoredChild.IsIgnored())
                {
                    return unignoredChild;
                }
            }
            return null;
        }

        protected void FirstChild(Node child)
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
                FirstChild(child);
                child.LeftSibling(null);
            }
            else
            {
                Node siblingIter;
                for (siblingIter = _offspring; siblingIter != null && siblingIter.HasRightSibling(); siblingIter = siblingIter.RightSibling())
                {
                    // Nothing to do here
                    // TODO - why are we looping for no reason?
                }
                if (siblingIter != null)
                {
                    siblingIter.RightSibling(child);
                    child.LeftSibling(siblingIter);
                }
            }

            child.Parent(this);
            _numChildren++;
        }

        public void RemoveNode()
        {
            for (var i = GetNumberOfChildren() - 1; i >= 0; i--)
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
            if (child == FirstChild())
            {
                _offspring = child.RightSibling();
                if (_offspring != null)
                {
                    _offspring.LeftSibling(null);
                }
            }
            else
            {
                Node siblingIter;
                for (siblingIter = _offspring; siblingIter != child && siblingIter != null; siblingIter = siblingIter.RightSibling())
                {
                    // Nothing to do here
                    // TODO
                }
                if (siblingIter == child)
                {
                    if (child.HasLeftSibling()) //TODO
                    {
                        child.LeftSibling().RightSibling(child.RightSibling());
                    }
                    if (child.HasRightSibling())
                    {
                        child.RightSibling().LeftSibling(child.LeftSibling());
                    }
                }
            }

            child.Parent(null);
            child.LeftSibling(null);
            child.RightSibling(null);
            _numChildren--;
        }

        public Node LeftSibling(bool isIndex = false) //TODO can this be merged with right?
        {
            var unignoredLeftSib = _leftSibling;
            if (unignoredLeftSib == null)
            {
                return null;
            }

            if (!unignoredLeftSib.IsIgnored() || isIndex)
            {
                return _leftSibling;
            }

            while (unignoredLeftSib.HasLeftSibling())
            {
                unignoredLeftSib = unignoredLeftSib.LeftSibling();

                if (!unignoredLeftSib.IsIgnored())
                {
                    return unignoredLeftSib;
                }
            }
            return null;
        }

        protected void LeftSibling(Node sibling)
        {
            _leftSibling = sibling;
        }

        public Node RightSibling(bool IsIndex = false)
        {
            var unignoredRightSib = _rightSibling; //TODO change name
            if (unignoredRightSib == null)
            {
                return null;
            }

            if (!unignoredRightSib.IsIgnored() || IsIndex)
            {
                return _rightSibling;
            }

            while (unignoredRightSib.HasRightSibling())
            {
                unignoredRightSib = unignoredRightSib.RightSibling();

                if (!unignoredRightSib.IsIgnored())
                {
                    return unignoredRightSib;
                }
            }
            return null;
        }

        protected void RightSibling(Node sibling) //TODO change names to add more detail
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

            for (var i = GetNumberOfChildren() - 1; i >= 0; i--)
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
            if (_parent != null)
            {
                if (!_parent.IsIgnored() || isIndex)
                {
                    return true;
                }
            }
            return false;    
        }

        public bool HasChild()
        {
            if (_offspring != null)
            {
                for (var i = GetNumberOfChildren() - 1; i >= 0; i--)
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
                        tempLeftSib = tempLeftSib.LeftSibling();
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
                        tempRightSib = tempRightSib.RightSibling();
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
            for (var i = 0; i < _numChildren; i++)
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

        public int GetXCoord() // gets and sets
        {
            return _xCoord;
        }

        public void SetXCoord(int x)
        {
            _xCoord = x;
        }

        public int GetYCoord()
        {
            return _yCoord;
        }

        public void SetYCoord(int y)
        {
            _yCoord = y;
        }

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

        public int GetNumberOfChildren()
        {
            return _numChildren;
        }

        public int GetNumGenerations()
        {
            if (!HasChild())
            {
                return 0;
            }

            var gens = 0;
            for (var i = 0; i < GetNumberOfChildren(); i++)
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
                 childIter = childIter.RightSibling(), i++)
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
                if (ind > GetNumberOfChildren() - 1 || ind < 0)
                {
                    return null;
                }
                Node childIter;
                uint i;
                for (childIter = _offspring, i = 0;
                     childIter != null && i < ind;
                     childIter = childIter.RightSibling(true), i++)
                {
                    // Nothing to do here //TODO
                }
                return childIter;
            }
        }

        #endregion

    }
}
