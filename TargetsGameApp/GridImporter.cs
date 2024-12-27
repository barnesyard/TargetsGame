using System.Text.Json;
using System.Threading.Tasks.Dataflow;
using Emgu.CV;
using Emgu.CV.Util;
using System.Drawing;
using Emgu.CV.Shape;

namespace TargetsGameApp;

public class GridImporter
{
    /// <summary>
    /// This method will read the region IDs from a JSON file 
    /// </summary>
    /// <param name="filePath">path to the JSON file</param>
    /// <param name="maxGridSize">the maximum size of the grid</param>
    /// <returns>a 2d list of lists of ints, the first dimension is the row, the second dimension is the column</returns>
    public static List<List<int>>? ImportFromJson(string filePath, int maxGridSize)
    {
        try
        {
            string jsonString = File.ReadAllText(filePath);
            List<List<int>>? regionIds = JsonSerializer.Deserialize<List<List<int>>>(jsonString);
            if (regionIds?.Count > maxGridSize)
            {
                throw new Exception("Grid size is greater than allowed size of " + maxGridSize);
            }
            return regionIds;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading JSON file: {ex.Message}");
            return null;
        }
    }

    public static List<List<int>>? ImportFromImage(string filePath)
    {
        Mat gridScreenShot = new Mat();
        gridScreenShot = CvInvoke.Imread(filePath, Emgu.CV.CvEnum.ImreadModes.Color);
        // If you need to debug you show any Mat (a "matrix") with the two lines below
        // CvInvoke.Imshow("Grid Screen Shot", gridScreenShot);
        // CvInvoke.WaitKey(); //This method causes your window showing the Mat to stay on the screen until a key is pressed

        // Convert the image to grayscale so it can be transformed into a black/white image
        Mat grayImage = new Mat(); // this is a new Mat (matrix) that will be passed in as a parameter but it is an output parameter
        // The conversion type could have been Bgr2Gray or Rgb2Gray, the Bgr seemed to work better
        CvInvoke.CvtColor(gridScreenShot, grayImage, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);

        // Convert to a "binary" image (just has black or white pixels) to find edges
        Mat binaryImage = new Mat();
        CvInvoke.Threshold(
            grayImage, // this is the input image, using the grayscale image to apply a threshold to a shade of gray to determine if pixel is on or off
            binaryImage, // this is the output of this method, rather than returning a Mat it takes in the object where output is stored
            10, // this is threshold when this calculation is performed by the BinaryInv type: value = value > threshold ? 0 : max_value.
                // I put 10 as the threshold because when it was converted to greyscale the colors were so light they were close to white and the lines were black (value 0)
            255, // this is max_value when this calculation is performed by the BinaryInv type: value = value > threshold ? 0 : max_value
            Emgu.CV.CvEnum.ThresholdType.BinaryInv); // Used BinaryInv because contours are found with white areas so I wanted the outside of the image to be black

        // Find contours
        Mat heirarchy = new Mat();
        VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint(); //this is the list of contour points that will be used to find regions on the grid
        CvInvoke.FindContours(
            binaryImage, // using the black and white image as input to make it really easy to find contours
            contours, // this is where the list of contour points will be stored
            heirarchy,  // not sure how to use this, docs say it is an optional output vector, containing information about the image topology
            Emgu.CV.CvEnum.RetrType.Tree, // retrieve all the contours and reconstructs the full hierarchy of nested contours
            Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple); // compress horizontal, vertical, and diagonal segments, that is, the function leaves only their ending points

        Console.WriteLine("The contour has the size of {0} which means the grid has {1} cells", contours.Size, contours.Size - 1);

        // If you need to debug you can draw contours and show that but do it on a new copy of the original image so we don't lose the original
        // Mat contourImage = gridScreenShot.Clone();
        // CvInvoke.DrawContours(contourImage, contours, 0, new Emgu.CV.Structure.MCvScalar(255, 0, 0), 2); // Draw contours in blue
        // CvInvoke.Imshow("Contours", contourImage);
        // CvInvoke.WaitKey();

        // Get the x and y values that define the bounding box of this grid along with width and height and grid dimension (which 7 for a 7x7 grid)
        GridInfo GridInfo = new(contours);

        // Create a list of unique color values which should have a length of gridSize so we can add another layer of verification
        List<Emgu.CV.Structure.MCvScalar> colorList = [];
        double tolerance = 10; // TODO: check this value with some screenshots that have a greyed cell due to mouse over
        // This is will be what we retun, a value for each cell that has a region ID
        List<List<int>> gridRegionIds = [];

        // Loop through each row
        for (int row = 0; row < GridInfo.GridDimension; row++)
        {
            List<int> rowRegionIds = [];
            // Loop through each column
            for (int col = 0; col < GridInfo.GridDimension; col++)
            {
                VectorOfVectorOfPoint cellContours = GetContoursForGridCell(GridInfo, row, col);

                // Create a mask for the cell, it will be the size of the original image 
                Mat mask = new(gridScreenShot.Size, Emgu.CV.CvEnum.DepthType.Cv8U, 1); // the depth type is 8 bit unsigned which is good for what I am doing, small range of color values
                mask.SetTo(new Emgu.CV.Structure.MCvScalar(0)); // in the new mask image we created, set all pixels to 0 (which is black)
                CvInvoke.DrawContours(mask, cellContours, -1, new Emgu.CV.Structure.MCvScalar(255), -1); // -1 thickness means fill, the 255 says fill with white color

                // Calculate mean color within the mask
                Emgu.CV.Structure.MCvScalar cellMeanColor = CvInvoke.Mean(gridScreenShot, mask);

                // Add this color to our list if it is unique (with some tolerance for similar colors)
                AddNewUniqueColor(cellMeanColor, colorList, tolerance);
                //Add the value for the region ID which will be the index into the list of colors
                rowRegionIds.Add(GetColorIndex(cellMeanColor, colorList, tolerance));

            }
            gridRegionIds.Add(rowRegionIds);
        }

        return gridRegionIds;
    }

