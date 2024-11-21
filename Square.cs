#pragma warning disable CS8600
#pragma warning disable CS8602
using System.Diagnostics;

public partial class Square {

    public bool HasCandidate(int n) => candidates[n];
    public void TryRemoveCandidate(int n) {
        if (candidates[n]) { //Candidate actually removed
            numCandidates--;

            candidates[n] = false;
            if (numCandidates == 1) {
                for (int i = 0; i < 9; i++) {
                    if (candidates[i]) {
                        num = i;
                        OnNumResolved(i);
                        break;
                    }
                }
            }

            if (numCandidates == 0) grid.error = GridError.noCandidates;
        }
    }
    public void RemoveConsecCandidates(int num) {
        if (num > 0) TryRemoveCandidate(num - 1);
        if (num < 8) TryRemoveCandidate(num + 1);
    }
    public void RemoveNonconsecCandidates(int num) {
        for (int i = 0; i < 9; i++) {
            if (Math.Abs(num - i) != 1) TryRemoveCandidate(i);
        }
    }

    public void RAW_BuildEdgeBothSides(int edgeNum, Edge edge) {
        edges[edgeNum] = edge;
        Square? neighbour = GetNeighbour(edgeNum);
        if (neighbour != null) neighbour.edges[(edgeNum + 2) % 4] = edge;
    }


    //Should only resolve from UNDETERMINED --> DOOR/WALL
    public void OnNumsDecideNewEdge(int edgeNum, Edge edge) {
        RAW_BuildEdgeBothSides(edgeNum, edge);
        Square? neighbour = GetNeighbour(edgeNum);
        
        switch (edge) {
            case Edge.WALL: //Neighbour on other side of wall
                TestIfNewWallClosesRegionThisSide(edgeNum);
                if (neighbour != null) neighbour.TestIfNewWallClosesRegionThisSide((edgeNum + 2) % 4);
                break;
            case Edge.DOOR:
                regionStatus = RegionStatus.PARTIAl_TETROMINO;
                neighbour.regionStatus = RegionStatus.PARTIAl_TETROMINO;
                CombineRegionsWithDoor(edgeNum); //Neighbour within same region (so only check once!)
                break;
            default:
                Debug.Assert(false); //Should never be Edge.UNDETERMINED
                break;
        }                       
    }

    public void TestIfNewWallClosesRegionThisSide(int edgeNum) {
        if (grid.error != GridError.NO_ERROR) return;

        //Has this wall now totally enclosed a region?
        (Square[] members, int numMembers, bool overflowed) = Solver.GrabWalledRegion(this);
        if (!overflowed) {
            if (numMembers == 1) regionStatus = RegionStatus.SINGLETON;
            else if (numMembers < 4) {
                grid.error = GridError.wallEnclosedRegionOfTwoOrThree;
            } else Solver.CompleteTetrominoFromWalls(grid, members);         
        }
    }

    void CombineRegionsWithDoor(int edgeNum) {
        if (grid.error != GridError.NO_ERROR) return;

        //Compute combined region size
        Square neighbour = GetNeighbour(edgeNum);
        int combinedSize = sizeOfDooredRegion + neighbour.sizeOfDooredRegion;

        if (combinedSize > 4) grid.error = GridError.combinedDoorsRegionTooLarge;
        else {
            //Propogate new region size
            Square[] seen = new Square[4]; int numSeen = 0;
            void propogate(Square s) {
                seen[numSeen] = s; numSeen++;
                s.sizeOfDooredRegion = combinedSize;
                for (int i=0; i<4; i++) {
                    if (s.GetEdge(i)==Edge.DOOR) {
                        Square neighbour = s.GetNeighbour(i);
                        if (!seen.Contains(neighbour)) propogate(neighbour);
                    }
                }
            }
            propogate(this);


            if (combinedSize == 4) { //Thus this must be a full tetromino
                Solver.CompleteTetrominoFromDoors(grid, this);
            }
        }
    }

    public void AssumeWall(int edgeNum) {
        RAW_BuildEdgeBothSides(edgeNum, Edge.WALL);
        Square neighbour = GetNeighbour(edgeNum);
        TestIfNewWallClosesRegionThisSide(edgeNum);
        neighbour.TestIfNewWallClosesRegionThisSide((edgeNum + 2) % 4);
        Solver.RemoveConsecCandidates(this, neighbour);
    }

