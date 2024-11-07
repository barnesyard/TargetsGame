# Summary 
This is a console app that I am creating to practice writing code but here are the intentions with the code. The idea is to use the LinkedIn Queens game as source of inspiration. With that game in mind I want to: 
1. Use OpenCV to scan a screenshot and generate a grid from a screenshot
1. Generate a random grid that one can solve for fun
1. Use backtrack algorithm to solve the grid
1. Use a hueristic approach to solving the puzzle
1. Comppare the 2 solve approaches to see how fast a hueristic can be
1. Eventually I want to make a webservice that will have a UI that calls an API to get data to populate the UI. This is practice code that I will port to that webservice. I want to create a UI experience where usesrs can walk through the solving process one step at a time.

# Details about Using OpenCV
While working on this project I wanted to use the OpenCV library to learn more about it. I found the Emgu.CV C# wrapper for OpenCV and used it to find regions of the grid in a screenshot. I am adding some visuals here for the sake of helping others learn and also to help the future version of myself remember what I did. I won't put the code here just some visuals.

##  Import a screenshot
The first step is import the screenshot. This is what it looked like for me:
![ImportedScreenShot](https://github.com/user-attachments/assets/9c358b3b-01c0-4038-b529-a8b7dc71c0cc)

##  Convert the screenshot to grayscale
This is an intermediary step that you need to do before you can convert it to a binary image:
![GrayedScreenShot](https://github.com/user-attachments/assets/b765eaab-5a50-4c26-91a3-3788b2535c9b)

##  Convert the grayscale image to a binary image
OpenCV finds "contours" (or edges or boundaries) in the image using a binary image. A binary image has pixels that are on or off. The contours are found by seeking the "on" pixels or white pixels.
![BinaryInverseScreenShot](https://github.com/user-attachments/assets/e7b1fef0-52f6-42ba-a0e3-87bd78f2e638)

##  Find contours in the image
The OpenCV functionality to find contours has many options. I found a "tree" of contours. For this screen shot that had the outer bounding box (shown in blue in this image) and child contours for each cell in the grid. In this example there were 50 contours, the outer box and the 49 boxes for the 7x7 cells in the grid.
![ContoursScreenShot](https://github.com/user-attachments/assets/4d73c037-b2fa-4c0c-a425-5f96f3b29bbe)

## Use a mask to extract color of a cell
Create a mask as shown in the image below. That mask is used with the original screenshot to find the mean color value of all the unmasked pixels.
![MaskScreenShot](https://github.com/user-attachments/assets/c55d4a5c-1fea-4150-9c79-b7ef4181f72f)






