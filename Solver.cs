#pragma warning disable CS8601
#pragma warning disable CS8604

//TODO: Deduction triggers should be added to a queue, and completed only once all the OBVIOUS changes have been made

using System;
using System.Diagnostics;

public static class Solver {

    static int mostTetrominosInSolution = 0;
    static DateTime startTime = DateTime.Now;

    //TODO: Assumptions to expand regions with doors


    //Solves the grid, depth first. Prints all solutions.
    public static int Solve(Grid grid) { //Returns the number of solutions.
        if (grid.numSolved==81) {
            Console.WriteLine("Solved It!");

            int numSingletons = grid.CountSingletons();
            Debug.Assert((81 - numSingletons) % 4 == 0);
            int numTetrominos = (81 - numSingletons) / 4;

            Console.WriteLine("Num Tetrominos: " + numTetrominos);
            Console.WriteLine("SingletonCoverage: " + (100 * numSingletons / 81) + "%");

           // grid.Print(false);
           // grid.Print(true);
           if (numTetrominos >= mostTetrominosInSolution) {
                mostTetrominosInSolution = numTetrominos;
                grid.PrintCandidates();
            }

           if (mostTetrominosInSolution==13) {
                Console.WriteLine(DateTime.Now - startTime);
                Console.Read();
            }
            
            return 1;
        } else {

            //Find first cell which has >1 candidate. Then guess all possible candidates. Then return.
            for (int y = 0; y < 9; y++) {
                for (int x=0; x<9; x++) {
                    Square s = grid.squares[x,y];
                    if (s.GetNum() == null) {
                        //This cell has >1 candidates. This is the one we will be making assumptions about.

                        int numSolutions = 0;

                        for (int n = 0; n < 9; n++) {
                            if (s.HasCandidate(n)) {
                                Grid copy = new Grid(grid);

                               // grid.PrintCandidates(grid.squares[x,y]);
                              //  Console.WriteLine("Assuming digit at " + x + ", " + y);
                                //  Console.ReadLine();
                                copy.squares[x, y].SetNum(n); //Make an assumption. The system automatically makes any follow-on deductions.
                                if (copy.error == GridError.NO_ERROR) {
                                    //Deductions led to a valid grid
                                    int numSolutionsFromAssumption = Solve(copy);
                                    numSolutions += numSolutionsFromAssumption;
                                    //Future Optimisation: if we don't make all assumptions about the same cell, we may want to remove n as a candidate for this cell if numSolutionsFromAssumption==0 here.
                                    //  But be sure to check if this removal itself leads to an invalid grid.

                                } else {
                                    //Deductions led to an inconsistency
                                   // copy.PrintCandidates();
                                   // Console.WriteLine("Assumption failed: " + copy.error);
                                    //Console.ReadLine();

                                    //Future Optimisation: if we don't make all assumptions about the same cell, we may want to remove n as a candidate for this cell if numSolutions==0 here.
                                    //  But be sure to check if this removal itself leads to an invalid grid.
                                }
                            }
                        }
                        return numSolutions;
                    }
                }
            }
        }

        Debug.Assert(false);
        return 0; //Should never get here: all squares have <2 candidates implies all nums found.
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

    public static Tetromino ComputeTetrominoShapeFromWalls(Grid grid, Square[] squares) {
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

        grid.PrintCandidates();
        Debug.Assert(false); //Should never reach this
        return Tetromino.O; 
    }

    //Set region and tetromino shape of all members; add extra doors and walls; test adjacent tetrominos
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

        foreach (Square s in squares) { //Add doors and walls
            for (int i = 0; i < 4; i++) {
                Square? neighbour = s.GetNeighbour(i);
                if (squares.Contains(neighbour)) { //Neighbour in tetromino
                    if (s.GetEdge(i) != Edge.DOOR) { //Add additional doors (e.g. in the O tetromino case formed by doors)
                        s.RAW_SetEdge(i, Edge.DOOR);
                        if (neighbour.GetNum()!=null) s.RemoveNonconsecCandidates((int) neighbour.GetNum()); //When a door is set (not by nums) on the side of a square with a whole num, then we enforce consecutivity
                    }
                } 
                else if (neighbour != null) { //Neighbour not in tetromino
                    if (s.GetEdge(i) != Edge.WALL) { //Add walls
                        s.RAW_BuildEdgeBothSides(i, Edge.WALL);
                        neighbour.TestIfNewWallClosesRegionThisSide((i + 2) % 4); //Only need to test region closure on the other side (this tetromino is deffo closed)
                        Solver.RemoveConsecCandidates(s, neighbour);
                    }
                } 
            }
        }

        if (grid.error != GridError.NO_ERROR) return;

        Tetromino shape = ComputeTetrominoShapeFromWalls(grid, squares);
        foreach (Square s in squares) s.SetTetromino(shape);

        //Test adjacent tetrominos
        foreach (Square s in squares) {
            for (int i = 0; i < 4; i++) {
                Square? neighbour = s.GetNeighbour(i);
                if (neighbour!=null && neighbour.GetTetromino() == shape && !squares.Contains(neighbour)) {
                    grid.error = GridError.doorCompletedTetrominoTouchesAnother;
                    return;
                }
            }
        }
 
    }

    //Set region and tetromino shape of all members; add extra doors; test adjacent tetrominos
    public static void CompleteTetrominoFromWalls(Grid grid, Square[] squares) {
        //Add doors
        foreach (Square s in squares) { 
            for (int i=0; i<4; i++) {
                Square? neighbour = s.GetNeighbour(i);
                if (squares.Contains(neighbour)) {
                    if (s.GetEdge(i) != Edge.DOOR) {
                        s.RAW_SetEdge(i, Edge.DOOR);
                        if (neighbour.GetNum() != null) s.RemoveNonconsecCandidates((int)neighbour.GetNum());
                    }
                }
            }
        }

        if (grid.error != GridError.NO_ERROR) return;


        Tetromino shape = ComputeTetrominoShapeFromWalls(grid, squares);
        foreach (Square s in squares) s.SetTetromino(shape);

        //Test adjacent tetrominos
        foreach (Square s in squares) { //Add doors
            for (int i = 0; i < 4; i++) {
                Square? neighbour = s.GetNeighbour(i);
                if (!squares.Contains(neighbour) && neighbour != null && neighbour.GetTetromino() == shape) {
                    grid.error = GridError.wallCompletedTetrominoTouchesAnother;
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