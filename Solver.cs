#pragma warning disable CS8601
#pragma warning disable CS8604

//TODO: Deduction triggers should be added to a queue, and completed only once all the OBVIOUS changes have been made

using System;
using System.Diagnostics;

public static class Solver {

    //Solves the grid, depth first. Prints all solutions.
    public static bool Solve(Grid grid) { //return true if a solution is found
        //Once all deductions exhausted:

        if (grid.numSolved==81) {
            grid.Print(false);
            grid.Print(true);
            grid.PrintCandidates();
            return true;
        } else {
            for (int y = 0; y < 9; y++) {
                for (int x=0; x<9; x++) {
                    Square s = grid.squares[x,y];
                    if (s.GetNum() == null) {
                        for (int n = 0; n < 9; n++) {
                            if (s.HasCandidate(n)) {
                                Grid copy = new Grid(grid);

                                grid.PrintCandidates(grid.squares[x,y]);
                                Console.WriteLine("Assuming digit at " + x + ", " + y);
                                Console.ReadLine();

                                copy.squares[x, y].SetNum(n);
                                if (copy.valid) { //Deductions were valid
                                    if (!Solve(copy)) grid.squares[x, y].TryRemoveCandidate(n); //But if it turns out this had no solutions, remove the candidate
                                } else { //Deductions led to an inconsistency

                                    copy.PrintCandidates();
                                    Console.WriteLine("Assumption failed");
                                    Console.ReadLine();

                                    grid.squares[x, y].TryRemoveCandidate(n);
                                }
                            }
                        }
                    }

                    //TODO: In solver - expand regions with doors


                    /*
                    for (int n=0; n<4; n++) {
                        if (s.GetEdge(n) == Edge.UNDETERMINED) {
                            Grid copy = new Grid(grid);
                            copy.squares[x, y].SetEdge(n, Edge.DOOR);
                            if (copy.valid) { //Check the deductions resulting from the change did not result in a contradiction

                                if (!Solve(copy)) {
                                    s.SetEdge(n, Edge.WALL);
                                    break;
                                }
                            }

                            copy = new Grid(grid);
                            copy.squares[x, y].SetEdge(n, Edge.WALL);
                            if (!Solve(copy)) s.SetEdge(n, Edge.DOOR);
                            
                        }
                    }*/
                }
            }
        }

        return false;

    }


    //Gathers up to 4 members of a walled region, also returns the num. members gathered, and whether it could have grabbed more (overflow)
    public static (Square[], int, bool) GrabWalledRegion(Square s) {
        Square[] seen = new Square[4];
        seen[0] = s;
        int numSeen = 1;
        bool overflow = BoundRegion(s);

        return (seen, numSeen, overflow);

        //Tests if the region is bounded (a singleton or tetromino), or if it is yet to be closed off
        bool BoundRegion(Square s) { //Returns if it overflowed (more than 4 in region)
            for (int i=0; i<4; i++) {
                Square? neighbour = s.GetNeighbour(i);
                if (s.GetEdge(i) != Edge.WALL && !seen.Contains(neighbour)) {
                    if (numSeen >= 4) return true;

                    seen[numSeen] = neighbour;
                    numSeen++;

                    if (BoundRegion(neighbour)) return true;
                }
            }
            return false;
        }
    }

    public static Tetromino ComputeTetrominoShape(Square[] squares) {
        int numCorners = 0;
        foreach (Square s in squares) {
            int numNeighbours = 0;
            for (int i = 0; i < 4; i++) { if (s.GetEdge(i) != Edge.WALL) numNeighbours++; }

            if (numNeighbours == 3) return Tetromino.T; //T
            if (numNeighbours == 2 && (s.GetEdge(0) == Edge.WALL ^ s.GetEdge(2) == Edge.WALL)) numCorners++; //Corner if 2 neighbours exactly one of which is vertical
        }
        switch (numCorners) {
            case 4: return Tetromino.O;
            case 0: return Tetromino.I;
            case 1: return Tetromino.L;
            case 2: return Tetromino.S;
        }

        Debug.Assert(false); //Should never reach this
        return Tetromino.O; 
    }

    //Set region and tetromino shape of all members; add extra doors and walls; test adjacent tetrominos.
    public static void CompleteTetrominoFromDoors(Grid grid, Square origin) { //Origin square is doored into a region of 4.
        //1. Collect members via DFS with list to check repeats
        Square[] squares = new Square[4];
        squares[0] = origin;
        int numSeen = 1;

        void BoundRegion(Square s) { 
            for (int i = 0; i < 4; i++) {
                Square? neighbour = s.GetNeighbour(i);
                if (s.GetEdge(i) == Edge.DOOR && !squares.Contains(neighbour)) {
                    squares[numSeen] = neighbour;
                    numSeen++;
                    BoundRegion(neighbour);
                }
            }
        }

        BoundRegion(origin);

        Tetromino shape = ComputeTetrominoShape(squares);
        foreach (Square s in squares) s.SetTetromino(shape);

        foreach (Square s in squares) { //Add doors and walls and test adjacent tetrominos
            for (int i = 0; i < 4; i++) {
                Square? neighbour = s.GetNeighbour(i);
                if (squares.Contains(neighbour)) {
                    if (s.GetEdge(i) != Edge.DOOR) {
                        s.RAW_SetEdge(i, Edge.DOOR);
                        if (neighbour.GetNum()!=null) s.RemoveNonconsecCandidates((int) neighbour.GetNum());
                    }
                                        } 
                else if (neighbour != null) {
                    if (neighbour.GetTetromino() == shape) {
                        grid.valid = false;
                        return;       
                    }
                    if (s.GetEdge(i) != Edge.WALL) {
                        s.RAW_BuildEdgeBothSides(i, Edge.WALL);
                        neighbour.TestIfNewWallClosesRegionThisSide((i + 2) % 4);
                        Solver.RemoveConsecCandidates(s, neighbour);
                    }
                } 
            }
        }
    }

    //Set region and tetromino shape of all members; add extra doors; test adjacent tetrominos
    public static void CompleteTetrominoFromWalls(Grid grid, Square[] squares) {
        Tetromino shape = ComputeTetrominoShape(squares);
        foreach (Square s in squares) s.SetTetromino(shape);
        
        foreach (Square s in squares) { //Add doors and test adjacent tetrominos
            for (int i=0; i<4; i++) {
                Square? neighbour = s.GetNeighbour(i);
                if (squares.Contains(neighbour)) {
                    if (s.GetEdge(i) != Edge.DOOR) {
                        s.RAW_SetEdge(i, Edge.DOOR);
                        if (neighbour.GetNum() != null) s.RemoveNonconsecCandidates((int)neighbour.GetNum());
                    }
                } else if (neighbour != null && neighbour.GetTetromino() == shape) {
                    grid.valid = false;
                    return;
                }
            }
        }
    }

    public static void RemoveConsecCandidates(Square a, Square b) {
        if (a.GetNum() != null) b.RemoveConsecCandidates((int) a.GetNum());
        if (b.GetNum() != null) a.RemoveConsecCandidates((int) b.GetNum());
    }
}