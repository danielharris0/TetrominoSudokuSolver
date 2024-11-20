internal class Program {

    static void Main(string[] args) {
        Grid grid = new Grid("5234/452/864321/7891/14567/??7", "7111/771/273333/2224/56666/???");
        Solver.Solve(grid);
    }

}