    /// <summary>
    /// Getting the index of the value of the color in the color list
    /// </summary>
    /// <param name="color">The value of the color found in the cell</param>
    /// <param name="colorList">The list of colors found in the grid</param>
    /// <param name="tolerance">The tolerance for exact color matching</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static int GetColorIndex(Emgu.CV.Structure.MCvScalar color, List<Emgu.CV.Structure.MCvScalar> colorList, double tolerance)
    {
        for (int i = 0; i < colorList.Count; i++)
        {
            if (Math.Abs(colorList[i].V0 - color.V0) < tolerance &&
                Math.Abs(colorList[i].V1 - color.V1) < tolerance &&
                Math.Abs(colorList[i].V2 - color.V2) < tolerance)
            {
                return i; // Return the index if a match is found
            }
        }
        // If we don't find a color throw an exception
        Console.WriteLine("Color value not found in list of colors");
        throw new Exception($"Color value not found in list of colors");
    }
    
    // New method to check and add unique color
    private static void AddNewUniqueColor(Emgu.CV.Structure.MCvScalar cellMeanColor, List<Emgu.CV.Structure.MCvScalar> colorList, double tolerance)
    {
        bool isUnique = true;
        foreach (var existingColor in colorList)
        {
            // Check if colors are similar (within tolerance)
            if (Math.Abs(existingColor.V0 - cellMeanColor.V0) < tolerance &&
                Math.Abs(existingColor.V1 - cellMeanColor.V1) < tolerance &&
                Math.Abs(existingColor.V2 - cellMeanColor.V2) < tolerance)
            {
                isUnique = false;
                break;
            }
        }

        if (isUnique)
        {
            colorList.Add(cellMeanColor);
            Console.WriteLine($"Found new unique color - B: {cellMeanColor.V0}, G: {cellMeanColor.V1}, R: {cellMeanColor.V2}");
        }
    }

    static private VectorOfVectorOfPoint GetContoursForGridCell(GridInfo gridInfo, int row, int col)
    {
        int cellWidth = gridInfo.Width / gridInfo.GridDimension;
        int cellHeight = gridInfo.Height / gridInfo.GridDimension;

        // Add shrink variable to shrink the size of the region sampled for color value to be sure it is homogenous, no boundary colors included by accident
        int shrinkAmount = 10;
        // Create a square contour for a single cell with shrinking
        Point[] cellPoints =
        [
            new(gridInfo.MinX + (col * cellWidth) + shrinkAmount, gridInfo.MinY + (row * cellHeight) + shrinkAmount),
                    new(gridInfo.MinX + ((col + 1) * cellWidth) - shrinkAmount, gridInfo.MinY + (row * cellHeight) + shrinkAmount),
                    new(gridInfo.MinX + ((col + 1) * cellWidth) - shrinkAmount, gridInfo.MinY + ((row + 1) * cellHeight) - shrinkAmount),
                    new(gridInfo.MinX + (col * cellWidth) + shrinkAmount, gridInfo.MinY + ((row + 1) * cellHeight) - shrinkAmount)
        ];

        VectorOfPoint cellContour = new(cellPoints);
        VectorOfVectorOfPoint cellContours = new(cellContour);
        return cellContours;
    }

    private class GridInfo
    {
        public int MinX { get; private set; } = int.MaxValue;
        public int MinY { get; private set; } = int.MaxValue;
        public int MaxX { get; private set; } = 0;
        public int MaxY { get; private set; } = 0;
        public int Width { get; private set; } = 0;
        public int Height { get; private set; } = 0;
        public int GridDimension { get; private set; } = 0;

        /// <summary>
        /// Find the min and max values of x and y from the top level contour in our contour tree found in image using CV
        /// </summary>
        /// <param name="contours">This is a VectorOfVectorOfPoint that is a tree of contours from image</param>
        public GridInfo(VectorOfVectorOfPoint contours)
        {
            // process in a for loop instead of LINQ because it is faster (based on internet research)
            for (int i = 0; i < contours[0].Size; i++)
            {
                if (contours[0][i].X < MinX) MinX = contours[0][i].X;
                if (contours[0][i].Y < MinY) MinY = contours[0][i].Y;
                if (contours[0][i].X > MaxX) MaxX = contours[0][i].X;
                if (contours[0][i].Y > MaxY) MaxY = contours[0][i].Y;
            }

            Width = MaxX - MinX;
            Height = MaxY - MinY;

            // In testing I found that the contour tree contained the outer bounding box and then a list of contours that correspond to cells in the grid
            // That means the count of contours is 1 + GridDimension^2. We can use this formula to find the GridDimension.
            // As a double check of my assumption I will use a switch statement to set the grid dimension (rather than making a calculation)
            switch (contours.Size - 1) // The grid dimension range from 7x7 to 11x11
            {
                case 49: GridDimension = 7; break;
                case 64: GridDimension = 8; break;
                case 81: GridDimension = 9; break;
                case 100: GridDimension = 10; break;
                case 121: GridDimension = 11; break;
                default:
                    Console.WriteLine("Grid dimension not found");
                    throw new Exception($"Unsupported grid dimension detected. Found {contours.Size - 1} cells, which doesn't correspond to a supported grid dimension.");
            }

        }
    }
}
