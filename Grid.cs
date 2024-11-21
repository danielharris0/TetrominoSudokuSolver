using System.Diagnostics;

public enum GridError {NO_ERROR, resolvedNumContradictedConsecutivityRule, wallCompletedTetrominoTouchesAnother, doorCompletedTetrominoTouchesAnother, noCandidates, wallEnclosedRegionOfTwoOrThree, combinedDoorsRegionTooLarge };

public class Grid {
    public Square[,] squares = new Square[9, 9];
    public int numSolved = 0;
    public GridError error = GridError.NO_ERROR;

    //Copy Constructor
    public Grid(Grid parent) {
        for (int x = 0; x < 9; x++) {
            for (int y = 0; y < 9; y++) squares[x, y] = new Square(this, parent.squares[x,y]);
        }
        numSolved = parent.numSolved;
    }

    public int CountSingletons() {
        int count = 0;
        for (int x = 0; x < 9; x++) {
            for (int y = 0; y < 9; y++) {
                if (squares[x, y].GetRegionStatus() == RegionStatus.SINGLETON) count++;
            }
        }
        return count;
    }

    public void PrintCandidates(Square? highlightSquare = null) {

        for (int y = 0; y < 9; y++) {
            for (int subY = 0; subY < 3; subY++) {
                for (int x = 0; x < 9; x++) {
                    Square s = squares[x, y];

                    if (subY == 0) {

                        // Console.ForegroundColor = ConsoleColor.DarkYellow; Console.Write(s.sizeOfDooredRegion);
                        Console.Write(" ");

                        switch (s.GetEdge(0)) {
                            case Edge.WALL: Console.ForegroundColor = ConsoleColor.Red; Console.Write("--- "); break;
                            case Edge.DOOR: Console.ForegroundColor = ConsoleColor.Green; Console.Write(" !  "); break;
                            case Edge.UNDETERMINED: Console.Write("    "); break;
                        }
                    }
                    else {
                        if (s.GetEdge(3) == Edge.WALL) {Console.ForegroundColor = ConsoleColor.Red; Console.Write('|');}
                        else if (s.GetEdge(3) == Edge.DOOR && subY==1) { Console.ForegroundColor = ConsoleColor.Green; Console.Write('~');}
                        else Console.Write(' ');

                        Console.ForegroundColor = s==highlightSquare ? ConsoleColor.Magenta : ConsoleColor.White;

                        if (s.GetNumCandidates() == 9) {
                            Console.Write("XXXX");
                        } else {
                            int n = 0; int i = 0;
                            while (i<9) {
                                if (s.HasCandidate(i)) {
                                    if (subY==1) {
                                        if (n < 4) {
                                            Console.Write(i + 1);
                                            n++;
                                        }
                                    } else {
                                        if (n >= 4) Console.Write(i + 1);
                                        n++;
                                    }                                    
                                }
                                i++;
                            }
                            Console.Write(new string(' ', subY==1 ? 4-n : 8-Math.Clamp(n, 4, 8)));
                        }

                        Debug.Assert(error!=GridError.NO_ERROR || s.x == 8 || s.GetEdge(1) == s.GetNeighbour(1).GetEdge(3));
                        if (x==8) { Console.ForegroundColor = ConsoleColor.Red; Console.Write('|'); }
                    }

                    Debug.Assert(error != GridError.NO_ERROR || s.y == 8 || s.GetEdge(2) == s.GetNeighbour(2).GetEdge(0));
                }
                Console.Write("\n");
            }
        }
        Console.ForegroundColor = ConsoleColor.Red; Console.Write(" ---  ---  ---  ---  ---  ---  ---  ---  ---  ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine();
        //Console.ReadLine();
    }

    public void Print(bool showShapes) {
        for (int y = 0; y < 9; y++) {
            for (int x = 0; x < 9; x++) {
                Square s = squares[x, y];

                if (showShapes) {
                    RegionStatus r = s.GetRegionStatus();
                    if (r == RegionStatus.TETROMINO) Console.Write(s.GetTetromino());
                    else if (r == RegionStatus.SINGLETON) Console.Write('.');
                    else Console.Write(' ');
                }
                //Show Single Candidates
                else {
                    if (s.GetNum()!=null) Console.Write(s.GetNum() + 1);
                    else Console.Write(' ');
                }


            }
            Console.Write("\n");
        }
        Console.ReadLine();
    }

    public Grid(string numbers, string tetrominos) {
        for (int x = 0; x < 9; x++) {
            for (int y = 0; y < 9; y++) squares[x, y] = new Square(this, x, y);
        }

        parseNumbers();
        int[,] regionMap = parseTetrominos();


        for (int x = 0; x < 9; x++) {
            for (int y = 0; y < 9; y++) {
                squares[x, y].SetEdgesViaRegionMap(regionMap);
            }
        }

        void parseNumbers() { 
            int x = 0; int y = 0;
            foreach (char c in numbers) {
                if (c == '/') { x = 0; y++; }
                else {
                    if (c != '?') {
                        int num = c - '0';
                        squares[x, y].SetNum(num - 1);
                    }
                    x++;
                }
            }
        }
        

        int[,] parseTetrominos() {
            int[,] regionMap = new int[9, 9];
            int x = 0; int y = 0;
            foreach (char c in tetrominos) {
                if (c == '/') {
                    x = 0; y++;
                }
                else {
                    if (c != '?') regionMap[x, y] = c - '0';
                    x++;
                }
            }
            return regionMap;
        }

    }
}