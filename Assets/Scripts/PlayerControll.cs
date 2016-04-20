using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class PlayerControll : MonoBehaviour
{
    public float moveSensitivityX = 1.0f; // Camera draging sensivity on x dimention
    public float moveSensitivityY = 1.0f; // Camera draging sensivity on y dimention
    public float orthoZoomSpeed = 1.05f; // camera zoom speed
    public float minZoom = 1.0f; // the minimum amount of zoom
    public float maxZoom = 20.0f; // the maximum amount of zoom
    public float drawableLimit = 1.0f; // this limitation separetes the user from scrolling the camera and drawing a line
    public bool updateZoomSensitivity = true;
    public bool invertMoveX = false; // inverts the dragging on x dimension
    public bool invertMoveY = false; // inverts the dragging on y dimension
    public Transform linePrefab; // this object will be used when instantiating the line
    public Transform barricadePrefab; // this object will be used when instantiating the barricades

    private GameObject lines; // this is the parent object of the lines, so everything will be more organized
    private GameObject barricades; // this is the parent object of the barricades, so everything will be more organized
    private bool isDrawable = true; // this boolean will seperate the action of drawing a line or scrolling the map

    /// These variables are related to the convert skill function
    private bool flagCheckConvertSkill; // this flag will rise when convert skill is applied
    private bool flagReportLineDrawn; // this flag is used to communicate between the convert function states
    private int convertSkillCounter; // this counter is used for convert function to apply certain action on different frames
    private Vector2 convertLandPos; // the land that is wanted to be converted

    // Use this for initialization
	void Start () 
    {
        convertSkillCounter = 0;
        flagReportLineDrawn = false;
        flagCheckConvertSkill = false;

        lines = new GameObject("Lines"); // create the empty parent object
        barricades = new GameObject("Barricades"); // create the empty parent object
    }
	
	// Update is called once per frame
	void Update () 
    {
        if (updateZoomSensitivity)
        {
            // set sensitivity
            moveSensitivityX = Camera.main.orthographicSize / 2.5f; 
            moveSensitivityY = Camera.main.orthographicSize / 2.5f;
        }

        if (flagCheckConvertSkill) // if convert skill is applied enter
            ConvertLand(PlayerProperties.playerTurn); // convert the selected land

        Touch[] touches = Input.touches; // store every touch to an array

        if (touches.Length > 0) // at least 1 finger is touching the screen
        {
            Vector3 touchPos; // the pixel that is being touched
            Vector2 worldPos; // pixel converted to coordinates

            touchPos = Input.GetTouch(0).position; // get the touch position as pixels
            touchPos.z = 25; // z position must be set since screen is in 2D

            worldPos = Camera.main.ScreenToWorldPoint(touchPos); // convert pixels to coordinates

            // Single Touch
            if (touches.Length == 1) // if only 1 finger is touching
            {
                if (touches[0].phase == TouchPhase.Moved) // enters if draging occurs
                {
                    Vector2 delta = touches[0].deltaPosition; // store the vector that shows the change in a single frame
                    if (delta.magnitude > drawableLimit) // if the dragging is in high amounts dont draw lines
                        isDrawable = false;

                    float positionX = delta.x * moveSensitivityX * Time.deltaTime; // compute the x postion accordung to the touch
                    positionX = invertMoveX ? positionX : positionX * -1;

                    float positionY = delta.y * moveSensitivityY * Time.deltaTime; // compute the y postion accordung to the touch
                    positionY = invertMoveY? positionY : positionY * -1;

                    Camera.main.transform.position += new Vector3(positionX, positionY, 0); // set the new camera position 
                }

                else if (touches[0].phase == TouchPhase.Ended)
                {

                    if (CheckIfButtonIsPressed())
                        Debug.Log("Was just a damned button.");

                    else if (isDrawable)
                    {
                        var playerTurn = PlayerProperties.playerTurn; // store the current player turn to access it locally
                        PlayerAction(worldPos, playerTurn); // draw the line
                    }

                    isDrawable = true;
                }
            }

            //Double Touch
            if (touches.Length == 2) // enters when there is 2 fingers touching 
            {
                Touch touchOne = touches[0]; 
                Touch touchTwo = touches[1];

                Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition; //store the first fingers previous frame touch position
                Vector2 touchTwoPrevPos = touchTwo.position - touchTwo.deltaPosition; //store the second fingers previous frame touch position

                float prevTouchDeltaMag = (touchOnePrevPos - touchTwoPrevPos).magnitude; // compute previous frames distance between two fingers
                float touchDeltaMag = (touchOne.position - touchTwo.position).magnitude; // compute current frames distance between two fingers

                float deltaMagDiff = prevTouchDeltaMag - touchDeltaMag; // check if the fingers are getting closer or further

                Camera.main.orthographicSize += deltaMagDiff * orthoZoomSpeed; // zoom accordingly the distance change between each finger
                Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, minZoom, maxZoom); // zoom within the limits
            }
        }
	}

    /// <summary>
    /// This function checks if the player pressed on a button, so that 
    /// the actions wont pass through the button.
    /// </summary>
    /// <returns></returns>
    bool CheckIfButtonIsPressed()
    {
        UnityEngine.EventSystems.EventSystem ct = UnityEngine.EventSystems.EventSystem.current;

        if (!ct.IsPointerOverGameObject()) return false;
        if (!ct.currentSelectedGameObject) return false;
        if (ct.currentSelectedGameObject.GetComponent<Button>() == null)
            return false;

        return true;
    }

    /// <summary>
    /// This function is called whenever a skill is applied, the information 
    /// of the current skill option comes from the skill manager to this function.
    /// If the player touch is close enought to the skill guide line, apply the skill.
    /// </summary>
    /// <param name="pos"> the position of the players touch </param>
    /// <param name="playerTurn"> The player drawing the line </param>
    void PlayerAction(Vector2 pos, int playerTurn)
    {
        float minDistance = 1.75f; // the touch coordinates must be within this range to apply the skill

        switch(SkillManager.currentSkill) // the current skill choosen by the player
        {
            case SkillManager.PlayerSkills.Natural: // this is the basic skill which draws line
                var drawableTaggedObjects = GameObject.FindGameObjectsWithTag("DrawableLines"); // get all the drawable lines

                foreach (GameObject d in drawableTaggedObjects)
                {
                    if (Vector2.Distance(d.transform.position, pos) < minDistance) // if the touch is close enought to the drawable position enter
                    {
                        if (DrawLine(playerTurn, d.transform.position)) // draw the line and enter if it is succeed
                        {
                            SkillManager.grantedLineNumber--; // the player has one less line to draw
                            InformOtherScripts(d.transform.position, playerTurn); // apply adding new drawables and inform the loop manager to seek for any possible loops
                        }
                    }
                }
                break;
            case SkillManager.PlayerSkills.Barricade: // this skill creates a barricade
                var barricadeDrawables = GameObject.FindGameObjectsWithTag("DrawableLines"); // get all the drawable lines

                foreach (GameObject d in barricadeDrawables)
                {
                    if (Vector2.Distance(d.transform.position, pos) < minDistance) // if the touch is close enought to the drawable position enter
                    {
                        PlaceBarricade(playerTurn, d.transform.position); // create a barricade
                        SkillManager.flagSkillApplied = true; // report the skillmanager that the skill is applied 
                    }
                }
                break;
            case SkillManager.PlayerSkills.Cluster: // this skill helps player to start building lines from a diffrent location
                var clusterEffects = GameObject.FindGameObjectsWithTag("ClusterEffect"); // get all the drawable lines

                foreach (GameObject ce in clusterEffects)
                {
                    if (Vector2.Distance(ce.transform.position, pos) < minDistance) // if the touch is close enought to the drawable position enter
                    {
                        AddNewDrawablesFromClusterEffect(ce.transform.position, playerTurn); // add the choosen area to drawables
                        SkillManager.flagSkillApplied = true; // report the skillmanager that the skill is applied 
                    }
                }
                
                break;
            case SkillManager.PlayerSkills.Convert: // this skill directly converts an area
                var convertEffects = GameObject.FindGameObjectsWithTag("ConvertEffect"); // get all the drawable lines

                foreach (GameObject conv in convertEffects)
                {
                    if (Vector2.Distance(conv.transform.position, pos) < minDistance) // if the touch is close enought to the drawable position enter
                    {
                        flagCheckConvertSkill = true; // raise the convert skill flag 
                        convertLandPos = conv.transform.position; // capture the area where convert skill is going to be applied
                    }
                }

                break;
        }
        
    }

    /// <summary>
    /// This skill places an barricade to a location so that the enemy players 
    /// wont be able to draw any line to that spot. The enemy barricades only 
    /// can be removed with the pierce skill.
    /// </summary>
    /// <param name="playerTurn"> current player`s turn</param>
    /// <param name="pos"> the location where the barricade is to place </param>
    void PlaceBarricade(int playerTurn, Vector2 pos)
    {
        var barricadeObject = Instantiate(barricadePrefab, new Vector3(pos.x, pos.y, 0), Quaternion.identity) as Transform; // create the barricade
        barricadeObject.name = "B_" + pos.x.ToString() + "_" + pos.y.ToString(); // change the lines name, 'B' stands for barricade
        barricadeObject.tag = "Barricade" + playerTurn.ToString();  // give this object a tag so it will be easy locate it
        barricadeObject.transform.parent = barricades.transform; // place them under the parent object

        foreach (Transform t in barricadeObject)
        {
            var rend = t.GetComponent<Renderer>();
            rend.material.color = PlayerProperties.playerColor[playerTurn]; // change the color according to who draw it
        }

        if (Mathf.Abs(Mathf.Floor(pos.x) - pos.x) == 0.5) // enters here if the barricade is horizontal
            barricadeObject.transform.eulerAngles = new Vector3(0, 0, 90); // rotate the angle to horizontal
        
    }

    /// <summary>
    /// This function draws a line on the specified location. It wont succeed if
    /// there is an enemy barricade on the way. After the line is drawn, the drawables 
    /// on the same location will be removed from the drawable line list.
    /// </summary>
    /// <param name="playerTurn"> the current player`s turn </param>
    /// <param name="drawablePosition"> the postion where the line is wanted to be drawn </param>
    /// <returns> returns true if the drawing is successfull, false otherwise </returns>
    bool DrawLine(int playerTurn, Vector2 drawablePosition)
    {
        var barricade = GameObject.Find("B_" + drawablePosition.x.ToString() + "_" + drawablePosition.y.ToString()); // get if there is any barricade on the postion where line is wanted to be drawn

        if (barricade != null) // if there is a barricade enter
        {
            if (barricade.tag == "Barricade" + playerTurn) // if the barricade belongs to the player itself enter
                Destroy(barricade); // destroy the barricade an continue to draw the line
            else return false; // return false because the barricade belongs to an enemy
        }

        for (int i = 1; i <= PlayerProperties.playerAmount; i++)
            PlayerProperties.playerDrawableLines.Remove(new Vector3(drawablePosition.x, drawablePosition.y, i)); // after replacing drawable with line, remove the drawable from the list of drawables

        var lineObject = Instantiate(linePrefab, new Vector3(drawablePosition.x, drawablePosition.y, 0), Quaternion.identity) as Transform; // create the line
        lineObject.name = "L_" + drawablePosition.x.ToString() + "_" + drawablePosition.y.ToString(); // change the lines name, 'L' stands for line
        lineObject.tag = "PlayerLines" + playerTurn;  // give this object a tag so it will be easy locate it
        lineObject.transform.parent = lines.transform; // place them under the parent object
        
        var rend = lineObject.GetComponent<Renderer>();
        rend.material.color = PlayerProperties.playerColor[playerTurn]; // change the color according to who draw it

        if (Mathf.Abs(Mathf.Floor(drawablePosition.x) - drawablePosition.x) == 0.5) // enters here if the line is horizontal
            lineObject.transform.eulerAngles = new Vector3(0, 0, 90);
        return true;
    }

    /// <summary>
    /// This function adds the surrounding line coordinates of the choosen area to drawable list 
    /// </summary>
    /// <param name="pos"> the choosen area </param>
    /// <param name="playerTurn"> the turn of the current player </param>
    void AddNewDrawablesFromClusterEffect(Vector2 pos, int playerTurn)
    {
        PlayerProperties.playerDrawableLines.Add(new Vector3(pos.x, pos.y - 2.5f, playerTurn));
        PlayerProperties.playerDrawableLines.Add(new Vector3(pos.x - 2.5f, pos.y, playerTurn));
        PlayerProperties.playerDrawableLines.Add(new Vector3(pos.x + 2.5f, pos.y, playerTurn));
        PlayerProperties.playerDrawableLines.Add(new Vector3(pos.x, pos.y + 2.5f, playerTurn));
    }

    /// <summary>
    /// This skill draws lines surrounding where ever the player has choosen to convert.
    /// Every line is drawen in two frames, so that the loop manager will be able to finish
    /// all the calculation within that time. The lines are drawen with the same method as the
    /// player draws line by using its basic draw line skill. The skill will not work if there is 
    /// an enemy barricade on the way, so the player should be carefull not to select an area where 
    /// enemy barricades are included. THIS FUNCTION WILL CHANGE BECAUSE IT IS TERRIBLY WRITTEN.
    /// </summary>
    /// <param name="playerTurn"></param>
    void ConvertLand(int playerTurn)
    {
        if (LoopManager.flagLoopDetected) // if loop is detected by the loopmanager then the skill is applied, so enter here
        {
            LoopManager.flagLoopDetected = false; // set detection to false to stay in a listening position
            convertSkillCounter = 8; // set the counter to the final state number
        }

        if (convertSkillCounter == 0 && // enter to draw the right side line
            GameObject.Find("L_" + (convertLandPos.x + 2.5f).ToString() + "_" + convertLandPos.y.ToString()) == null)
        {
            DrawLine(playerTurn, new Vector2(convertLandPos.x + 2.5f, convertLandPos.y)); // draw the line
            flagReportLineDrawn = true; // the line is drawn, this will enable the next section
        }
        else if (convertSkillCounter == 1 && flagReportLineDrawn) // if the previous section line is drawn the program will enter here
        {
            InformOtherScripts(new Vector2(convertLandPos.x + 2.5f, convertLandPos.y), playerTurn); // since the line is drawn on the previous section, inform the loop manager to find any possible loops
            convertSkillCounter++; // increase the counter to proceed to the next state
            flagReportLineDrawn = false; 
        }
        else if (convertSkillCounter == 2 && // enter to draw the left side line
            GameObject.Find("L_" + (convertLandPos.x - 2.5f).ToString() + "_" + convertLandPos.y.ToString()) == null)
        {
            DrawLine(playerTurn, new Vector2(convertLandPos.x - 2.5f, convertLandPos.y));// draw the line
            flagReportLineDrawn = true; // the line is drawn, this will enable the next section
        }
        else if (convertSkillCounter == 3 && flagReportLineDrawn) // if the previous section line is drawn the program will enter here
        {
            InformOtherScripts(new Vector2(convertLandPos.x - 2.5f, convertLandPos.y), playerTurn); // since the line is drawn on the previous section, inform the loop manager to find any possible loops
            convertSkillCounter++; // increase the counter to proceed to the next state
            flagReportLineDrawn = false;
        }
        else if (convertSkillCounter == 4 && // enter to draw the top side line
            GameObject.Find("L_" + convertLandPos.x.ToString() + "_" + (convertLandPos.y + 2.5f).ToString()) == null)
        {
            DrawLine(playerTurn, new Vector2(convertLandPos.x, convertLandPos.y + 2.5f));// draw the line
            flagReportLineDrawn = true; // the line is drawn, this will enable the next section
        }
        else if (convertSkillCounter == 5 && flagReportLineDrawn) // if the previous section line is drawn the program will enter here
        {
            InformOtherScripts(new Vector2(convertLandPos.x, convertLandPos.y + 2.5f), playerTurn); // since the line is drawn on the previous section, inform the loop manager to find any possible loops
            convertSkillCounter++; // increase the counter to proceed to the next state
            flagReportLineDrawn = false;
        }
        else if (convertSkillCounter == 6 && // enter to draw the bottom side line
            GameObject.Find("L_" + convertLandPos.x.ToString() + "_" + (convertLandPos.y - 2.5f).ToString()) == null)
        {
            DrawLine(playerTurn, new Vector2(convertLandPos.x, convertLandPos.y - 2.5f));// draw the line
            flagReportLineDrawn = true; // the line is drawn, this will enable the next section
        }
        else if (convertSkillCounter == 7 && flagReportLineDrawn) // if the previous section line is drawn the program will enter here
        {
            InformOtherScripts(new Vector2(convertLandPos.x, convertLandPos.y - 2.5f), playerTurn); // since the line is drawn on the previous section, inform the loop manager to find any possible loops
            convertSkillCounter++; // increase the counter to proceed to the next state
            flagReportLineDrawn = false;
        }
        else if (convertSkillCounter == 8) // enter to exit the function
        {
            flagCheckConvertSkill = false; // set detection to false to stay in a listening position
            convertSkillCounter = 0; // set the counter to the starting position
            SkillManager.flagSkillApplied = true; // inform the skillmanager that the skill is applied
            return;
        }

        if (!LoopManager.flagCheckForLoops) convertSkillCounter ++; // increase the counter to proceed to the next state
    }

    /// <summary>
    /// This function is a helper function to add the drawables to the list for both sides of a line
    /// </summary>
    /// <param name="pos"> The position of the line </param>
    /// <param name="playerTurn"> the current player turn </param>
    void AddNewDrawables(Vector2 pos, int playerTurn)
    {
        if (Mathf.Abs(Mathf.Floor(pos.x) - pos.x) != 0.5) // the line is verticle
        {
            AddNeighboorsAsDrawables(new Vector2(pos.x, pos.y + 2.5f), playerTurn); // sen the top corner location of the line to add neightboors as drawables
            AddNeighboorsAsDrawables(new Vector2(pos.x, pos.y - 2.5f), playerTurn); // sen the bottom corner location of the line to add neightboors as drawables
        }
        else // the line is horizontal
        {
            AddNeighboorsAsDrawables(new Vector2(pos.x + 2.5f, pos.y), playerTurn); // sen the right corner location of the line to add neightboors as drawables
            AddNeighboorsAsDrawables(new Vector2(pos.x - 2.5f, pos.y), playerTurn); // sen the left corner location of the line to add neightboors as drawables
        }
    }

    /// <summary>
    /// This function expands the drawable points after a line is drawn. The expension is made acording to,
    /// the corner point position. It adds left, right, up and down line coordinates, which is adjacent 
    /// to the selected corner, as drawable lines. This function uses the singleton pattern to prevent any duplicate records. 
    /// </summary>
    /// <param name="cornerPostion"> the corner postion </param>
    /// <param name="playerTurn"> the current player turn </param>
    void AddNeighboorsAsDrawables(Vector2 cornerPostion, int playerTurn)
    {
        if ((cornerPostion.x - 2.5) >= 0 && SatisfyDrawableConditions(new Vector2(cornerPostion.x - 2.5f, cornerPostion.y), playerTurn))
            PlayerProperties.playerDrawableLines.Add(new Vector3(cornerPostion.x - 2.5f, cornerPostion.y, playerTurn)); // add the line postion which is left to the corner

        if ((cornerPostion.x + 2.5) <= 100 && SatisfyDrawableConditions(new Vector2(cornerPostion.x + 2.5f, cornerPostion.y), playerTurn))
            PlayerProperties.playerDrawableLines.Add(new Vector3(cornerPostion.x + 2.5f, cornerPostion.y, playerTurn)); // add the line postion which is right to the corner

        if ((cornerPostion.y - 2.5) >= 0 && SatisfyDrawableConditions(new Vector2(cornerPostion.x, cornerPostion.y - 2.5f), playerTurn))
            PlayerProperties.playerDrawableLines.Add(new Vector3(cornerPostion.x, cornerPostion.y - 2.5f, playerTurn)); // add the line postion which is top of the corner

        if ((cornerPostion.y + 2.5) <= 100 && SatisfyDrawableConditions(new Vector2(cornerPostion.x, cornerPostion.y + 2.5f), playerTurn))
            PlayerProperties.playerDrawableLines.Add(new Vector3(cornerPostion.x, cornerPostion.y + 2.5f, playerTurn)); // add the line postion which is bottom of the corner
    }

    /// <summary>
    /// This function checks if the position is eligable to be added to the drawable list.
    /// To be eligable ther shouldnt be any lines, barricades or drawables to be drawen.
    /// Also there shouldnt be any areas conquered on the neighboor of the line postion.
    /// The last condition about area is only being checked for any possible mistake, it should not 
    /// effect anything if it is removed.
    /// </summary>
    /// <param name="pos"> the line location which is wanted to be added to the darawble list </param>
    /// <param name="playerTurn"> the current player turn </param>
    /// <returns> return true if eligable, false otherwise </returns>
    bool SatisfyDrawableConditions(Vector2 pos, int playerTurn)
    {
        if (GameObject.Find("L_" + pos.x.ToString() + "_" + pos.y.ToString()) == null && // there is no line drawn on the location
            !PlayerProperties.playerDrawableLines.Contains(new Vector3(pos.x, pos.y, playerTurn)) && // this location is not already in the list
            GameObject.Find("A_" + pos.x.ToString() + "_" + (pos.y + 2.5).ToString()) == null && // area is not conquered on the top side (for horizontal lines)
            GameObject.Find("A_" + (pos.x + 2.5).ToString() + "_" + pos.y.ToString()) == null && // area is not conquered on the right side (for verticle lines)
            GameObject.Find("B_" + pos.x.ToString() + "_" + pos.y.ToString()) == null) // there is no barricade on the location
            return true;
        else return false;
    }

    /// <summary>
    /// After a line is drawn inform the loop manager for any possible loops and
    /// add the neighboor lines to the drawable list.
    /// </summary>
    /// <param name="pos"> The position of the drawn line </param>
    /// <param name="playerTurn"> the current player turn </param>
    void InformOtherScripts(Vector2 pos, int playerTurn)
    {
        AddNewDrawables(pos, playerTurn); // add the expanded drawables to the list
        // these parameters will help the game manager to locate if the player has invaded any land
        LoopManager.lineLocation = pos; // the last drawn line location 
        LoopManager.flagCheckForLoops = true; // send a check command
    }
}
