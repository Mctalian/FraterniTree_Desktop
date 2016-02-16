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
        public static bool PositionTree(Node apexNode)
        {
            if( apexNode == null ) return true;

            InitListofPreviousNodes();

            // Do the preliminary positioning with postorder walk.
            FirstWalk( apexNode, 0 );

            // Determine how to adjust all the nodes with respect to
            // the location of the root.
            _xTopAdjustment = apexNode.CoordinateX - (int) apexNode.Prelim;
            _yTopAdjustment = apexNode.CoordinateY;

            // Do the final positioning with a preorder walk.
            var result = SecondWalk( apexNode, 0, 0 );
            if( result )
            {
                apexNode.ExecuteCallback();
            }

            return result;
        }

        private static void FirstWalk(Node currentNode, int currentLevel)
        {
            currentNode.Prev = GetPrevNodeAtLevel( currentLevel );
            SetPrevNodeAtLevel( currentLevel, currentNode );
            currentNode.Modifier = 0;

            if( currentNode.IsLeaf() || (currentLevel == MaxLevelDepth) )
            {
                if( currentNode.HasLeftSibling() )
                {
                    currentNode.Prelim = currentNode.GetLeftSibling().Prelim 
                        + HorizontalSiblingSeparationFine 
                        + MeanNodeSize( currentNode.GetLeftSibling(), currentNode );
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

                var midpoint = (leftMostNode.Prelim + rightMostNode.Prelim) / 2;

                if( currentNode.HasLeftSibling() )
                {
                    currentNode.Prelim = currentNode.GetLeftSibling().Prelim 
                        + HorizontalSiblingSeparationFine 
                        + MeanNodeSize( currentNode.GetLeftSibling(), currentNode );

                    currentNode.Modifier = currentNode.Prelim - midpoint;
                    Apportion( currentNode, currentLevel );
                }
                else
                {
                    currentNode.Prelim = midpoint;
                }
            }
        }

        private static bool SecondWalk(Node currentNode, int currentLevel, int modsum)
        {
            var result = true;
            long xTemp = 0;
            long yTemp = 0;

            if( currentLevel <= MaxLevelDepth )
            {
                xTemp = _xTopAdjustment + (long) (currentNode.Prelim + modsum);
                yTemp = _yTopAdjustment + (long) (currentLevel*(VerticalLevelSeparation + currentNode.GetHeight()));
            }

            if( !CheckExtentRange( (int) xTemp, (int) yTemp ) ) return false;

            currentNode.CoordinateX = (int)xTemp;
            currentNode.CoordinateY = (int)yTemp;

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

        private static void Apportion(Node currentNode, int currentLevel)
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
                    ancestorLeftMost = ancestorLeftMost.Parent();
                    ancestorNeighbor = ancestorNeighbor.Parent();
                    rightModsum += ancestorLeftMost.Modifier;
                    leftModsum += ancestorNeighbor.Modifier;
                }

                var distance = neighbor.Prelim 
                    + leftModsum 
                    + HorizontalSubtreeSeparationCoarse 
                    + MeanNodeSize( leftMostNode, neighbor ) 
                    - (leftMostNode.Prelim + rightModsum);

                if( distance > 0 )
                {
                    uint numLeftSiblings = 0;
                    Node tmp;

                    for ( tmp = currentNode; tmp != null && tmp != ancestorNeighbor; tmp = tmp.GetLeftSibling() ) {
                        numLeftSiblings++;
                    }

                    if (tmp == null) return;

                    var portion = distance / numLeftSiblings;
                    for (tmp = currentNode; tmp != ancestorNeighbor; tmp = tmp.GetLeftSibling())
                    {
                        tmp.Prelim = tmp.Prelim + distance;
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

        private static Node GetLeftMost(Node currentNode, int currentLevel, int searchDepth)
        {
            if( currentLevel == searchDepth ) return currentNode; 
            if( currentNode.IsLeaf() ) return null; 

            Node leftMost;
            var rightMostNode = currentNode.GetFirstChild();

            for ( leftMost = GetLeftMost( rightMostNode, currentLevel + 1, searchDepth ); leftMost == null && rightMostNode.HasRightSibling();
                rightMostNode = rightMostNode.GetRightSibling() ) 
            {
                leftMost = GetLeftMost( rightMostNode, currentLevel + 1, searchDepth );
            } 

            return leftMost;
        }

        private static float MeanNodeSize(Node leftNode, Node rightNode)
        {
            float nodeSize = 0;

            if( leftNode != null )
            {
                nodeSize += (float) leftNode.GetWidth()/2;
            }

            if( rightNode != null )
            {
                nodeSize += (float) rightNode.GetWidth()/2;
            }

            return nodeSize;
        }

        private static bool CheckExtentRange(int xValue, int yValue)
        {
            return true;
        }

        private static void InitListofPreviousNodes( )
        {
            PreviousNode tmp;
            for ( tmp = _levelZeroPointer; tmp != null; tmp = tmp.NextLevel )
            {
                tmp.PrevNode = null;
            }
        }

        private static Node GetPrevNodeAtLevel(int levelNumber)
        {
            PreviousNode temp;
            uint i = 0;

            for ( temp = _levelZeroPointer; temp != null; temp = temp.NextLevel ) 
            {
                if( i++ == levelNumber ) return temp.PrevNode;
            }

            return null;
        }

        private static void SetPrevNodeAtLevel(int levelNumber, Node thisNode)
        {
            PreviousNode tmp;
            uint i = 0;

            for ( tmp = _levelZeroPointer; tmp != null; tmp = tmp.NextLevel )
            {
                if( i++ == levelNumber )
                {
                    tmp.PrevNode = thisNode;
                    return;
                }

                if( tmp.NextLevel == null )
                {
                    var newNode = new PreviousNode
                    {
                        PrevNode = null,
                        NextLevel = null
                    };
                    tmp.NextLevel = newNode;
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