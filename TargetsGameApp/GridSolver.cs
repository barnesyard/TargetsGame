using System;
using System.Collections.Generic;

namespace TargetsGameApp
{
    public class GridSolver
    {
        // Since this project is all about learning and growing skills I want to implement two approaches.
        // I think it will be interesting to see how fast each approach is.

        // Method to solve the grid using a heuristic approach
        public static void SolveWithHeuristic(Grid grid)
        {
            // First step is a one-time-only look for a region where an entire row and entire col cross inside the region.
            Coordinate targetCoord;
            if (RegionHasCrossing(grid, out targetCoord))
            {
                // set the targetCoord in the grid as a target and mark appropriate cells with x
                UpdateCellsStatus(grid, targetCoord);
            }

            // write out the grid so we can verify the updated cells, will remove this later
            grid.WriteGrid();
        }

        /// <summary>
        /// This method updates the status of all the cells that are impacted when a target cell is located
        /// </summary>
        /// <param name="grid">The grid of cells that need to be udpated</param>
        /// <param name="tgt">The coordinate value for the target cell</param>
        /// <returns>A list of all the cells that were marked as invalid is returned so this update can be undone as needed</returns>
        private static List<Cell> UpdateCellsStatus(Grid grid, Coordinate tgt)
        {
            // Creating a list of cells to invalidate 
            // When updating the status we can check to see if they have already been marked
            // If they have already been marked we won't add them to the updated cells list
            // By using a Hashset here it is easier to check to see if values are already added
            HashSet<Cell> cellsToUpdate = new HashSet<Cell>();

            // Add all the cells in the same row as the target cell (excluding the target cell)
            cellsToUpdate.UnionWith(
                grid.GridCells[tgt.Row]
                    .Where(cell => cell.Coordinate.Col != tgt.Col)
            );

            // Add all the cells in the same column as the target cell (excluding the target cell)
            cellsToUpdate.UnionWith(
                grid.GridCells.Select(row => row[tgt.Col])
                    .Where(cell => cell.Coordinate.Row != tgt.Row)
            );

            // Add the diagonally adjacent cells because targets cannot touch diagonally
            cellsToUpdate.UnionWith(
                tgt.DiagonalCoordinates()
                    .Where(coord => grid.IsValidCoordinate(coord))
                    .Select(coord => grid.GridCells[coord.Row][coord.Col])
            );

            // Add all other cells in the same region as the target (excluding the target cell)
            Region targetRegion = grid.GridRegions[grid.GridCells[tgt.Row][tgt.Col].RegionId];
            cellsToUpdate.UnionWith(
                targetRegion.Cells
                    .Where(cell => cell.Coordinate != tgt)
            );

            // Now we will create a list of all the cells that were actually invalidated due to this target cell
            // We are keeping this list for the algorithmic solve, if we hit a dead end we will need a way to undo the steps we took
            // Using Cell instead of Coordinate because the Cell has extra data that will be helpful
            List<Cell> updatedCells = new List<Cell>();

            // If a cell has a status of 'o' it means it is empty so we need to set it to 'x' which is invalid
            // If a cell has a status of 't' meaning it is already marked as a target, or 'x' we don't need to do anything to it
            foreach (var cell in cellsToUpdate)
            {
                if (cell.Status == CellStatus.o)
                {
                    cell.Status = CellStatus.x;
                    updatedCells.Add(cell); 
                }
            }

            // Now that all invalid cells are set, update the status of the target cell to 't' because it is the 't'arget
            grid.GridCells[tgt.Row][tgt.Col].Status = CellStatus.t;
            // The Grid object has a list of target locations, update that so when it is needed it is correct
            grid.TargetLocations[tgt.Row] = tgt.Col;

            return updatedCells;
        }


        // Helper method to check if a region has an entire column and entire row crossing inside it
        static private bool RegionHasCrossing(Grid grid, out Coordinate tgt)
        {
            Coordinate targetCoordinate = new Coordinate();
            bool retVal = false;
            // Create a list of regions where the count of cells >= 2 * gridSize because it has to be at least that big
            List<Region> largeRegions = grid.GridRegions
                .Where(region => region.Cells.Count >= (2 * grid.GridSize))
                .ToList();

            foreach (Region region in largeRegions)
            {
                // Check for an entire row existing in the region
                var rowGroups = region.Cells.GroupBy(cell => cell.Coordinate.Row)
                    .Where(group => group.Count() == grid.GridSize);

                // Check for an entire column existing in the region
                var colGroups = region.Cells.GroupBy(cell => cell.Coordinate.Col)
                    .Where(group => group.Count() == grid.GridSize);

                // If there is an entire row and an entire column in the region the place where they cross is a target
                if (rowGroups.Any() && colGroups.Any())
                {
                    // Find the coordinate that exists in both rowGroups and colGroups
                    targetCoordinate = rowGroups.SelectMany(group => group)
                        .Select(cell => cell.Coordinate)
                        .Intersect(colGroups.SelectMany(group => group)
                        .Select(cell => cell.Coordinate))
                        .FirstOrDefault();

                    retVal = true;
                }
            }
            tgt = targetCoordinate;
            return retVal;
        }

        // Method to solve the grid using an algorithmic approach
        public static void SolveWithAlgorithm(Grid grid)
        {
            // ... algorithmic solving logic ...
            // ToDo: Later implement a backtracking algorithm

            //Below here is AI generated code for solving a similar puzzle, it will help with ideas on how to implement when I get here.
            /*

            basic implementation of a Star Battle puzzle solver in C#.
            This example uses a backtracking algorithm to solve the puzzle:

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
            */
        }

    }
}