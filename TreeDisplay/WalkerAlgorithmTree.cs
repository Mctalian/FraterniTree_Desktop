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

        private static PreviousNode LevelZeroPtr;
        private static int xTopAdjustment;
        private static int yTopAdjustment;
        public static int MaxDepth { get; set; } // Number of levels in the tree
        public static int LevelSeparation { get; set; } // Vertical Separation
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
            if( ApexNode == null )
            {
                return true;
            }

            // Initialize the list of previous nodes at each level.
            InitListofPreviousNodes();

            // Do the preliminary positioning with postorder walk.
            FirstWalk( ApexNode, 0 );

            // Determine how to adjust all the nodes with respect to
            // the location of the root.
            xTopAdjustment = ApexNode.CoordinateX - (int) ApexNode.Prelim;
            yTopAdjustment = ApexNode.CoordinateY;

            // Do the final positioning with a preorder walk.
            var result = SecondWalk( ApexNode, 0, 0 );
            if( result )
            {
                // User specific, if callbacks are needed.
                ApexNode.ExecuteCallback();
            }

            return result;
        }

        /// <summary>
        /// In this first postorder walk, every node of the tree is
        /// assigned a preliminary x-coordinate (held in field
        /// Node.Prelim). In addition, internal nodes are given
        /// modifiers, which will be used to move their offspring
        /// to the right (held in field Node.Modifier).
        /// </summary>
        /// <param name="currentNode">Node to begin walk</param>
        /// <param name="currentLevel">Level of the tree</param>
        private static void FirstWalk(Node currentNode, int currentLevel)
        {
            currentNode.Prev = GetPrevNodeAtLevel( currentLevel );

            SetPrevNodeAtLevel( currentLevel, currentNode );

            currentNode.Modifier = 0;

            if( currentNode.IsLeaf()
                || (currentLevel == MaxDepth) )
            {
                if( currentNode.HasLeftSibling() )
                {
                    currentNode.Prelim = currentNode.GetLeftSibling().Prelim +
                                         SiblingSeparation +
                                         MeanNodeSize( currentNode.GetLeftSibling(), currentNode );
                }
                else
                {
                    currentNode.Prelim = 0;
                }
            }
            else
            {
                var rightMostNode = currentNode.GetFirstChild();
                var leftMostNode = rightMostNode;
                FirstWalk( leftMostNode, currentLevel + 1 );

                while ( rightMostNode.HasRightSibling() )
                {
                    rightMostNode = rightMostNode.GetRightSibling();
                    FirstWalk( rightMostNode, currentLevel + 1 );
                }

                var midpoint = (leftMostNode.Prelim + rightMostNode.Prelim)/2;

                if( currentNode.HasLeftSibling() )
                {
                    currentNode.Prelim = currentNode.GetLeftSibling().Prelim +
                                         SiblingSeparation +
                                         MeanNodeSize( currentNode.GetLeftSibling(), currentNode );
                    currentNode.Modifier = currentNode.Prelim - midpoint;
                    Apportion( currentNode, currentLevel );
                }
                else
                {
                    currentNode.Prelim = midpoint;
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
        /// modifier field (Node.Modifier). In this second pass
        /// down the tree, modifiers are accumulated and applied
        /// to every node.
        /// </summary>
        /// <param name="currentNode">Root of the current subtree</param>
        /// <param name="currentLevel">Level of the tree</param>
        /// <param name="Modsum">Modifier sum from ancestors</param>
        /// <returns></returns>
        private static bool SecondWalk(Node currentNode, int currentLevel, int Modsum)
        {
            var result = true;
            long xTemp = 0;
            long yTemp = 0;

            if( currentLevel <= MaxDepth )
            {
                xTemp = xTopAdjustment + (long) (currentNode.Prelim + Modsum);
                yTemp = yTopAdjustment + (long) (currentLevel*(LevelSeparation + currentNode.GetHeight()));
            }

            if( CheckExtentRange( (int) xTemp, (int) yTemp ) )
            {
                currentNode.CoordinateX = (int) xTemp;
                currentNode.CoordinateY = (int) yTemp;

                if( currentNode.HasChild() )
                {
                    result = SecondWalk( currentNode.GetFirstChild(), currentLevel + 1,
                        (int) (Modsum + currentNode.Modifier) );
                }

                if( result && currentNode.HasRightSibling() )
                {
                    result = SecondWalk( currentNode.GetRightSibling(), currentLevel, Modsum );
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
        /// <param name="currentNode">Root node of subtree</param>
        /// <param name="currentLevel">Level of the main tree</param>
        private static void Apportion(Node currentNode, int currentLevel)
        {
            var leftMostNode = currentNode.GetFirstChild();
            var neighbor = leftMostNode.LeftNeighbor();
            uint compareDepth = 1;
            var depthToStop = (uint) (MaxDepth - currentLevel);

            while ( leftMostNode != null
                    && neighbor != null
                    && compareDepth <= depthToStop )
            {
                float leftModsum = 0;
                float rightModsum = 0;

                var ancestorLeftMost = leftMostNode;
                var ancestorNeighbor = neighbor;

                for ( var i = 0; i < compareDepth; i++ )
                {
                    ancestorLeftMost = ancestorLeftMost.Parent();
                    ancestorNeighbor = ancestorNeighbor.Parent();
                    rightModsum += ancestorLeftMost.Modifier;
                    leftModsum += ancestorNeighbor.Modifier;
                }

                var distance = neighbor.Prelim +
                               leftModsum +
                               SubtreeSeparation +
                               MeanNodeSize( leftMostNode, neighbor ) -
                               (leftMostNode.Prelim + rightModsum);

                if( distance > 0 )
                {
                    uint numLeftSiblings = 0;
                    Node tmp;

                    for ( tmp = currentNode; tmp != null && tmp != ancestorNeighbor; tmp = tmp.GetLeftSibling() )
                    {
                        numLeftSiblings++;
                    }

                    if( tmp != null )
                    {
                        var portion = distance/numLeftSiblings;
                        for ( tmp = currentNode; tmp != ancestorNeighbor; tmp = tmp.GetLeftSibling() )
                        {
                            tmp.Prelim = tmp.Prelim + distance;
                            tmp.Modifier = tmp.Modifier + distance;
                            distance -= portion;
                        }
                    }
                    else
                    {
                        return;
                    }
                }

                compareDepth++;
                leftMostNode = leftMostNode.IsLeaf()
                    ? GetLeftMost( currentNode, 0, (int) compareDepth )
                    : leftMostNode.GetFirstChild();

                neighbor = leftMostNode == null
                    ? null
                    : leftMostNode.LeftNeighbor();
            }
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
        /// <param name="currentNode">Root of current subtree</param>
        /// <param name="currentLevel">Level below original node searching for leftmost descendant</param>
        /// <param name="searchDepth">Depth comparison</param>
        /// <returns></returns>
        private static Node GetLeftMost(Node currentNode, int currentLevel, int searchDepth)
        {
            if( currentLevel == searchDepth )
            {
                return currentNode;
            }

            if( currentNode.IsLeaf() )
            {
                return null;
            }

            Node LeftMost;
            var rightMostNode = currentNode.GetFirstChild();

            for ( LeftMost = GetLeftMost( rightMostNode, currentLevel + 1, searchDepth );
                LeftMost == null && rightMostNode.HasRightSibling();
                rightMostNode = rightMostNode.GetRightSibling() )
            {
                LeftMost = GetLeftMost( rightMostNode, currentLevel + 1, searchDepth );
            } {}


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

            if( LeftNode != null )
            {
                NodeSize += (float) LeftNode.GetWidth()/2;
            }

            if( RightNode != null )
            {
                NodeSize += (float) RightNode.GetWidth()/2;
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
        private static void InitListofPreviousNodes( )
        {
            PreviousNode tmp;
            for ( tmp = LevelZeroPtr; tmp != null; tmp = tmp.NextLevel )
            {
                tmp.PrevNode = null;
            }
        }

        /// <summary>
        /// Get the previous node at this level
        /// </summary>
        /// <param name="LevelNumber"></param>
        /// <returns></returns>
        private static Node GetPrevNodeAtLevel(int LevelNumber)
        {
            var tmp = LevelZeroPtr;
            uint i = 0;

            for ( tmp = LevelZeroPtr; tmp != null; tmp = tmp.NextLevel )
            {
                if( i++ == LevelNumber )
                {
                    return tmp.PrevNode;
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

            for ( tmp = LevelZeroPtr; tmp != null; tmp = tmp.NextLevel )
            {
                if( i++ == LevelNumber )
                {
                    tmp.PrevNode = ThisNode;
                    return;
                }
                if( tmp.NextLevel == null )
                {
                    NewNode = new PreviousNode();
                    NewNode.PrevNode = null;
                    NewNode.NextLevel = null;
                    tmp.NextLevel = NewNode;
                }
            }

            LevelZeroPtr = new PreviousNode();
            LevelZeroPtr.PrevNode = ThisNode;
            LevelZeroPtr.NextLevel = null;
        }

    }
}