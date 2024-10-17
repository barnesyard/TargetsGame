// See https://aka.ms/new-console-template for more information
using System;
using System.Diagnostics;
using System.Dynamic;
using System.Numerics;

namespace TargetsGameApp;

class Program
{
    static void Main()
    {
        // First randomly specify the size of the grid
        Random theRandom = new Random();
        int gridSize = theRandom.Next(7, 11);
        Console.WriteLine("Creating a grid of size {0}", gridSize);

        Grid theGrid = new(gridSize);
        theGrid.GenerateGrid();

        // Enable Unicode output
        Console.OutputEncoding = System.Text.Encoding.UTF8;


    }
}

public class Coordinate
{
    private int Row { get; set; }
    private int Col { get; set; }

    public Coordinate(int row, int col)
    {
        Row = row;
        Col = col;
    }

    public Coordinate Up() => new Coordinate(Row - 1, Col);
    public Coordinate Right() => new Coordinate(Row, Col + 1);
    public Coordinate Down() => new Coordinate(Row + 1, Col);
    public Coordinate Left() => new Coordinate(Row, Col - 1);

    public void SetRow(int row)
    {
        this.Row = row;
    }
    public void SetCol(int col)
    {
        this.Col = col;
    }
    public int GetRow()
    {
        return this.Row;
    }
    public int GetCol()
    {
        return this.Col;
    }
}

public class Cell
{
    private int RegionId { get; set; }
    private Coordinate Coordinate { get; set; }
    private bool IsTarget { get; set; }

    public Cell()
    {
        this.Coordinate = new Coordinate(0, 0);
        this.IsTarget = false;
        this.RegionId = -1;
    }

    public Cell(int row, int col)
    {
        this.Coordinate = new Coordinate(row, col);
        this.IsTarget = false;
        this.RegionId = -1;
    }

    public void SetCoordinate(int row, int col)
    {
        this.Coordinate = new Coordinate(row, col);
    }

    public void SetIsTarget(bool isTarget)
    {
        this.IsTarget = isTarget;
    }
    public void SetRegionId(int regionId)
    {
        this.RegionId = regionId;
    }
    public Coordinate GetCoordinate()
    {
        return this.Coordinate;
    }
    public bool GetIsTarget()
    {
        return this.IsTarget;
    }
    public int GetRegionId()
    {
        return this.RegionId;
    }

    public static Cell operator +(Cell cell, Coordinate coord)
    {
        return new Cell(cell.Coordinate.GetRow() + coord.GetRow(), cell.Coordinate.GetCol() + coord.GetCol());
    }
}

// A region refers to a set of cells that are orthoganally adjacent 
// the number of regions is equal to the GridSize, one region per target
public class Region
{
    public List<Cell> Cells { get; private set; } = new List<Cell>();

    public void Add(Cell cell)
    {
        Cells.Add(cell);
    }
}

public class Grid
{
    private int GridSize { get; }
    private List<int> TargetLocations { get; set; }

    public List<List<Cell>> GridCells { get; private set; }

    public Grid(int size)
    {
        this.GridSize = size;
        this.GridCells = [];
        this.TargetLocations = [];
        //initialize target locations values to 0 thru GridSize (which is a totally invalid arrangement)
        for (int i = 0; i < this.GridSize; i++)
        {
            this.TargetLocations.Add(i);
            this.GridCells.Add([]);
            // add initial cell with default region id to each cell
            for (int j = 0; j < this.GridSize; j++)
            {
                Cell tempCell = new(i, j);
                this.GridCells[i].Add(tempCell);
            }
        }
    }

    private void WritePositions(int[] positions, ConsoleColor color)
    {
        // Writing in different colors so track the original color
        ConsoleColor originalForegroundColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        for (int i = 0; i < positions.Length; i++)
        {
            Console.Write("{0}", positions[i]);
        }
        Console.WriteLine();
        // Done writing in color so reset to original color
        Console.ForegroundColor = originalForegroundColor;
    }

