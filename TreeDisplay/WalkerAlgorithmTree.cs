namespace TreeDisplay
{

    /// <summary>
    ///     Uses the John Q. Walker II algorithm: http://www.cs.unc.edu/techreports/89-034.pdf
    ///     Referenced C++ Implementation during troubleshooting:
    ///     http://www.drdobbs.com/positioning-nodes-for-general-trees/184402320
    /// </summary>
    public static class WalkerAlgorithmTree
    {
        private static PreviousNode _levelZeroPointer;
        private static int _xTopAdjustment;
        private static int _yTopAdjustment;
        public static int MaxLevelDepth { private get; set; }
        public static int VerticalLevelSeparation { private get; set; } 
        public static int HorizontalSiblingSeparationFine { private get; set; }
        public static int HorizontalSubtreeSeparationCoarse { private get; set; }

        /// <summary>
        ///     This function determines the coordinates for each
        ///     node in a tree. A pointer to the apex node of the
        ///     tree is passed as input. This assumes that the x
        ///     and y coordinates of the apex node are set as
        ///     desired, since the tree underneath it will be
        ///     positioned with respect to those coordinates.
        /// </summary>
        /// <param name="apexNode">The root node of the tree to be positioned</param>
        /// <returns>TRUE if no errors, otherwise returns FALSE</returns>
        public static bool PositionTree(LeafNode apexNode)
        {
            if( apexNode == null ) return true;

            InitListofPreviousNodes();

            // Do the preliminary positioning with postorder walk.
            FirstWalk( apexNode, 0 );

            // Determine how to adjust all the nodes with respect to
            // the location of the root.
            _xTopAdjustment = apexNode.HorizontalCoordinate - (int) apexNode.PreliminaryLocation;
            _yTopAdjustment = apexNode.VerticalCoordinate;

            // Do the final positioning with a preorder walk.
            var result = SecondWalk( apexNode, 0, 0 );
            if( result )
            {
                apexNode.ExecuteCallback();
            }

            return result;
        }

        private static void FirstWalk(LeafNode currentNode, int currentLevel)
        {
            currentNode.LeftSibling = GetPrevNodeAtLevel( currentLevel );
            SetPrevNodeAtLevel( currentLevel, currentNode );
            currentNode.Modifier = 0;

            if( currentNode.IsLeaf() || (currentLevel == MaxLevelDepth) )
            {
                if( currentNode.HasLeftSibling() )
                {
                    currentNode.PreliminaryLocation = currentNode.GetLeftSibling().PreliminaryLocation 
                        + HorizontalSiblingSeparationFine 
                        + CalcAverageNodeSize( currentNode.GetLeftSibling(), currentNode );
                }
                else
                {
                    currentNode.PreliminaryLocation = 0;
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

                var midpoint = (leftMostNode.PreliminaryLocation + rightMostNode.PreliminaryLocation) / 2;

                if( currentNode.HasLeftSibling() )
                {
                    currentNode.PreliminaryLocation = currentNode.GetLeftSibling().PreliminaryLocation 
                        + HorizontalSiblingSeparationFine 
                        + CalcAverageNodeSize( currentNode.GetLeftSibling(), currentNode );

                    currentNode.Modifier = currentNode.PreliminaryLocation - midpoint;
                    Apportion( currentNode, currentLevel );
                }
                else
                {
                    currentNode.PreliminaryLocation = midpoint;
                }
            }
        }

        private static bool SecondWalk(LeafNode currentNode, int currentLevel, int modsum)
        {
            var result = true;
            long xTemp = 0;
            long yTemp = 0;

            if( currentLevel <= MaxLevelDepth )
            {
                xTemp = _xTopAdjustment + (long) (currentNode.PreliminaryLocation + modsum);
                yTemp = _yTopAdjustment + (long) (currentLevel*(VerticalLevelSeparation + currentNode.Height));
            }

            currentNode.HorizontalCoordinate = (int)xTemp;
            currentNode.VerticalCoordinate = (int)yTemp;

            if (currentNode.HasChild())
            {
                result = SecondWalk(currentNode.GetFirstChild(), currentLevel + 1, (int)(modsum + currentNode.Modifier));
            }

            if (result && currentNode.HasRightSibling())
            {
                result = SecondWalk(currentNode.GetRightSibling(), currentLevel, modsum);
            }

            return result;
        }

        private static void Apportion(LeafNode currentNode, int currentLevel)
        {
            var leftMostNode = currentNode.GetFirstChild();
            var neighbor = leftMostNode.LeftNeighbor();
            uint compareDepth = 1;
            var depthToStop = (uint) (MaxLevelDepth - currentLevel);

            while ( leftMostNode != null && neighbor != null && compareDepth <= depthToStop )
            {
                float leftModsum = 0;
                float rightModsum = 0;

                var ancestorLeftMost = leftMostNode;
                var ancestorNeighbor = neighbor;

                for ( var i = 0; i < compareDepth; i++ )
                {
                    ancestorLeftMost = ancestorLeftMost.GetParent();
                    ancestorNeighbor = ancestorNeighbor.GetParent();
                    rightModsum += ancestorLeftMost.Modifier;
                    leftModsum += ancestorNeighbor.Modifier;
                }

                var distance = neighbor.PreliminaryLocation 
                    + leftModsum 
                    + HorizontalSubtreeSeparationCoarse 
                    + CalcAverageNodeSize( leftMostNode, neighbor ) 
                    - (leftMostNode.PreliminaryLocation + rightModsum);

                if( distance > 0 )
                {
                    uint numLeftSiblings = 0;

                    var tmp = currentNode;
                    while ( tmp != null && tmp != ancestorNeighbor ) 
                    {
                        numLeftSiblings++;
                        tmp = tmp.GetLeftSibling();
                    }

                    if (tmp == null) return;

                    var portion = distance / numLeftSiblings;
                    for (tmp = currentNode; tmp != ancestorNeighbor; tmp = tmp.GetLeftSibling())
                    {
                        tmp.PreliminaryLocation = tmp.PreliminaryLocation + distance;
                        tmp.Modifier = tmp.Modifier + distance;
                        distance -= portion;
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

        private static LeafNode GetLeftMost(LeafNode currentNode, int currentLevel, int searchDepth)
        {
            if( currentLevel == searchDepth ) return currentNode; 
            if( currentNode.IsLeaf() ) return null;
            
            var rightMostNode = currentNode.GetFirstChild();
            var leftMost = GetLeftMost( rightMostNode, currentLevel + 1, searchDepth );

            while ( leftMost == null && rightMostNode.HasRightSibling() ) 
            {
                leftMost = GetLeftMost( rightMostNode, currentLevel + 1, searchDepth );
                rightMostNode = rightMostNode.GetRightSibling();
            } 

            return leftMost;
        }

        private static float CalcAverageNodeSize(LeafNode leftNode, LeafNode rightNode)
        {
            float nodeSize = 0;

            if( leftNode != null )
            {
                nodeSize += (float) leftNode.Width / 2;
            }

            if( rightNode != null )
            {
                nodeSize += (float) rightNode.Width / 2;
            }

            return nodeSize;
        }

        private static void InitListofPreviousNodes( )
        {
            for ( var tmp = _levelZeroPointer; tmp != null; tmp = tmp.NextLevel )
            {
                tmp.PrevNode = null;
            }
        }

        private static LeafNode GetPrevNodeAtLevel(int levelNumber)
        {
            uint i = 0;

            for ( var temp = _levelZeroPointer; temp != null; temp = temp.NextLevel ) 
            {
                if( i++ == levelNumber ) return temp.PrevNode;
            }

            return null;
        }

        private static void SetPrevNodeAtLevel(int levelNumber, LeafNode thisNode)
        {
            uint i = 0;

            for ( var tmp = _levelZeroPointer; tmp != null; tmp = tmp.NextLevel )
            {
                if( i++ == levelNumber )
                {
                    tmp.PrevNode = thisNode;
                    return;
                }

                if( tmp.NextLevel == null )
                {
                    tmp.NextLevel = new PreviousNode
                    {
                        PrevNode = null,
                        NextLevel = null
                    };
                }
            }

            _levelZeroPointer = new PreviousNode
            {
                PrevNode = thisNode,
                NextLevel = null
            };
        }
    }

}