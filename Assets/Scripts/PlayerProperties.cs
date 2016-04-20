using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class PlayerProperties : MonoBehaviour {

    public static int playerAmount; // the number of players
    public static int playerTurn; // the variable indicating whose turn is it
    public static int[] playerSkillPoints;
    public static float[] playerDominationPercentage;
    public static string[] playerNames; // this list stores the player names
    public static Color[] playerColor; // this array will store each players color
    public static Vector2[] playerCameraLocation; // this array will store each players starting location
    public static List<Vector3> playerDrawableLines; // this list will hold the drawable lines and will refresh each turn. The first two parameters is the corrdinates whereas the last parameter shows which player owns it
    public static List<Vector3> playerBorder; // this list will hold the players border line coordinates and will refresh each turn. The first two parameters is the corrdinates whereas the last parameter shows which player owns it
    public static bool flagEndTurn = false; // this flag rises up when turn is finished
    public static int turnPassed;
    public Text[] playerStats;

    private Vector2[] tempArray;
    private bool flagMoveCamera = false; // this flag rises when ever the camera postion moves from one player to another
    private float cameraMoveStartTime; // captures the time before the camera movement function starts
    private float cameraMoveJourneyLength; // stores the distance between two different camera postions
    private float[] textPositions;
    
    // Use this for initialization
	void Start () 
    {
        var referenceTextPos = (int)playerStats[1].transform.position.y;
        turnPassed = 1; // game starts from turn number 1

        // instantiate the lists
        playerDrawableLines = new List<Vector3>(); 
        playerBorder = new List<Vector3>();
        
        playerTurn = 1; // set which player starts first
        playerAmount = 4; // set the number of players

        playerColor = new Color[] {Color.gray, Color.white, Color.blue, Color.red, Color.green, Color.black, Color.yellow, Color.magenta, Color.cyan}; // define all the colors for each player
        textPositions = new float[] { referenceTextPos + Screen.height * 0.5f, referenceTextPos, referenceTextPos - Screen.height * 0.05f, referenceTextPos - Screen.height * 0.1f, referenceTextPos - Screen.height * 0.15f, referenceTextPos - Screen.height * 0.2f, referenceTextPos - Screen.height * 0.25f, referenceTextPos - Screen.height * 0.3f, referenceTextPos - Screen.height * 0.35f };
        playerDominationPercentage = new float[playerAmount + 1];
        tempArray = new Vector2[playerAmount + 1];
        playerSkillPoints = new int[playerAmount + 1];

        InstantiateSpawnPoints(); // instantiate all arrays that is relevent to spawn points
        InitializePlayerStats();

        Camera.main.transform.position = new Vector3(playerCameraLocation[playerTurn].x, playerCameraLocation[playerTurn].y, -25.0f); // camera position is set for the first player to start
    }
	
	// Update is called once per frame
	void Update () {

        RefreshStats();

        if (flagEndTurn) // enter if the turn ended
        {
            playerCameraLocation[playerTurn] = Camera.main.transform.position; // store players camera position so whenever turn comes back to player camera will be set where player left it as

            if (playerTurn == playerAmount) 
            {
                cameraMoveJourneyLength = Vector3.Distance(new Vector3(playerCameraLocation[playerTurn].x, playerCameraLocation[playerTurn].y, -25.0f), new Vector3(playerCameraLocation[1].x, playerCameraLocation[1].y, -25.0f));
                turnPassed++;
                playerTurn = 1;
            }
            else
            {
                cameraMoveJourneyLength = Vector3.Distance(new Vector3(playerCameraLocation[playerTurn].x, playerCameraLocation[playerTurn].y, -25.0f), new Vector3(playerCameraLocation[playerTurn + 1].x, playerCameraLocation[playerTurn + 1].y, -25.0f));
                playerTurn ++;
            }

            cameraMoveStartTime = Time.time;// capture the camera start time
            flagMoveCamera = true;
            SkillManager.flagTurnBeggins = true;

            flagEndTurn = false;
        }

        if (flagMoveCamera)
            MoveCamera(cameraMoveStartTime, cameraMoveJourneyLength); // move the camera to the next players view
    }

    void RefreshStats()
    {
        playerStats[playerTurn].text = "Player_" + playerTurn + ", " + playerDominationPercentage[playerTurn] + "%, " + playerSkillPoints[playerTurn];

        for (int i = 0; i < playerDominationPercentage.Length; i++)
            tempArray[i] = new Vector2 (playerDominationPercentage[i], i);

        IntArrayBubbleSort(tempArray);

        for (int i = 1; i <= playerAmount; i++)
        {
            playerStats[(int)tempArray[i].y].transform.position = new Vector3(playerStats[0].transform.position.x, textPositions[i], 0);
        }
    }

    void IntArrayBubbleSort(Vector2[] data)
    {
        int i, j;
        int N = data.Length;

        for (j = N - 1; j > 1; j--)
        {
            for (i = 1; i < j; i++)
            {
                if (data[i].x < data[i + 1].x)
                    exchange(data, i, i + 1);
            }
        }
    }

    void exchange(Vector2[] data, int m, int n)
    {
        Vector2 temporary;

        temporary = data[m];
        data[m] = data[n];
        data[n] = temporary;
    }

    /// <summary>
    /// This function will set the stat section on the UI. Each
    /// player will have a line that shows, player name, domination
    /// rate and skill points colored each with unique player color
    /// </summary>
    void InitializePlayerStats()
    {
        for(int i = 1; i<= playerAmount; i++)
        {
            playerStats[i].text = "Player_" + i + ", " + playerDominationPercentage[i] + "%, " + playerSkillPoints[i];
            playerStats[i].color = playerColor[i];
        }
        
    }

    /// <summary>
    /// This function moves the camera position from the player played its turn to the player currently playing
    /// Here speed speed and distance calculations are made which defines the camera movement behaviour
    /// </summary>
    /// <param name="startTime">The time where camera starts to move</param>
    /// <param name="journeyLength">The distance between the starting position and destination position</param>
    void MoveCamera(float startTime, float journeyLength)
    {        
        float distCovered = (Time.time - startTime) * 200.0f; // speed value
        float fracJourney = distCovered / journeyLength; // the change in this variable defines the camera movement speed

        if (playerTurn == 1)
            Camera.main.transform.position = Vector3.Lerp(new Vector3(playerCameraLocation[playerAmount].x, playerCameraLocation[playerAmount].y, -25.0f), new Vector3(playerCameraLocation[1].x, playerCameraLocation[1].y, -25.0f), fracJourney); // moves the camera
        else
            Camera.main.transform.position = Vector3.Lerp(new Vector3(playerCameraLocation[playerTurn - 1].x, playerCameraLocation[playerTurn - 1].y, -25.0f), new Vector3(playerCameraLocation[playerTurn].x, playerCameraLocation[playerTurn].y, -25.0f), fracJourney); // moves the camera
        
        if(fracJourney > 1)
        {
            flagMoveCamera = false; // stop the movement
        }
    }

    
    /// <summary>
    /// This function defines the spawn points for each player.
    /// Also adds the first drawables to the list of drawables.
    /// Currently, it is not working dynamically everything is 
    /// defined for the current level. 
    /// </summary>
    void InstantiateSpawnPoints()
    {
        playerCameraLocation = new [] { new Vector2 (50, 50), // does not belong to anyone
                                        new Vector2 (7.5f, 7.5f), // Player 1 camera pos setup
                                        new Vector2 (92.5f, 92.5f), // Player 2 camera pos setup
                                        new Vector2(7.5f, 92.5f), // Player 3 camera pos setup
                                        new Vector2(92.5f, 7.5f), // Player 4 camera pos setup
                                        new Vector2(7.5f, 50), // Player 5 camera pos setup
                                        new Vector2 (50, 7.5f), // Player 6 camera pos setup
                                        new Vector2(50, 92.5f), // Player 7 camera pos setup
                                        new Vector2(92.5f, 50)}; // Player 8 camera pos setup

        for (int i = 1; i <= playerAmount; i++) // add the drawable lines for each player
        {
            playerDrawableLines.Add(new Vector3(playerCameraLocation[i].x, playerCameraLocation[i].y - 2.5f, i));
            playerDrawableLines.Add(new Vector3(playerCameraLocation[i].x - 2.5f, playerCameraLocation[i].y, i));
            playerDrawableLines.Add(new Vector3(playerCameraLocation[i].x + 2.5f, playerCameraLocation[i].y, i));
            playerDrawableLines.Add(new Vector3(playerCameraLocation[i].x, playerCameraLocation[i].y + 2.5f, i));
        }
    }
}
