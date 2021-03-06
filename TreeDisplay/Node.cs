﻿using System;
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
        private Node m_Parent;
        /// <summary>
        /// Offspring, or First Child of this Node.
        /// </summary>
        private Node m_Offspring;
        /// <summary>
        /// Left Sibling Node of this Node.
        /// </summary>
        private Node m_LeftSibling;
        /// <summary>
        /// Right Sibling Node of this Node.
        /// </summary>
        private Node m_RightSibling;

        #endregion

        #region Descendant Data

        /// <summary>
        /// Number of children nodes of this Node.
        /// </summary>
        private int m_NumChildren;

        #endregion

        #region Node Location and Appearance Data

        /// <summary>
        /// Node X Location
        /// </summary>
        private int m_XCoord;
        /// <summary>
        /// Node Y Location
        /// </summary>
        private int m_YCoord;
        /// <summary>
        /// Node Width
        /// </summary>
        private int m_Width;
        /// <summary>
        /// Node Height
        /// </summary>
        private int m_Height;

        #endregion

        #region Implementation Specific Data
        /// <summary>
        /// If a callback is needed based on user implementation
        /// </summary>
        private Action m_Callback = null;
        /// <summary>
        /// Ignore this node and consequently all descendants
        /// </summary>
        private bool m_IgnoreNode;

        #endregion

        #endregion

        #region Public Data

        /// <summary>
        /// Left Neigbor to this node.
        /// </summary>
        public Node m_Prev;
        /// <summary>
        /// Preliminary location value
        /// </summary>
        public float m_Prelim;
        /// <summary>
        /// Modifier used to determine final location
        /// </summary>
        public float m_Modifier;

        #endregion

        #region Constructors

        public Node(Node p, Node o, Node l, Node r,
                    int nC, int x, int y, int w, int h,
                    Node prev, float prelim, float mod)
        {
            m_Parent = p;
            m_Offspring = o;
            m_LeftSibling = l;
            m_RightSibling = r;
            m_NumChildren = nC;
            m_XCoord = x;
            m_YCoord = y;
            m_Width = w;
            m_Height = h;
            m_Prev = prev;
            m_Prelim = prelim;
            m_Modifier = mod;
        }

        public Node() : this(null, null, null, null, 0, 0, 0, 0, 0, null, 0, 0)
        {
        }

        public Node(Node duplicate)
        {

        }

        #endregion

        #region Tree Navigation and Manipulation Methods

        public Node FirstChild(bool isIndex = false)
        {
            Node UnignoredChild = m_Offspring;
            if (UnignoredChild == null)
            {
                return null;
            }

            if (!UnignoredChild.IsIgnored() || isIndex)
            {
                return m_Offspring;
            }
            else
            {
                while (UnignoredChild.HasRightSibling())
                {
                    UnignoredChild = UnignoredChild.RightSibling();

                    if (!UnignoredChild.IsIgnored())
                    {
                        return UnignoredChild;
                    }
                }
                return null;
            }
        }

        protected void FirstChild(Node Child)
        {
            m_Offspring = Child;
        }

        public void AddChild(Node Child)
        {
            if (Child.HasParent())
            {
                Child.Parent().RemoveChild(Child);
            }
            if (this.IsLeaf())
            {
                this.FirstChild(Child);
                Child.LeftSibling(null);
            }
            else
            {
                Node SiblingIter;
                for (SiblingIter = this.m_Offspring; SiblingIter != null && SiblingIter.HasRightSibling(); SiblingIter = SiblingIter.RightSibling())
                {
                    // Nothing to do here
                }
                if (SiblingIter != null)
                {
                    SiblingIter.RightSibling(Child);
                    Child.LeftSibling(SiblingIter);
                }
            }
            Child.Parent(this);
            this.m_NumChildren++;
        }

        public void RemoveNode()
        {
            for (int i = GetNumberOfChildren() - 1; i >= 0; i--)
            {
                if (this[i] == null)
                {
                    continue;
                }
                this.Parent().AddChild(this[i]);
            }

            this.Parent().RemoveChild(this);
        }

        protected void RemoveChild(Node Child)
        {
            if (Child == this.FirstChild())
            {
                this.m_Offspring = Child.RightSibling();
                if (this.m_Offspring != null)
                {
                    this.m_Offspring.LeftSibling(null);
                }
            }
            else
            {
                Node SiblingIter;
                for (SiblingIter = this.m_Offspring; SiblingIter != Child && SiblingIter != null; SiblingIter = SiblingIter.RightSibling())
                {
                    // Nothing to do here
                }
                if (SiblingIter == Child)
                {
                    if (Child.HasLeftSibling())
                    {
                        Child.LeftSibling().RightSibling(Child.RightSibling());
                    }
                    if (Child.HasRightSibling())
                    {
                        Child.RightSibling().LeftSibling(Child.LeftSibling());
                    }
                }
            }
            Child.Parent(null);
            Child.LeftSibling(null);
            Child.RightSibling(null);
            this.m_NumChildren--;
        }

        //public void RemoveAllChildren()
        //{
        //    for (int i = GetNumberOfChildren() - 1; i >= 0; i--)
        //    {
        //        RemoveChild(this[i]);
        //    }
        //}

        public Node LeftSibling(bool isIndex = false)
        {
            Node UnignoredLeftSib = m_LeftSibling;
            if (UnignoredLeftSib == null)
            {
                return null;
            }

            if (!UnignoredLeftSib.IsIgnored() || isIndex)
            {
                return m_LeftSibling;
            }
            else
            {
                while (UnignoredLeftSib.HasLeftSibling())
                {
                    UnignoredLeftSib = UnignoredLeftSib.LeftSibling();

                    if (!UnignoredLeftSib.IsIgnored())
                    {
                        return UnignoredLeftSib;
                    }
                }
                return null;
            }
        }

        protected void LeftSibling(Node Sibling)
        {
            m_LeftSibling = Sibling;
        }

        public Node RightSibling(bool IsIndex = false)
        {
            Node UnignoredRightSib = m_RightSibling;
            if (UnignoredRightSib == null)
            {
                return null;
            }

            if (!UnignoredRightSib.IsIgnored() || IsIndex)
            {
                return m_RightSibling;
            }
            else
            {
                while (UnignoredRightSib.HasRightSibling())
                {
                    UnignoredRightSib = UnignoredRightSib.RightSibling();

                    if (!UnignoredRightSib.IsIgnored())
                    {
                        return UnignoredRightSib;
                    }
                }
                return null;
            }
        }

        protected void RightSibling(Node Sibling)
        {
            m_RightSibling = Sibling;
        }

        public Node Parent(bool isIndex = false)
        {
            if (m_Parent != null)
            {
                if (!m_Parent.IsIgnored() || isIndex)
                {
                    return m_Parent;
                }
            }
            return null;
        }

        protected void Parent(Node Parent)
        {
            m_Parent = Parent;
        }

        public Node LeftNeighbor()
        {
            Node UnignoredLeftNeighbor = m_Prev;
            if (UnignoredLeftNeighbor == null)
            {
                return null;
            }

            if (!UnignoredLeftNeighbor.IsIgnored())
            {
                return m_Prev;
            }
            else
            {
                while (UnignoredLeftNeighbor.LeftNeighbor() != null)
                {
                    UnignoredLeftNeighbor = UnignoredLeftNeighbor.LeftNeighbor();

                    if (!UnignoredLeftNeighbor.IsIgnored())
                    {
                        return UnignoredLeftNeighbor;
                    }
                }
                return null;
            }
        }

        #endregion

        #region Check Associated Nodes

        public bool IsLeaf()
        {
            if (this != null && this.m_Offspring == null)
            {
                return true;
            }
            else
            {
                for (int i = GetNumberOfChildren() - 1; i >= 0; i--)
                {
                    if (this[i] == null)
                    {
                        continue;
                    }
                    if (this[i].IsIgnored())
                    {
                        continue;
                    }
                    else
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public bool HasParent(bool isIndex = false)
        {
            if (this != null && this.m_Parent != null)
            {
                if (!m_Parent.IsIgnored() || isIndex)
                {
                    return true;
                }
            }
            return false;    
        }

        public bool HasChild()
        {
            if (this != null && this.m_Offspring != null)
            {
                for (int i = GetNumberOfChildren() - 1; i >= 0; i--)
                {
                    if (this[i] == null)
                    {
                        continue;
                    }
                    if (this[i].IsIgnored())
                    {
                        continue;
                    }
                    else
                    {
                        return true;
                    }
                }
                return false;
            }
            else
            {
                return false;
            }
        }

        public bool HasLeftSibling(bool isIndex = false)
        {
            if (this != null && this.m_LeftSibling != null)
            {
                Node tempLeftSib = m_LeftSibling;
                if (!tempLeftSib.IsIgnored() || isIndex)
                {
                    return true;
                }
                else
                {
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
            }
            else
            {
                return false;
            }
        }

        public bool HasRightSibling(bool isIndex = false)
        {
            if (this != null && this.m_RightSibling != null)
            {
                Node tempRightSib = m_RightSibling;
                if (!tempRightSib.IsIgnored() || isIndex)
                {
                    return true;
                }
                else
                {
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
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region Implementation Specific

        public void SetCallback(Action cb)
        {
            m_Callback = cb;
        }

        public void ExecuteCallback()
        {
            m_Callback();
            for (int i = 0; i < m_NumChildren; i++)
            {
                if (this[i] != null)
                {
                    this[i].ExecuteCallback();
                }
            }
        }

        public void SetIgnore(bool b)
        {
            m_IgnoreNode = b;
        }

        public bool IsIgnored()
        {
            return m_IgnoreNode;
        }

        #endregion

        #region Node Sizing and Appearance

        public int GetXCoord()
        {
            return m_XCoord;
        }

        public void SetXCoord(int x)
        {
            m_XCoord = x;
        }

        public int GetYCoord()
        {
            return m_YCoord;
        }

        public void SetYCoord(int y)
        {
            m_YCoord = y;
        }

        public int GetWidth()
        {
            return m_Width;
        }

        public void SetWidth(int w)
        {
            if (w >= 0)
            {
                m_Width = w;
            }
        }

        public int GetHeight()
        {
            return m_Height;
        }

        public void SetHeight(int h)
        {
            if (h >= 0)
            {
                m_Height = h;
            }
        }

        #endregion

        #region Descendent Counting

        public int GetNumberOfChildren()
        {
            return m_NumChildren;
        }

        public int GetNumGenerations()
        {
            if (this.HasChild())
            {
                int gens = 0;
                for (int i = 0; i < GetNumberOfChildren(); i++)
                {
                    if (this[i] != null)
                    {
                        gens = Math.Max(gens, this[i].GetNumGenerations());
                    }
                }
                return gens + 1;
            }
            else
            {
                return 0;
            }
        }

        #endregion

        #region Children Indexing

        public int GetChildIndex(Node Child)
        {
            Node ChildIter;
            uint i;
            for (ChildIter = this.m_Offspring, i = 0;
                 ChildIter != Child && ChildIter != null;
                 ChildIter = ChildIter.RightSibling(), i++)
            {
            }
            if (ChildIter != null)
            {
                return (int)(i);
            }
            else
            {
                return -1;
            }
        }

        public Node this[int ind]
        {
            get
            {
                if (ind > GetNumberOfChildren() - 1 || ind < 0)
                {
                    return null;
                }
                Node ChildIter;
                uint i;
                for (ChildIter = this.m_Offspring, i = 0;
                     ChildIter != null && i < ind;
                     ChildIter = ChildIter.RightSibling(true), i++)
                {
                    // Nothing to do here
                }
                return ChildIter;
            }
        }

        #endregion

    }
}
