using System;
using System.Collections.Generic;

namespace TargetsGameApp
{
    public class GridSolver
    {
        // Method to solve the grid using an algorithmic approach
        public static void SolveWithAlgorithm(Grid grid)
        {
            // ... algorithmic solving logic ...
        }

        // Method to solve the grid using a heuristic approach
        public static void SolveWithHeuristic(Grid grid)
        {
            // ... heuristic solving logic ...
            // First step is a one-time-only look for a region where an entire row and entire col cross inside the region.
            Coordinate targetCoord;
            if (RegionHasCrossing(grid, out targetCoord))
            {
                // set the targetCoord in the grid as a target and mark appropriate cells with x
                SetTargetUpdateCellsStatus(grid, targetCoord);
            }

            // write out the grid so we can verify the updated cells
            grid.WriteGrid();
        }

        private static List<Cell> SetTargetUpdateCellsStatus(Grid grid, Coordinate tgt)
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

            // Add the diagonally adjacent cells
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

            // This is a list of all the cells that were invalidated due to this target cell
            // We are keeping this list for the algorithmic solve, if we hit a dead end we will need a way to undo the steps we took
            List<Cell> updatedCells = new List<Cell>(); // List to collect updated coordinates

            // Loop over all cells in cellsToUpdate and update their status if it's 'o'
            foreach (var cell in cellsToUpdate)
            {
                if (cell.Status == CellStatus.o)
                {
                    cell.Status = CellStatus.x; // Set status to 'x'
                    updatedCells.Add(cell); // Add to updatedCells list
                }
            }

            // Set the status of the target cell to 't' because it is the 't'arget
            grid.GridCells[tgt.Row][tgt.Col].Status = CellStatus.t; // Set target cell status to 't'

            return updatedCells; // Return the list of updated cells
        }


        // Helper method to check if a region has an entire column and entire row crossing inside it
        static private bool RegionHasCrossing(Grid grid, out Coordinate tgt)
        {
            Coordinate targetCoordinate = new Coordinate();
            bool retVal = false;
            // Create a list of regions where the count of cells >= 2 * gridSize
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
    }
}