    private void WriteGrid()
    {
        // Create an array of ConsoleColor values that can be used for the regions
        ConsoleColor[] regionColors = [
            ConsoleColor.DarkYellow,
            ConsoleColor.Green,
            ConsoleColor.Red,
            ConsoleColor.Yellow,
            ConsoleColor.Magenta,
            ConsoleColor.Cyan,
            ConsoleColor.DarkMagenta,
            ConsoleColor.Gray,
            ConsoleColor.DarkCyan,
            ConsoleColor.Blue];

        // Enable Unicode output
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        ConsoleColor originalForegroundColor = Console.ForegroundColor;
        for (int i = 0; i < GridSize; i++)
        {
            for (int j = 0; j < GridSize; j++)
            {
                // Set the color based on the region ID
                if (this.GridCells[i][j].GetRegionId() != -1)
                {
                    Console.ForegroundColor = regionColors[this.GridCells[i][j].GetRegionId()];
                }
                Console.Write(this.GridCells[i][j].GetIsTarget() ? '\u233e' : '\u2610');
                Console.ForegroundColor = originalForegroundColor;
            }
            Console.WriteLine();
        }
        Console.ForegroundColor = originalForegroundColor;
        Console.WriteLine();
    }

    public void GenerateGrid()
    {
        // print out the array so we can see the "before"
        this.WritePositions(this.TargetLocations.ToArray(), ConsoleColor.Green);
        Console.WriteLine("Shuffling the positions");

        // for each row of the grid assign a random column where the target can be found
        this.TargetLocations = this.AssignTargetLocations(this.GridSize);

        // print out the array so we can see the "after"
        this.WritePositions(this.TargetLocations.ToArray(), ConsoleColor.Red);

        // sort the rows by distance from center to allow region generation to start with targets closest to center
        List<int> rowsByDist = SortRowsByDistTargetIsFromCenter(this.TargetLocations);

        // update grid with target locations and set region IDs based on dist from center
        for (int i = 0; i < GridSize; i++)
        {
            this.GridCells[rowsByDist[i]][this.TargetLocations[rowsByDist[i]]].SetIsTarget(true);
            this.GridCells[rowsByDist[i]][this.TargetLocations[rowsByDist[i]]].SetRegionId(i);
        }

        // Write the grid to the screen
        this.WriteGrid();

        List<Region> regions = GenerateRegions(rowsByDist, this.GridCells);

    }

    // Q: when do you choose to update members of the class vs returning values?
    public List<Region> GenerateRegions(List<int> rowsByDist, List<List<Cell>> gridCells)
    {
        // Random size per region
        // total count of cells in all regions = GridSize * GridSize
        // start with a target closest to center and work outwards
        // track which targets are assigned a region

        // Create a list of rows ordered by distance of target from center
        //throw new NotImplementedException("Not implemented yet!");
        List<Region> regions = [];
        for (int rg = 0; rg < rowsByDist.Count; rg++)
        {
            // pass the cell coordinate into the command to generate a region along with the grid.
            // Find the cell in the row that has the target
            Cell targetCell = gridCells[rowsByDist[rg]][TargetLocations[rowsByDist[rg]]];
            Region region = GenerateRegion(targetCell, gridCells);
            regions.Add(region);

            // Update the grid with the region IDs, the approach to this needs to be reconsidered
            foreach (Cell cell in region.Cells)
            {
                gridCells[cell.GetCoordinate().GetRow()][cell.GetCoordinate().GetCol()].SetRegionId(targetCell.GetRegionId());
            }

            this.WriteGrid();
        }
        return regions;
    }

    /// <summary>
    /// Generate a region of random size grown from target location
    /// </summary>
    /// <param name="row"></param>
    /// <param name="cells"></param>
    /// <returns></returns>
    public Region GenerateRegion(Cell targetCell, List<List<Cell>> gridCells)
    {
        Region region = new Region();
        // The region starts with the targetCell so add it first
        region.Add(targetCell);

        // Set the size of the grid using the number of cells in a row
        int gridSize = gridCells[0].Count;

        // The region will a set of cells that are orthogonally adjacent
        // The region will be a random size that will depend on status of the grid
        // The initial region size will random between 2 and the gridSize
        Random random = new Random();
        int regionSize = random.Next(2, gridSize);

        int currentSize = 1;

        List<Cell> adjacentCells = GetValidOrthogonalAdjacentCells(targetCell);

        while (currentSize < regionSize && adjacentCells.Count > 0)
        {
            // Select a random cell from the list of VALID adjacent cells
            int cellIndex = random.Next(adjacentCells.Count);
            Cell selectedCell = adjacentCells[cellIndex];
            selectedCell.SetRegionId(targetCell.GetRegionId());
            region.Add(selectedCell);

            // Remove the selected cell from the list of valid adjacent cells since it has been added to the region 
            adjacentCells.RemoveAt(cellIndex);
            // Get the list of valid adjacent cells for the selected cell and add them to the list
            adjacentCells.AddRange(GetValidOrthogonalAdjacentCells(selectedCell));

            currentSize++;
        }

        return region;
    }

