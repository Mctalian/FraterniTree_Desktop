using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace TreeDisplay
{
    /// <summary>
    /// Uses the John Q. Walker II algorithm: http://www.cs.unc.edu/techreports/89-034.pdf
    /// Referenced C++ Implementation during troubleshooting: http://www.drdobbs.com/positioning-nodes-for-general-trees/184402320
    /// </summary>
    public static class WalkerAlgorithmTree
    {
        private static PreviousNode LevelZeroPtr = null;
        private static int xTopAdjustment;
        private static int yTopAdjustment;
        public static int MaxDepth          { get; set; } // Number of levels in the tree
        public static int LevelSeparation   { get; set; } // Vertical Separation
        public static int SiblingSeparation { get; set; } // Horizontal Separation Fine
        public static int SubtreeSeparation { get; set; } // Horizontal Separation Coarse

        /// <summary>
        /// This function determines the coordinates for each
        /// node in a tree. A pointer to the apex node of the
        /// tree is passed as input. This assumes that the x
        /// and y coordinates of the apex node are set as
        /// desired, since the tree underneath it will be
        /// positioned with respect to those coordinates.
        /// </summary>
        /// <param name="ApexNode">The root node of the tree to be positioned</param>
        /// <returns>TRUE if no errors, otherwise returns FALSE</returns>
        public static bool PositionTree(Node ApexNode)
        {
            if (ApexNode != null)
            {
                // Initialize the list of previous nodes at each level.
                InitPrevNodeList();

                // Do the preliminary positioning with postorder walk.
                FirstWalk(ApexNode, 0);

                // Determine how to adjust all the nodes with respect to
                // the location of the root.
                xTopAdjustment = ApexNode.GetXCoord() - (int)(ApexNode.m_Prelim);
                yTopAdjustment = ApexNode.GetYCoord();

                // Do the final positioning with a preorder walk.
                bool result = SecondWalk(ApexNode, 0, 0);
                if (result)
                {
                    // User specific, if callbacks are needed.
                    ApexNode.ExecuteCallback();
                }
                return result;
            }
            else
            {
                // Trivial: Return true if a null pointer was passed.
                return true;
            }
        }

        /// <summary>
        /// In this first postorder walk, every node of the tree is
        /// assigned a preliminary x-coordinate (held in field
        /// Node.m_Prelim). In addition, internal nodes are given
        /// modifiers, which will be used to move their offspring
        /// to the right (held in field Node.m_Modifier).
        /// </summary>
        /// <param name="ThisNode">Node to begin walk</param>
        /// <param name="CurrentLevel">Level of the tree</param>
        private static void FirstWalk(Node ThisNode, int CurrentLevel)
        {
            Node LeftMost;
            Node RightMost;
            Node LeftMostUnignored = null;
            Node RightMostUnignored = null;
            float Midpoint;

            if (ThisNode.IsIgnored())
            {
                return;
            }

            ThisNode.m_Prev = GetPrevNodeAtLevel(CurrentLevel);

            SetPrevNodeAtLevel(CurrentLevel, ThisNode);

            ThisNode.m_Modifier = 0;

            if (ThisNode.IsLeaf() || (CurrentLevel == MaxDepth))
            {
                if (ThisNode.HasLeftSibling())
                {
                    if (!ThisNode.LeftSibling().IsIgnored())
                    {
                        ThisNode.m_Prelim = ThisNode.LeftSibling().m_Prelim +
                                        (float)SiblingSeparation +
                                        MeanNodeSize(ThisNode.LeftSibling(), ThisNode);
                    }
                    else
                    {
                        ThisNode.m_Prelim = 0;
                    }
                }
                else
                {
                    ThisNode.m_Prelim = 0;
                }
            }
            else
            {
                RightMost = ThisNode.FirstChild();
                LeftMost = RightMost;
                RightMostUnignored = RightMost;
                LeftMostUnignored = LeftMost;
                if (!LeftMost.IsIgnored())
                {
                    FirstWalk(LeftMost, CurrentLevel + 1);
                }

                while (RightMost.HasRightSibling())
                {
                    RightMost = RightMost.RightSibling();
                    if (!RightMost.IsIgnored())
                    {
                        RightMostUnignored = RightMost;
                        FirstWalk(RightMost, CurrentLevel + 1);
                    }
                    if (LeftMostUnignored.IsIgnored())
                    {
                        LeftMostUnignored = RightMost;
                    }
                }
                if (!LeftMost.IsIgnored() && !RightMost.IsIgnored() && RightMost == RightMostUnignored)
                {
                    Midpoint = ((float)(LeftMost.m_Prelim + RightMost.m_Prelim) / (float)(2));
                }
                else if (LeftMost.IsIgnored() && !RightMost.IsIgnored() && RightMost == RightMostUnignored)
                {
                    Midpoint = ((float)(LeftMostUnignored.m_Prelim + RightMost.m_Prelim) / (float)(2));
                }
                else if (!LeftMost.IsIgnored())
                {
                    Midpoint = ((float)(LeftMost.m_Prelim + RightMostUnignored.m_Prelim) / (float)(2));
                }
                else
                {
                    Midpoint = ((float)(LeftMostUnignored.m_Prelim + RightMostUnignored.m_Prelim) / (float)(2));
                }
                

                if (ThisNode.HasLeftSibling())
                {
                    if (!ThisNode.LeftSibling().IsIgnored())
                    {
                        ThisNode.m_Prelim = ThisNode.LeftSibling().m_Prelim +
                                        (float)SiblingSeparation +
                                        MeanNodeSize(ThisNode.LeftSibling(), ThisNode);
                        ThisNode.m_Modifier = ThisNode.m_Prelim - Midpoint;
                        Apportion(ThisNode, CurrentLevel);
                    }
                    else
                    {
                        ThisNode.m_Prelim = Midpoint;
                    }  
                }
                else
                {
                    ThisNode.m_Prelim = Midpoint;
                }
            }
        }

        /// <summary>
        /// During a second preorder walk, each node is given
        /// a final x-coordinate by summing its preliminary
        /// x-coordinate and the modifiers of all the node's
        /// ancestors. The y-coordinate depends on the height
        /// of the tree. If the actual position of an interior
        /// node is right of its preliminary place, the subtree
        /// rooted at the node must be moved right to center the
        /// sons around the father. Rather than immediately
        /// readjust all the nodes in the subtree, each node
        /// remembers the distance to the provisional place in a
        /// modifier field (Node.m_Modifier). In this second pass
        /// down the tree, modifiers are accumulated and applied
        /// to every node.
        /// </summary>
        /// <param name="ThisNode">Root of the current subtree</param>
        /// <param name="CurrentLevel">Level of the tree</param>
        /// <param name="Modsum">Modifier sum from ancestors</param>
        /// <returns></returns>
        private static bool SecondWalk(Node ThisNode, int CurrentLevel, int Modsum)
        {
            bool result = true;
            long xTemp;
            long yTemp;
            float NewModsum;

            xTemp = 0;
            yTemp = 0;

            if (CurrentLevel <= MaxDepth)
            {
                NewModsum = Modsum;

                xTemp = (long)xTopAdjustment + (long)(ThisNode.m_Prelim + Modsum);
                yTemp = (long)yTopAdjustment + (long)(CurrentLevel * (LevelSeparation + ThisNode.GetHeight()));
            }
            if (CheckExtentRange((int)xTemp, (int)yTemp))
            {
                ThisNode.SetXCoord((int)xTemp);
                ThisNode.SetYCoord((int)yTemp);

                if (ThisNode.HasChild())
                {
                    result = SecondWalk(ThisNode.FirstChild(), CurrentLevel + 1, (int)(Modsum + ThisNode.m_Modifier));
                }

                if (result && ThisNode.HasRightSibling())
                {
                    result = SecondWalk(ThisNode.RightSibling(), CurrentLevel, Modsum);
                }
            }
            else
            {
                result = false;
            }

            return result;
        }

        /// <summary>
        /// This procedure cleans up the positioning
        /// of small sibling subtrees, thus fixing the "left-to-right gluing"
        /// problem evident in earlier algorithms. When moving a new subtree
        /// farther and farther to the right, gaps may open up among smaller
        /// subtrees that were previously sandwhiched between larger subtrees.
        /// Thus, when moving the new, larger subtree to the right, the distance
        /// it is moved is also apportioned to smaller, interior subtrees,
        /// creating a pleasing aesthetic placement.
        /// </summary>
        /// <param name="ThisNode">Root node of subtree</param>
        /// <param name="CurrentLevel">Level of the main tree</param>
        private static void Apportion(Node ThisNode, int CurrentLevel)
        {
            Node LeftMost;
            Node Neighbor;
            Node AncestorLeftMost;
            Node AncestorNeighbor;
            Node tmp;
            uint i;
            uint CompareDepth;
            uint DepthToStop;
            uint NumLeftSiblings;
            float LeftModsum;
            float RightModsum;
            float Distance;
            float Portion;

            LeftMost = ThisNode.FirstChild();
            Neighbor = LeftMost.LeftNeighbor();

            CompareDepth = 1;
            DepthToStop = (uint)(MaxDepth - CurrentLevel);

            while (LeftMost != null && Neighbor != null && CompareDepth <= DepthToStop)
            {
                LeftModsum = 0;
                RightModsum = 0;

                AncestorLeftMost = LeftMost;
                AncestorNeighbor = Neighbor;

                for (i = 0; i < CompareDepth; i++)
                {
                    AncestorLeftMost = AncestorLeftMost.Parent();
                    AncestorNeighbor = AncestorNeighbor.Parent();
                    RightModsum += AncestorLeftMost.m_Modifier;
                    LeftModsum += AncestorNeighbor.m_Modifier;
                }

                Distance = Neighbor.m_Prelim +
                           LeftModsum +
                           SubtreeSeparation +
                           MeanNodeSize(LeftMost, Neighbor) -
                           (LeftMost.m_Prelim + RightModsum);

                if (Distance > 0)
                {
                    NumLeftSiblings = 0;
                    for (tmp = ThisNode; tmp != null && tmp != AncestorNeighbor; tmp = tmp.LeftSibling())
                    {
                        NumLeftSiblings++;
                    }

                    if (tmp != null)
                    {
                        Portion = Distance / (float)(NumLeftSiblings);
                        for (tmp = ThisNode; tmp != AncestorNeighbor; tmp = tmp.LeftSibling())
                        {
                            tmp.m_Prelim = tmp.m_Prelim + Distance;
                            tmp.m_Modifier = tmp.m_Modifier + Distance;
                            Distance -= Portion;
                        }
                    }
                    else
                    {
                        return;
                    }

                }

                CompareDepth++;
                if (LeftMost.IsLeaf())
                {
                    LeftMost = GetLeftMost(ThisNode, 0, (int)(CompareDepth));
                }
                else
                {
                    LeftMost = LeftMost.FirstChild();
                }
                if (LeftMost != null)
                {
                    Neighbor = LeftMost.LeftNeighbor();
                }
                else
                {
                    Neighbor = null;
                }
            } // end of while
        }

        /// <summary>
        /// This function returns the leftmost descendant of a
        /// node at a given Depth. This is implemented using a
        /// postorder walk of the subtree under Node, down to 
        /// the Level of Depth. Level here is not the absolute
        /// tree level used in the two main tree walks; it
        /// refers to the level below the node whose leftmost
        /// descendant is being found.
        /// </summary>
        /// <param name="ThisNode">Root of current subtree</param>
        /// <param name="CurrentLevel">Level below original node searching for leftmost descendant</param>
        /// <param name="SearchDepth">Depth comparison</param>
        /// <returns></returns>
        private static Node GetLeftMost(Node ThisNode, int CurrentLevel, int SearchDepth)
        {
            Node LeftMost;
            Node RightMost;

            if (CurrentLevel == SearchDepth)
            {
                LeftMost = ThisNode;
            }
            else if (ThisNode.IsLeaf())
            {
                LeftMost = null;
            }
            else
            {
                for (LeftMost = GetLeftMost(RightMost = ThisNode.FirstChild(), CurrentLevel + 1, SearchDepth);
                     LeftMost == null && RightMost.HasRightSibling();
                     LeftMost = GetLeftMost(RightMost = RightMost.RightSibling(), CurrentLevel + 1, SearchDepth))
                {
                    // Nothing to do
                }
            }
            return LeftMost;
        }

        /// <summary>
        /// This function returns the mean size of the two
        /// passed nodes. It addes the size of the right half
        /// of lefthand node to the left half of righthand node.
        /// If all nodes are the same size, this is a trivial
        /// calculation.
        /// </summary>
        /// <param name="LeftNode">The left of the two nodes</param>
        /// <param name="RightNode">The right of the two nodes</param>
        /// <returns>Mean size between the two nodes</returns>
        private static float MeanNodeSize(Node LeftNode, Node RightNode)
        {
            float NodeSize = 0;

            if (LeftNode != null)
            {
                NodeSize += ((float)(LeftNode.GetWidth()) / 2);
            }
            if (RightNode != null)
            {
                NodeSize += ((float)(RightNode.GetWidth()) / 2);
            }

            return NodeSize;
        }

        /// <summary>
        /// This function verifies that the passed x-coordinate
        /// and y-coordinate are within the coordinate system
        /// being used for the drawing. For example, if the x-
        /// and y-coordinates must be 2-byte integers, this
        /// function could determine whether xValue or
        /// yValue are too large.
        /// </summary>
        /// <param name="xValue">x-coordinate value to check</param>
        /// <param name="yValue">y-coordinate value to check</param>
        /// <returns></returns>
        private static bool CheckExtentRange(int xValue, int yValue)
        {
            return true;
        }

        /// <summary>
        /// Initialize the list of previous nodes at each level.
        /// </summary>
        private static void InitPrevNodeList()
        {
            PreviousNode tmp;
            for (tmp = LevelZeroPtr; tmp != null; tmp = tmp.m_NextLevel)
            {
                tmp.m_PrevNode = null;
            }
        }

        /// <summary>
        /// Get the previous node at this level
        /// </summary>
        /// <param name="LevelNumber"></param>
        /// <returns></returns>
        private static Node GetPrevNodeAtLevel(int LevelNumber)
        {
            PreviousNode tmp = LevelZeroPtr;
            uint i = 0;

            for (tmp = LevelZeroPtr; tmp != null; tmp = tmp.m_NextLevel)
            {
                if (i++ == LevelNumber)
                {
                    return tmp.m_PrevNode;
                }
            }
            return null;
        }

        /// <summary>
        /// Set an element in the list.
        /// </summary>
        /// <param name="LevelNumber">Current Level</param>
        /// <param name="ThisNode">Starting Node</param>
        private static void SetPrevNodeAtLevel(int LevelNumber, Node ThisNode)
        {
            PreviousNode tmp;
            PreviousNode NewNode;
            uint i = 0;

            for (tmp = LevelZeroPtr; tmp != null; tmp = tmp.m_NextLevel)
            {
                if (i++ == LevelNumber)
                {
                    tmp.m_PrevNode = ThisNode;
                    return;
                }
                else if (tmp.m_NextLevel == null)
                {
                    NewNode = new PreviousNode();
                    NewNode.m_PrevNode = null;
                    NewNode.m_NextLevel = null;
                    tmp.m_NextLevel = NewNode;
                }
            }

            LevelZeroPtr = new PreviousNode();
            LevelZeroPtr.m_PrevNode = ThisNode;
            LevelZeroPtr.m_NextLevel = null;
        }
    }
}
