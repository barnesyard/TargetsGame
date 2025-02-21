// See https://aka.ms/new-console-template for more information
using System;
using System.Diagnostics;
using System.Dynamic;
using System.Numerics;
using System.Text.Json;

namespace TargetsGameApp;

class Program
{
    static void Main()
    {
        // TODO: use command line args to load a file
        int maxGridSize = 11;
        // string imageFilePath = "./img/Screenshot 2024-10-21 092751.png";
        string imageFilePath = "./img/c.png";
        List<List<int>>? regionIds = GridImporter.ImportFromImage(imageFilePath);

        // Read region IDs from JSON file using GridImporter
        // string jsonFilePath = "grid.json";
        // List<List<int>>? regionIds = GridImporter.ImportFromJson(jsonFilePath, maxGridSize);
        Grid theGrid;
        if (regionIds != null)
        {
            theGrid = new(regionIds);
        }
        else
        {
            Console.WriteLine("Failed to load region IDs from JSON file. Generating a random grid.");
            Random theRandom = new Random();
            int gridSize = theRandom.Next(7, maxGridSize);
            theGrid = new(gridSize);
            theGrid.GenerateGrid();

            theGrid.WriteGrid();
        }
        // Write the grid to the screen
        theGrid.WriteGrid();

        GridSolver.SolveWithHeuristic(theGrid);
    }
}

public struct Coordinate
{
    public int Row { get; }
    public int Col { get; }

    public Coordinate(int row, int col)
    {
        Row = row;
        Col = col;
    }

    public static bool operator !=(Coordinate left, Coordinate right)
    {
        return !(left == right); // Use the existing == operator for comparison
    }

    public static bool operator ==(Coordinate left, Coordinate right)
    {
        return left.Row == right.Row && left.Col == right.Col;
    }
    public override bool Equals(object? obj)
    {
        if (obj is Coordinate other)
        {
            return this.Row == other.Row && this.Col == other.Col;
        }
        return false;
    }

    // This is needed when you override the Equals operator.
    public override int GetHashCode()
    {
        return HashCode.Combine(Row, Col);
    }

    public Coordinate Up() => new Coordinate(Row - 1, Col);
    public Coordinate UpRight() => new Coordinate(Row - 1, Col + 1);
    public Coordinate Right() => new Coordinate(Row, Col + 1);
    public Coordinate DownRight() => new Coordinate(Row + 1, Col + 1);
    public Coordinate Down() => new Coordinate(Row + 1, Col);
    public Coordinate DownLeft() => new Coordinate(Row + 1, Col - 1);
    public Coordinate Left() => new Coordinate(Row, Col - 1);
    public Coordinate UpLeft() => new Coordinate(Row - 1, Col - 1);

    public List<Coordinate> OrthogonalCoordinates()
    {
        return new List<Coordinate>
        {
            Up(),
            Right(),
            Down(),
            Left()
        };
    }
    public List<Coordinate> DiagonalCoordinates()
    {
        return new List<Coordinate>
        {
            UpRight(),
            DownLeft(),
            DownRight(),
            UpLeft()
        };
    }

    public List<Coordinate> AllAdjacentCoordinates()
    {
        return new List<Coordinate>
        {
            Up(),
            Right(),
            Down(),
            Left(),
            UpRight(),
            DownLeft(),
            DownRight(),
            UpLeft()
        };
    }
}

// Enum to represent the status of a cell
public enum CellStatus
{
    x, // invalid
    o, // empty
    t  // target
}

public class Cell
{
    public int RegionId { get; set; }
    public CellStatus Status { get; set; }
    public Coordinate Coordinate { get; set; }

    // I am doing this to explore access with properties and fields
    private bool _isTarget;
    public bool IsTarget
    {
        get { return _isTarget; }
        set { _isTarget = value; } //This is no different than your basic property but this is where extra functionality can be added
    }

    public Cell()
    {
        this.Coordinate = new Coordinate(0, 0);
        this.IsTarget = false;
        this.RegionId = -1;
        this.Status = CellStatus.o;
    }

    public Cell(int row, int col)
    {
        this.Coordinate = new Coordinate(row, col);
        this.IsTarget = false;
        this.RegionId = -1;
        this.Status = CellStatus.o;
    }

    public Coordinate GetCoordinate()
    {
        return this.Coordinate;
    }

    public static Cell operator +(Cell cell, Coordinate coord)
    {
        return new Cell(cell.Coordinate.Row + coord.Row, cell.Coordinate.Col + coord.Col);
    }
}

// A region refers to a set of cells that are orthoganally adjacent 
// the number of regions in a grid is equal to the GridSize, one region per target
public class Region
{
    public List<Cell> Cells { get; private set; } = new List<Cell>();
    public int Id { get; private set; } = -1;

    public void Add(Cell cell)
    {
        if (this.Id == -1) this.Id = cell.RegionId; // a value of -1 indicates the region was newly created with no cells yet
        // This exception should only be thrown if there is problem with the logic in the code, it won't happen through usage
        if (this.Id != cell.RegionId) throw new InvalidOperationException("Cell does not have the same RegionId as the region.");
        Cells.Add(cell);
    }
}
