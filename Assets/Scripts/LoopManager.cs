using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LoopManager : MonoBehaviour {

    public static bool flagCheckForLoops; // this flag is rised when a line is drawn, it checks if there is any closed loop
    public static Vector2 lineLocation; // the last drawen lines coordinates
    public static bool flagLoopDetected;

    public Transform planePrefab; // this object will be used when instantiating the conquered area
    public Material fadedMaterial; // this material will be used when the object needs to be transparent

    private Color[] playerColor; // this array will store each players color
    private Transform planeObject; // this variable is used to access every conquered area object properties
    private GameObject planes; // this is the parent object of the conquered areas, so everything will be more coordinated 
    private Renderer rend;
    private int playerNumber;

    enum Direction { North, South, East, West };

    /// <summary>
    /// This class is used to define each 
    /// point that creates a closed mesh
    /// </summary>
    class PointCheckUp
    {
        public Vector2 position; // the position of the point
        public Direction nextPointDirection; // this is the direction of the next point in the path
        public int cornerDirection; // this is where the point sits on the drawen line

        // these are the directions which are checked or not to define a path
        private bool checkNorth = false;
        private bool checkSouth = false;
        private bool checkEast = false;
        private bool checkWest = false;

        /// <summary>
        /// This is the construter function, it defines the point objects position an direction of the line
        /// </summary>
        /// <param name="direction"> the position of the point </param>
        /// <param name="pos"> this is where the point sits on the drawen line </param>
        public PointCheckUp(int direction, Vector2 pos)
        {
            cornerDirection = direction; // set the direction
            position = pos; // set the position

            // no direction is checked yet, every direction is checkable
            this.checkNorth = true;
            this.checkSouth = true;
            this.checkEast = true;
            this.checkWest = true;

            //This switch remove the checking option of a point so it wont check backwards
            switch (direction)
            {
                case 0:
                    this.checkSouth = false;
                    break;
                case 1:
                    this.checkNorth = false;
                    break;
                case 2:
                    this.checkWest = false;
                    break;
                case 3:
                    this.checkEast = false;
                    break;
            }
        }

        /// <summary>
        /// This function checkes the current points surrounding points.
        /// If the function finds any lines it stores the next points direction
        /// and returns its position. 
        /// </summary>
        /// <returns> The next point position </returns>
        public Vector2 CheckSurroundings()
        {
            if (checkNorth)
            {
                this.checkNorth = false;

                if (GameObject.Find("L_" + this.position.x.ToString() + "_" + (this.position.y + 2.5).ToString()) != null)
                {
                    nextPointDirection = Direction.North; // store the next point direction
                    return new Vector2(this.position.x, this.position.y + 5); // return the position of the next point
                }
            }
            if (checkSouth)
            {
                this.checkSouth = false;

                if (GameObject.Find("L_" + this.position.x.ToString() + "_" + (this.position.y - 2.5).ToString()) != null)
                {
                    nextPointDirection = Direction.South;// store the next point direction
                    return new Vector2(this.position.x, this.position.y - 5);// return the position of the next point
                }
            }
            if (checkEast)
            {
                this.checkEast = false;

                if (GameObject.Find("L_" + (this.position.x + 2.5).ToString() + "_" + this.position.y.ToString()) != null)
                {
                    nextPointDirection = Direction.East;// store the next point direction
                    return new Vector2(this.position.x + 5, this.position.y);// return the position of the next point
                }
            }
            if (checkWest)
            {
                this.checkWest = false;

                if (GameObject.Find("L_" + (this.position.x - 2.5).ToString() + "_" + this.position.y.ToString()) != null)
                {
                    nextPointDirection = Direction.West;// store the next point direction
                    return new Vector2(this.position.x - 5, this.position.y);// return the position of the next point
                }
            }

            return new Vector2(-1, -1); // return this vector if nothing is found
        }
    }

    // Use this for initialization
    void Start()
    {
        flagCheckForLoops = false;
        flagLoopDetected = false;
        playerNumber = PlayerProperties.playerAmount; // store the static player number value into a local variable 
        playerColor = PlayerProperties.playerColor; // store the colors on start so we can access this variable locally

        planes = new GameObject("Planes"); // create the empty parent object
    }

    void Update()
    {
        if (flagCheckForLoops) // check if there is any closed loops
        {
            var path = FindBiggestLoop(); // obtain the biggest path of lines forming the closed loops

            if (path.Count != 0) // if path does exists there is a closed loop
            {
                var playerTurn = PlayerProperties.playerTurn;

                OccupyLands(path, playerTurn); // fill the closed loop with planes
                flagLoopDetected = true;
            }

            flagCheckForLoops = false; // checking complete
        }
    }

    /// <summary>
    /// This function checks if there is any closed loops, 
    /// after a line is drawn by a player. If there is a closed loop
    /// it returns a list of verticle line coordinates of lines that forges this loop
    /// If there is multiple loops detected, this function calculates the areas
    /// of each loop and returns the biggest loops path.
    /// </summary>
    /// <returns> The verticle line coordinates forming the biggest closed loop </returns>
    List<Vector2> FindBiggestLoop()
    {
        Vector2 pos;

        var cornerCounter = 0; // start counting from 0
        var posDirection = 0; //initialize with a random value        
        var pathArea = 0f; // the area of the loop
        var verticleLineCoordinates = new List<Vector2>(); // this list will contain only verticle lines 
        var path = new List<PointCheckUp>(); // this list will contain all the points in the path

        if (Mathf.Abs(Mathf.Floor(lineLocation.x) - lineLocation.x) == 0)
        {
            pos = new Vector2(lineLocation.x, lineLocation.y + 2.5f); // the coordinate of the first point
            posDirection = (int)Direction.North; // if line is verticle start from the top side and continue so
        }
        else
        {
            pos = new Vector2(lineLocation.x + 2.5f, lineLocation.y); // the coordinate of the first point
            posDirection = (int)Direction.East; // if line is horizontal start from right side and continue so
        }

        path.Add(new PointCheckUp(posDirection, pos)); // add the first point to the list

        while (cornerCounter >= 0)
        {
            var nextPos = path[cornerCounter].CheckSurroundings(); // build the relation ship with the current point and the next point which both are in the path

            if ((int)path[cornerCounter].nextPointDirection == posDirection && nextPos.x == pos.x && nextPos.y == pos.y) // if the next point is pointing the first point the loop is completed, which mean there is a closed loop
            {
                var area = CalculateArea(path); // calculate the area of the loop

                if (area > pathArea) // if the area is bigger than the previous loop enter here
                {
                    pathArea = area; // refresh the biggest area
                    verticleLineCoordinates = ConvertPointsToVerticleLines(path); // convert the list of all points to only verticle lines
                }

                continue;
            }


            else if (CheckRepeatedPath(path, nextPos)) // if point already exists in the path, dont store it again so that program wont get stuck in an infinite loop 
                continue;

            if (nextPos.x == -1) // if there is no lines in the direction go back to the previous point an search for an other direction
            {
                path.RemoveAt(cornerCounter); // all the directions are empty, remove this point
                cornerCounter--;
                continue;
            }

            path.Add(new PointCheckUp((int)path[cornerCounter].nextPointDirection, nextPos)); //this point is the next point, add it to the path
            cornerCounter++;
        }

        return verticleLineCoordinates;
    }

    /// <summary>
    /// This function calculates the closed loop area.
    /// Every square will count as 1f unit.
    /// Starts from the left side of loop and scans throught right
    /// step by step. On each step adds up the located area.
    /// </summary>
    /// <param name="path"> the list of points forming the loop </param>
    /// <returns> The area of the closed loop </returns>
    private float CalculateArea(List<PointCheckUp> path)
    {
        float area = 0; // the area
        bool flagAdd = false; // if this flag is raised, there is some calculated area to add
        List<Vector2> linePathForAreaCalc = new List<Vector2>();

        linePathForAreaCalc = ConvertPointsToLines(linePathForAreaCalc, path); // convert every point to lines

        float leftCoordinate_X = path[0].position.x; // this is the very left coordinate of the loop
        float rightCoordinate_X = path[0].position.x; // this is the very right coordinate of the loop

        for (int i = 0; i < path.Count; i++)
        {
            if (leftCoordinate_X > path[i].position.x) // enter here if found a coordinate on lefter side
                leftCoordinate_X = path[i].position.x;

            if (rightCoordinate_X < path[i].position.x) // enter here if found a coordinate on righter side
                rightCoordinate_X = path[i].position.x;
        }

        leftCoordinate_X -= 2.5f; // start from the left of the loop and be ready to scan throught right to calculate area

        var storeValueTop_Y = -1f; // the top coordinate of the current step
        var storeValueBot_Y = -1f; // the bottom coordinate of the current step
        var index = 0; // this variable will be used to locate the right member of the list

        while (true) // untill scaning is completed
        {
            for (int i = 0; i < linePathForAreaCalc.Count; i++) // walk throught the lines of path
            {
                if (linePathForAreaCalc[i].x == leftCoordinate_X + 5f && linePathForAreaCalc[i].y > storeValueTop_Y && !flagAdd) // find the line top line on the current step
                {
                    storeValueTop_Y = linePathForAreaCalc[i].y;
                    index = i;
                }

                if (linePathForAreaCalc[i].x == leftCoordinate_X + 5f && linePathForAreaCalc[i].y > storeValueBot_Y && flagAdd) // find the line below the top line on the current step
                {
                    storeValueBot_Y = linePathForAreaCalc[i].y;
                    index = i;
                }
            }

            linePathForAreaCalc.RemoveAt(index); // remove the found line from the list

            if (flagAdd)
            {
                area += (storeValueTop_Y - storeValueBot_Y) / 5; // add all the squares between the top line and the bottom line
                storeValueTop_Y = -1f; // reset the values to default
                storeValueBot_Y = -1f; // reset the values to default
                leftCoordinate_X += 5; // move throught the next step
                if (leftCoordinate_X + 2.5f == rightCoordinate_X) // enter if the scanning is completed
                    return area;
            }

            flagAdd ^= true; // toggle the flag
        }
    }

    /// <summary>
    /// This fucntion converts the path of points into
    /// coordinates of verticle lines. The program will use this
    /// list to define which areas are conquered
    /// </summary>
    /// <param name="path"> this is the path of points </param>
    /// <returns> the coordinates of verticle lines </returns>
    private List<Vector2> ConvertPointsToVerticleLines(List<PointCheckUp> path)
    {
        List<Vector2> linePath = new List<Vector2>();

        linePath = ConvertPointsToLines(linePath, path); // convert points into lines of path 

        List<Vector2> tempList = new List<Vector2>(linePath);// create a temprory list so that the real one wont be effected
        RemoveUnwantedHorizontalLines(tempList); // this function removes the horizontal lines in the conquered area

        // this removes the lines from the list which are horizontal
        for (int i = 0; i < linePath.Count; i++)
        {
            if (Mathf.Abs(Mathf.Floor(linePath[i].x) - linePath[i].x) != 0) // if line is horizontal
            {
                linePath.RemoveAt(i);
                i--;
            }
        }

        return linePath; // return the verticle line coordinates
    }

    /// <summary>
    /// This function converts point coordinates into line coordinates of a path
    /// </summary>
    /// <param name="linePath"> the line coordinates of a path(loop) </param>
    /// <param name="path"> The point coordinates of a path(loop) </param>
    /// <returns></returns>
    private List<Vector2> ConvertPointsToLines(List<Vector2> linePath, List<PointCheckUp> path)
    {
        // this adds all the line corinates to the list
        foreach (PointCheckUp p in path)
        {
            switch (p.nextPointDirection)
            {
                case Direction.East:
                    linePath.Add(new Vector2(p.position.x + 2.5f, p.position.y));
                    break;
                case Direction.North:
                    linePath.Add(new Vector2(p.position.x, p.position.y + 2.5f));
                    break;
                case Direction.South:
                    linePath.Add(new Vector2(p.position.x, p.position.y - 2.5f));
                    break;
                case Direction.West:
                    linePath.Add(new Vector2(p.position.x - 2.5f, p.position.y));
                    break;
            }
        }
        return linePath;
    }

    /// <summary>
    /// This function is called to clear all the horizontal lines and barricades
    /// inside the conquered area, so that the conquered path is clear
    /// </summary>
    /// <param name="tempList"></param>
    private void RemoveUnwantedHorizontalLines(List<Vector2> tempList)
    {
        // remove the verticle lines from the list
        for (int i = 0; i < tempList.Count; i++)
        {
            if (Mathf.Abs(Mathf.Floor(tempList[i].y) - tempList[i].y) != 0) // if line is horizontal
            {
                tempList.RemoveAt(i);
                i--;
            }
        }

        var lineTop = new Vector2(0, 0);
        var lineBot = new Vector2(0, 0);
        var index = 0;
        var flagPaired = false;

        while (tempList.Count > 0) // loop untill every area inside the path is conquered.
        {
            for (int i = 0; i < tempList.Count; i++) // walk through the coordinates to define the top right and left 
            {
                // find the top right line and the line below it
                if (((tempList[i].x > lineTop.x) || (tempList[i].x == lineTop.x && tempList[i].y > lineTop.y)) && flagPaired == false)
                {
                    lineTop = tempList[i];
                    index = i;
                }
                else if (((tempList[i].x > lineBot.x) || (tempList[i].x == lineBot.x && tempList[i].y > lineBot.y)) && flagPaired == true)
                {
                    lineBot = tempList[i];
                    index = i;
                }
            }

            tempList.RemoveAt(index); // once the line is found remove it from the path

            if (flagPaired)
            {
                while (true)
                {
                    if (lineTop.y - 5f == lineBot.y)
                    {
                        lineTop = new Vector2(0, 0);
                        lineBot = new Vector2(0, 0);
                        break;
                    }

                    if (GameObject.Find("L_" + (lineTop.x).ToString() + "_" + (lineTop.y - 5f).ToString()) != null) // if the area is not already occupied
                        Destroy(GameObject.Find("L_" + (lineTop.x).ToString() + "_" + (lineTop.y - 5f).ToString()));
                    if(GameObject.Find("B_" + lineTop.x.ToString() + "_" + (lineTop.y - 5f).ToString()) != null)
                        Destroy(GameObject.Find("B_" + lineTop.x.ToString() + "_" + (lineTop.y - 5f).ToString()));

                    RemoveDrawableIfExists(new Vector2(lineTop.x, lineTop.y - 5f));

                    lineTop.y -= 5; // shift the right line towards the left line
                }
            }

            flagPaired ^= true; // toggle flag to true after the bot line is found
        }
    }

    /// <summary>
    /// This function removes all the drawables that are stored
    /// inside a conquered area. The player shouldnt be able to draw
    /// ant lines inside a occupied loop.
    /// </summary>
    /// <param name="coordinates"> The coordinates of drawable line the program will check </param>
    private void RemoveDrawableIfExists(Vector2 coordinates)
    {
        var storedDrawables = PlayerProperties.playerDrawableLines;

        for (int i = 1; i <= playerNumber; i++) // check for all the players
        {
            var v = new Vector3(coordinates.x, coordinates.y, i);

            storedDrawables.Remove(v); // remove if any found
        }
    }

    /// <summary>
    /// This function checks if the postion is already stored in the path.
    /// </summary>
    /// <param name="path"> The path of lines </param>
    /// <param name="pos"> the position that is being checked </param>
    /// <returns> True if the coordinate is repeating </returns>
    bool CheckRepeatedPath(List<PointCheckUp> path, Vector2 pos)
    {
        foreach (PointCheckUp point in path)
            if (point.position == pos) return true; // the position is found within the path

        return false; // the position is not stored in the path yet
    }

    /// <summary>
    /// This function fills the closed path with planes colored accordintly the current players who conquered it.
    /// Starting from the top right line, the program scans throught left and fills the areas untill another line is detected.
    /// This function also removes the verticle lines and barricades within the occupied land.
    /// </summary>
    /// <param name="path"> The coordinates of verticle lines </param>
    /// <param name="playerTurn"> Indicates which player`s turn it is </param>
    void OccupyLands(List<Vector2> path, int playerTurn)
    {
        var lineTopRight = new Vector2(0, 0); // the top right line
        var lineTopLeft = new Vector2(0, 0); // the line on left side of the top right line
        var flagOccupy = false; // this flag tells the program if the arease should be occupied or not
        var areaConqueredAtOnce = 0;
        var index = 0;

        while (path.Count > 0) // loop untill every area inside the path is conquered.
        {
            for (int i = 0; i < path.Count; i++) // scan through the coordinates to define the top right and left 
            {
                // find the top right line and the line next to it
                if (((path[i].y > lineTopRight.y) || (path[i].y == lineTopRight.y && path[i].x > lineTopRight.x)) && flagOccupy == false)
                {
                    lineTopRight = path[i];
                    index = i;
                }
                else if (((path[i].y > lineTopLeft.y) || (path[i].y == lineTopLeft.y && path[i].x > lineTopLeft.x)) && flagOccupy == true)
                {
                    lineTopLeft = path[i];
                    index = i;
                }
            }

            path.RemoveAt(index); // once the line is found remove it from the path

            // wait until both lines are found, do not occupy if one of the line pairs are missing
            if (flagOccupy)
            {
                

                while (true) // occupy all the lands between the two line pair
                {
                    if (GameObject.Find("A_" + (lineTopRight.x - 2.5f).ToString() + "_" + (lineTopRight.y).ToString()) == null) // if the area is not already occupied
                    {
                        OccupyLand(new Vector2(lineTopRight.x - 2.5f, lineTopRight.y), playerTurn); // occupy the area with the specified coordinate

                        // YOU CAN MAKE YOUR SCORE CALCULATION HERE !!!
                        areaConqueredAtOnce ++;
                        PlayerProperties.playerDominationPercentage[playerTurn] += 0.25f;
                    }

                    if (lineTopRight.x - 5f == lineTopLeft.x) // when the right line overlaps the left line, reset the stored values and end the loop
                    {
                        lineTopRight = new Vector2(0, 0);
                        lineTopLeft = new Vector2(0, 0);
                        break;
                    }

                    if (GameObject.Find("L_" + (lineTopRight.x - 5f).ToString() + "_" + (lineTopRight.y).ToString()) != null) // if the area is not already occupied
                        Destroy(GameObject.Find("L_" + (lineTopRight.x - 5f).ToString() + "_" + (lineTopRight.y).ToString()));
                    if (GameObject.Find("B_" + (lineTopRight.x - 5f).ToString() + "_" + (lineTopRight.y).ToString()) != null) // check if there is any barricades drawen in the occupied area
                        Destroy(GameObject.Find("B_" + (lineTopRight.x - 5f).ToString() + "_" + (lineTopRight.y).ToString())); // remove the barricade in the occupied area

                    RemoveDrawableIfExists(new Vector2(lineTopRight.x - 5f, lineTopRight.y));

                    lineTopRight.x -= 5; // shift the right line towards the left line
                }
            }

            flagOccupy ^= true; // toggle flag to true after the left line is found
        }
        var skillPoint = PlayerProperties.playerSkillPoints[playerTurn] + areaConqueredAtOnce * areaConqueredAtOnce;

        if (skillPoint > 1000)
            skillPoint = 1000;
        PlayerProperties.playerSkillPoints[playerTurn] = skillPoint;

        AnimationManager.flagRefreshSkillPoint = true;
    }

    /// <summary>
    /// This function occupys the land if its surrounded.
    /// Its a temproary function it is going to change
    /// </summary>
    /// <param name="coordinate"> The land coordinate that is conquered </param>
    void OccupyLand(Vector2 coordinate, int playerTurn)
    {
        planeObject = Instantiate(planePrefab, new Vector3(coordinate.x, coordinate.y, 0), planePrefab.rotation) as Transform; // create the line
        planeObject.name = "A_" + coordinate.x.ToString() + "_" + coordinate.y.ToString();
        planeObject.transform.parent = planes.transform;
        planeObject.tag = "PlayerArea" + playerTurn;

        rend = planeObject.GetComponent<Renderer>();

        // this damn part cost my 2.5 hours... it is a bug, thanks to this answer http://answers.unity3d.com/questions/974388/fading-objects-does-not-work-unity-5.html
        // set alpha value of a material on runtime 
        Color _color = playerColor[playerTurn]; // store the color
        _color.a = fadedMaterial.color.a; // store the alpha value
        rend.material = fadedMaterial; // use the dummy material
        rend.material.color = _color;   // assign the stored rgba value

        var turnTag = new GameObject(PlayerProperties.turnPassed.ToString()); // create the empty parent object
        turnTag.transform.parent = planeObject.transform;
    }
}
