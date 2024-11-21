internal class Program {

    static void Main(string[] args) {
        Grid grid = new Grid("", "1222/112/314444/333");
        grid.squares[5, 2].SetNum(0);

        grid.PrintCandidates();

        int n = Solver.Solve(grid);
        Console.WriteLine("Num Solutions: " + n);
        Console.ReadKey();
    }

}