    public void RAW_SetEdge(int edgeNum, Edge edge) { edges[edgeNum] = edge; }


    public void SetNum(int num) {
        Debug.Assert(HasCandidate(num));

        for (int i=0; i<9; i++) {
            if (i!=num) TryRemoveCandidate(i);
        }
    }
    public int GetNumCandidates() => numCandidates;
    public int? GetNum() => num;
    public Edge GetEdge(int edgeNum) => edges[edgeNum];
    public RegionStatus GetRegionStatus() => regionStatus;
    public Tetromino? GetTetromino() => tetromino;

    public void SetTetromino(Tetromino shape) {
        regionStatus = RegionStatus.TETROMINO;
        tetromino = shape;
    }

    public Square? GetNeighbour(int edgeNum) {
        Square? square = null;
        switch (edgeNum) {
            case 0: square = y>0 ? grid.squares[x, y - 1] : null; break;
            case 1: square = x<8 ? grid.squares[x + 1, y] : null; break;
            case 2: square = y<8 ? grid.squares[x, y + 1] : null; break;
            case 3: square = x>0 ? grid.squares[x - 1, y] : null; break;
        }
        //neighbours[edgeNum] = (square, true);
        return square;

    }

    void OnNumResolved(int num) {
        if (grid.error != GridError.NO_ERROR) return;
        grid.numSolved++;

        //Remove Candidates from Rows and Cols
        for (int x = 0; x < 9; x++) { if (x != this.x) grid.squares[x, y].TryRemoveCandidate(num); }
        for (int y = 0; y < 9; y++) { if (y != this.y) grid.squares[x, y].TryRemoveCandidate(num); }
        //TODO: i think somehow the 9-3 edge is not being detected, maybe cause they're both full nums - or possibly a zero-candidate situation emerges which breaks certain assumptions: I should check and see what that 'should never reach here' error was on about
        //Apply consecutivity edge rule
        for (int i=0; i<4; i++) {

            Edge edgeDeduction = deduceEdge(i);
            Edge currentEdge = GetEdge(i);

            if (edgeDeduction != Edge.UNDETERMINED) {
                if (currentEdge == Edge.UNDETERMINED) {
                    //This number implies a previously unresolved edge
                    OnNumsDecideNewEdge(i, edgeDeduction);
                } else if (currentEdge != edgeDeduction) {
                    //Test that the implied edge matches the existing edge
                    grid.error = GridError.resolvedNumContradictedConsecutivityRule;
                }
            } else if (currentEdge != Edge.UNDETERMINED) {
                //Now we know both the edge and the number - so we can enforce consecutivity on neighouring candidates
                if (currentEdge == Edge.WALL) GetNeighbour(i).RemoveConsecCandidates(num);
                if (currentEdge == Edge.DOOR) GetNeighbour(i).RemoveNonconsecCandidates(num);
            }
        }


        Edge deduceEdge(int edgeDir) { //Given I have just resoled num, check my neighbours candidates
            Square? neighbour = GetNeighbour(edgeDir);
            if (neighbour == null) return Edge.WALL;

            int consec = 0; int nonconsec = 0;
            for (int i =0; i<9; i++) {
                if (neighbour.HasCandidate(i)) {
                    if (Math.Abs(i - num) == 1) consec++;
                    else nonconsec++;
                }
            }
            if (consec == 0) return Edge.WALL;
            if (nonconsec == 0) return Edge.DOOR;
            return Edge.UNDETERMINED;
        }
    }


    public void SetEdgesViaRegionMap(int[,] regionMap) {
        int n = regionMap[x, y];
        if (y > 0 && regionMap[x, y - 1] != n) { AssumeWall(0); }
        if (x < 8 && regionMap[x + 1, y] != n) { AssumeWall(1); }
        if (y < 8 && regionMap[x, y + 1] != n) { AssumeWall(2); }
        if (x > 0 && regionMap[x - 1, y] != n) { AssumeWall(3); }
    }

}