    private List<Cell> GetValidOrthogonalAdjacentCells(Cell cell)
    {
        List<Cell> adjacentCells = [];
        Coordinate[] directions = [cell.GetCoordinate().Up(), cell.GetCoordinate().Right(), cell.GetCoordinate().Down(), cell.GetCoordinate().Left()];

        foreach (Coordinate dir in directions)
        {
            Cell adjacentCell = new(dir.GetRow(), dir.GetCol());
            if (IsValidCell(adjacentCell) && this.GridCells[adjacentCell.GetCoordinate().GetRow()][adjacentCell.GetCoordinate().GetCol()].GetRegionId() == -1)
            {
                adjacentCells.Add(adjacentCell);
            }
        }

        return adjacentCells;
    }

    private bool IsValidCell(Cell cell)
    {
        var coord = cell.GetCoordinate();
        return coord.GetRow() >= 0 && coord.GetRow() < GridSize &&
               coord.GetCol() >= 0 && coord.GetCol() < GridSize;
    }

    /// <summary>
    /// Sort the rows so they are ordered with target closest to center of grid is first and furthest is last
    /// </summary>
    /// <param name="targetLocations"></param>
    /// <returns>List of ints where each value can be used to index the grid array to get the row based on distance from center</returns>
    public List<int> SortRowsByDistTargetIsFromCenter(List<int> targetLocations)
    {
        List<int> sortedRows = [];
        // establish the gridsize based on the count of items in the targetLocations list, this is a big assumption that constrains the code
        int gridSize = targetLocations.Count;
        List<Tuple<int, double>> distancesForRows = [];
        // subtract 1 from the gridsize to get the center value, e.g. if gridSize is 9 then 4,4 is ctr cell, if gridsize is 8 then use 3.5,3.5
        double ctrSize = ((double)gridSize - 1) / 2;
        double distance;
        for (int r = 0; r < gridSize; r++)
        {
            distance = (Math.Abs((double)r - ctrSize)) + (Math.Abs((double)targetLocations[r] - ctrSize));
            distancesForRows.Add(Tuple.Create(r, distance));
        }
        List<Tuple<int, double>> sortedDistancesForRows = [];
        sortedDistancesForRows = distancesForRows.OrderBy(coord => coord.Item2).ToList();
        for (int i = 0; i < gridSize; i++)
        {
            sortedRows.Add(sortedDistancesForRows[i].Item1);
        }
        return sortedRows;
    }

