using System.Globalization;
using TargetsGameApp;
namespace TargetsGameTests;

public class Grid_GenerateRegionsShould
{
    [Fact]
    public void GenerateRegions_ReturnsListOfCorrectLength()
    {
        int numTargets = 9;
        Grid gridObj = new Grid(numTargets);
        List<int> targetLocations = gridObj.AssignTargetLocations(numTargets);
        List<Region> generatedRegions = gridObj.GenerateRegions(targetLocations, gridObj.GridCells);
        Assert.Equal(numTargets, generatedRegions.Count);
    }
   [Fact]
    public void AssignTargetLocations_ReturnsListOfCorrectLength()
    {
        int numTargets = 9;
        Grid gridObj = new Grid(numTargets);
        List<int> targetList = gridObj.AssignTargetLocations(9);
        Assert.Equal(numTargets, targetList.Count);
    }}