using System.Diagnostics;

public enum RegionStatus { UNDETERMINED, SINGLETON, PARTIAl_TETROMINO, TETROMINO}
public enum Tetromino { T, O, I, L, S };
public enum Edge { UNDETERMINED, DOOR, WALL };

public partial class Square {
	Grid grid;
	public int x; public int y;

	int? num; //Big digit
	bool[] candidates;
	int numCandidates;

	public int sizeOfDooredRegion; //num cells in a group connected by doors
	Edge[] edges;
	RegionStatus regionStatus;
	Tetromino? tetromino; //only relevent if regionStatus==TETROMINO

	public Square(Grid grid, int x, int y) {
		this.grid = grid;
		this.x = x; this.y = y;

		num = null;
		candidates = new bool[9]; for (int i = 0; i < 9; i++) candidates[i] = true;
		edges = new Edge[4];
		regionStatus = RegionStatus.UNDETERMINED;
		numCandidates = 9;
		sizeOfDooredRegion = 1;

		if (y == 0) edges[0] = Edge.WALL;
		if (x == 8) edges[1] = Edge.WALL;
		if (y == 8) edges[2] = Edge.WALL;
		if (x == 0) edges[3] = Edge.WALL;
	}

	//Copy Constructor
	public Square(Grid grid, Square parent) {
		this.grid = grid;

		num = parent.num;
		x = parent.x; y = parent.y;
		numCandidates = parent.numCandidates;
		sizeOfDooredRegion = parent.sizeOfDooredRegion;

		tetromino = parent.tetromino;

		candidates = new bool[9];
		parent.candidates.CopyTo(candidates, 0);

		edges = new Edge[4];
		parent.edges.CopyTo(edges, 0);

		regionStatus = parent.regionStatus;

	}

}