    /// <summary>
    /// This method will randomly assign the targets to cells in the grid while conforming to the rules of placement
    /// </summary>
    /// <returns>List of ints which are the column values where the target is located within the row that corresponds to the in
    /// index into the list.</returns>
    /// <remarks>
    /// This method could probably be private but I am making it public while I am fleshing out design.
    /// It also will allow me to do TDD while developing the method
    /// </remarks>
    public List<int> AssignTargetLocations(int gridSize)
    {
        Random random = new();
        List<int> targetColumns = [];
        List<int> availableColumns = [];
        for (int i = 0; i < gridSize; i++)
        {
            availableColumns.Add(i);
        }
        for (int row = 0; row < gridSize; row++)
        {
            bool validPosition;
            int colIndex;
            int col;
            // Using a second list to reduce the number of times this do loop runs
            List<int> untriedAvailableColumns = [];
            untriedAvailableColumns.AddRange(availableColumns);
            do
            {
                // I wanted to create a method that didn't return a failure and another attempt was made. I wanted the 
                // logic to be sound enough that it would always work without a reshuffle so I am handling specific situations.

                // If there are only 4 values left in the available columns, handle the situation where we have continuous values
                // for example, if the remaining values are 3,4,5,6 there are limited choices for what can be picked next
                // if it is like 3,4,5,9 the 4 must be chosen to allow for the remaining numbers to not be diagonally adjacent
                if (untriedAvailableColumns.Count == 4 &&
                    untriedAvailableColumns[0] == untriedAvailableColumns[1] - 1 &&
                    untriedAvailableColumns[1] == untriedAvailableColumns[2] - 1)
                {
                    colIndex = 1;
                }
                // if it is like 1,4,5,6 the 5 must be chosen to allow for the remaining numbers to not be diagonally adjacent
                else if (untriedAvailableColumns.Count == 4 &&
                         untriedAvailableColumns[1] == untriedAvailableColumns[2] - 1 &&
                         untriedAvailableColumns[2] == untriedAvailableColumns[3] - 1)
                {
                    colIndex = 2;
                }
                // When there are 3 values left like 1,4,5 or 4,5,8 the middle number needs to be selected to avoid
                // diagonal adjacency except when the value from previous row is a possible diagonally adjacent value
                // if the values are 1,4,5 and 1 is chosen then 4 & 5 will be diagonally adjacent.
                else if (untriedAvailableColumns.Count == 3 &&
                (targetColumns[row - 1] != untriedAvailableColumns[1] + 1) &&
                (targetColumns[row - 1] != untriedAvailableColumns[1] - 1) &&
                (untriedAvailableColumns[0] == untriedAvailableColumns[1] - 1 ||
                untriedAvailableColumns[1] == untriedAvailableColumns[2] - 1))
                {
                    colIndex = 1;
                }
                else
                {
                    colIndex = random.Next(untriedAvailableColumns.Count);
                }
                col = untriedAvailableColumns[colIndex];
                validPosition = true;

                // If the target is diagonally adjacent to another target it is not valid
                // Since we are setting values 1 row at a time, just need to compare to previous row
                if ((row > 0 && targetColumns[row - 1] == col - 1) ||
                    (row > 0 && targetColumns[row - 1] == col + 1))
                {
                    validPosition = false;
                    // If we find an invalid column, remove it from possible columns to try in next loop
                    untriedAvailableColumns.RemoveAt(colIndex);
                }
            } while (!validPosition);
            targetColumns.Add(col);
            int removeColIx = availableColumns.IndexOf(col);
            availableColumns.RemoveAt(removeColIx);
        }

        return targetColumns;
    }

}

//Below here is AI generated code for solving this puzzle
/*

Sure! Here is a basic implementation of a Star Battle puzzle solver in C#.
This example uses a backtracking algorithm to solve the puzzle:

using System;

class StarBattleSolver
{
static int N = 10; // Size of the grid
static int[,] grid = new int[N, N]; // The puzzle grid
static int starsPerRow = 2; // Number of stars per row and column

static bool IsSafe(int row, int col)
{
    // Check row and column
    for (int i = 0; i < N; i++)
    {
        if (grid[row, i] == 1 || grid[i, col] == 1)
            return false;
    }

    // Check surrounding cells
    for (int i = -1; i <= 1; i++)
    {
        for (int j = -1; j <= 1; j++)
        {
            int newRow = row + i;
            int newCol = col + j;
            if (newRow >= 0 && newRow < N && newCol >= 0 && newCol < N && grid[newRow, newCol] == 1)
                return false;
        }
    }

    return true;
}

static bool Solve(int row, int col, int starsPlaced)
{
    if (starsPlaced == N * starsPerRow)
        return true;

    if (col == N)
    {
        row++;
        col = 0;
    }

    if (row == N)
        return false;

    if (IsSafe(row, col))
    {
        grid[row, col] = 1;
        if (Solve(row, col + 1, starsPlaced + 1))
            return true;
        grid[row, col] = 0;
    }

    return Solve(row, col + 1, starsPlaced);
}

static void PrintGrid()
{
    for (int i = 0; i < N; i++)
    {
        for (int j = 0; j < N; j++)
        {
            Console.Write(grid[i, j] == 1 ? "*" : ".");
        }
        Console.WriteLine();
    }
}

public static void Main()
{
    if (Solve(0, 0, 0))
        PrintGrid();
    else
        Console.WriteLine("No solution found.");
}
}

*/




