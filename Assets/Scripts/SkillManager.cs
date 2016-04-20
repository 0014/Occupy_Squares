using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SkillManager : MonoBehaviour {

    public static int grantedLineNumber; // the remaining number of line that the player can draw
    public static bool flagTurnBeggins; // the flag that indicates the beginning of the turn
    public static bool flagSkillApplied; // this flag tells this script if a skill is applied or not
    public static PlayerSkills currentSkill; // the players selected skill
    public int skillPointIncreaseAmount;
    public Transform drawableLinePrefab; // this object will be used when instantiating the line
    public Transform clusterEffectPrefab; // this object will be used when instantiating the cluster effect
    public Transform convertEffectPrefab; //  this object will be used when instantiating the convert effect
    public Material fadedMaterial; // this material will be used when the object needs to be transparent

    private GameObject drawables; // this is the parent object of the lines, so everything will be more coordinated 
    private GameObject cluster; // this is the parent object of the lines, so everything will be more coordinated 
    private GameObject convert; // this is the parent object of the lines, so everything will be more coordinated 
    private Color[] playerColor; // this array will store each players color
    private bool flagApplySkill;
    private int[] skillCosts;
    private int frameCounter;

    public enum PlayerSkills {Natural, Pierce, Convert, Cluster, Barricade};

    // Use this for initialization
	void Start ()
    {
        playerColor = PlayerProperties.playerColor; // store the colors on start so we can access this variable locally
        drawables = new GameObject("Drawables"); // create the empty parent object
        cluster = new GameObject("Cluster Effects"); // create the empty parent object
        convert = new GameObject("Convert Effects"); // create the empty parent object

        skillPointIncreaseAmount = 2;
        grantedLineNumber = 1; // on beginning of the game, player will have a single line to draw
        frameCounter = 0;
        flagTurnBeggins = true; // player 1 turn beggins
        flagSkillApplied = false; // this flag rises after a skill is applied
        flagApplySkill = false;

        StoreSkillPoints();
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (flagApplySkill)
        {
            frameCounter ++;

            if (frameCounter == 2)
            {
                ApplySkill();
                flagApplySkill = false;
                frameCounter = 0;
            }
        }

        if (flagSkillApplied) // if a skill is applied
        {
            ClearBeforeApplySkill(0); // return to regular line drawing 
            flagSkillApplied = false; // set the flag to false
            AnimationManager.activatePanelMovement = true;
        }

        if (flagTurnBeggins) // this flag rises whenever a turn passes to the next player
        {
            var playerTurn = PlayerProperties.playerTurn;
            var currentTurn = PlayerProperties.turnPassed;

            CalculateSkillPoints(playerTurn, currentTurn); // calculate the skill point for the current user.
            CalculateGrantedLineNumber(playerTurn); // calculate how many lines can this player draw before its turn end
            flagTurnBeggins = false; // lower the flag until the turn is ended
            ClearBeforeApplySkill(0); // apply the the default skill
        }

        if (grantedLineNumber == 0) // if the player can not draw anymore lines, its turn is ended
            EndTurn(); // end the current players turn
	}

    void StoreSkillPoints()
    { 
        skillCosts = new int[5];

        skillCosts[0] = AnimationManager.skillBarricadeCost;
        skillCosts[1] = AnimationManager.skillClusterCost;
        skillCosts[2] = AnimationManager.skillConvertCost;
        skillCosts[3] = AnimationManager.skillNaturalCost;
        skillCosts[4] = AnimationManager.skillPierceCost; 
    }

    /// <summary>
    /// This function is used whenever a player applies a skill
    /// </summary>
    /// <param name="skill"> this parameter indicates which skill is going to be applied </param>
    public void ClearBeforeApplySkill(int skill)
    {
        RemoveAllGuideLines(); // clear all the guidelines 

        currentSkill = (PlayerSkills)skill; // store the current skill whwich the player is using 

        flagApplySkill = true;
     }

    /// <summary>
    /// This function is used whenever a player applies a skill
    /// </summary>
    /// <param name="skill"> this parameter indicates which skill is going to be applied </param>
    public void ApplySkill()
    {
        var playerTurn = PlayerProperties.playerTurn;

        switch (currentSkill)
        {
            case (PlayerSkills.Natural): // the default line drawing, its not exactly a skill
                ShowNaturalSkillGuideLines(playerTurn); // show the drawable lines so the player can know where he/she can draw a line
                PlayerProperties.playerSkillPoints[playerTurn] -= skillCosts[3];
                break;
            case (PlayerSkills.Pierce): // this is the skill to break all the neighboor barricades
                ApplyPierceSkill(playerTurn); // directly apply the pierce skill. No guidelines for this one
                PlayerProperties.playerSkillPoints[playerTurn] -= skillCosts[4];
                break;
            case (PlayerSkills.Convert): // convert is applied to convert a neighboor area directly
                ShowConvertSkillGuidelines(playerTurn); // show the convertable areas so the player can know where to be converted
                PlayerProperties.playerSkillPoints[playerTurn] -= skillCosts[2];
                break;
            case (PlayerSkills.Cluster): // cluster is applied to jump to another nearby location
                ShowClusterSkillGuidelines(playerTurn); // show the areas where cluster can be applied so the player can know where to be apply cluster
                PlayerProperties.playerSkillPoints[playerTurn] -= skillCosts[1];
                break;
            case (PlayerSkills.Barricade): // barricade skill is applied by placing a barricade so that the enemys cannot place any lines over it
                ShowNaturalSkillGuideLines(playerTurn); // show the drawable lines so the player can know where he/she can place a barricade
                PlayerProperties.playerSkillPoints[playerTurn] -= skillCosts[0];
                break;
        }
    }

    /// <summary>
    /// This function calculates how many more lines can a player draw before its turn ends
    /// </summary>
    /// <param name="playerTurn"> The current player </param>
    void CalculateGrantedLineNumber(int playerTurn)
    {
        grantedLineNumber = 1; // this is something temporary
    }

    /// <summary>
    /// This function will calculate the skill point for the current user.
    /// Player will be spending these point while purchasing skills.
    /// </summary>
    /// <param name="PlayerTurn"> The current player </param>
    void CalculateSkillPoints(int playerTurn, int currentTurn)
    { 
        var playerAreas = GameObject.FindGameObjectsWithTag("PlayerArea" + playerTurn); // get all the drawable lines
        var skillPoint = PlayerProperties.playerSkillPoints[playerTurn];
        foreach (GameObject area in playerAreas)
        {
            foreach (Transform go in area.transform)
            {
                int turnCreated = int.Parse(go.name);

                if (currentTurn - turnCreated <= 5)
                    skillPoint += (currentTurn - turnCreated) * skillPointIncreaseAmount;
                else
                    skillPoint += 5 * skillPointIncreaseAmount;
            }
        }

        if (skillPoint > 1000)
            skillPoint = 1000;
        PlayerProperties.playerSkillPoints[playerTurn] = skillPoint;
        AnimationManager.flagRefreshSkillPoint = true;
    }

    /// <summary>
    /// This function shows the player where he\she can draw a line.
    /// It is shown for each player on the beginning of the turn.
    /// This function uses the singleton design pattern to prevent creating any duplicate lines. 
    /// </summary>
    void ShowNaturalSkillGuideLines(int playerTurn)
    {
        foreach (Vector3 drawable in PlayerProperties.playerDrawableLines)
        {
            if (drawable.z == playerTurn && GameObject.Find("D_" + drawable.x.ToString() + "_" + drawable.y.ToString()) == null) // do not recreate the line if it exists
            {
                var drawableLineObject = Instantiate(drawableLinePrefab, new Vector3(drawable.x, drawable.y, 0), Quaternion.identity) as Transform; // create the line
                drawableLineObject.name = "D_" + drawable.x.ToString() + "_" + drawable.y.ToString(); // change the lines name, 'D' stands for drawable
                drawableLineObject.tag = "DrawableLines"; // give this object a tag so it will be easy to clear all of them at once
                drawableLineObject.transform.parent = drawables.transform; // place them under the parent object

                var rend = drawableLineObject.GetComponent<Renderer>();

                // set alpha value of a material on runtime 
                Color _color = playerColor[playerTurn]; // store the color
                _color.a = fadedMaterial.color.a; // store the alpha value
                rend.material = fadedMaterial; // use the dummy material
                rend.material.color = _color;   // assign the stored rgba value

                if (Mathf.Abs(Mathf.Floor(drawable.x) - drawable.x) == 0.5) // check if the line is suppose to be verticle
                    drawableLineObject.transform.eulerAngles = new Vector3(0, 0, 90); // rotate the line so it becames verticle
            }
        }
    }

    /// <summary>
    /// This function show the players where they can apply the cluster skill
    /// when its applied the player will have the opportunity to draw the lines on the applied area
    /// </summary>
    void ShowClusterSkillGuidelines(int playerTurn)
    {
        var playerLineObjects = GameObject.FindGameObjectsWithTag("PlayerLines" + playerTurn); // find all the lines with this specific tag

        foreach (GameObject lines in playerLineObjects)
        {
            if (Mathf.Abs(Mathf.Floor(lines.transform.position.x) - lines.transform.position.x) == 0.5) // if line is horizontal
            {
                ClusterSkillEffectLocations(new Vector2(lines.transform.position.x - 2.5f, lines.transform.position.y)); // the left point pos
                ClusterSkillEffectLocations(new Vector2(lines.transform.position.x + 2.5f, lines.transform.position.y)); // the right point pos
            }
            else // line is verticle
            {
                ClusterSkillEffectLocations(new Vector2(lines.transform.position.x, lines.transform.position.y + 2.5f)); // the top point pos
                ClusterSkillEffectLocations(new Vector2(lines.transform.position.x, lines.transform.position.y - 2.5f)); // the bottom point pos
            }
        }
    }

    /// <summary>
    /// This function is helps the main function to display which areas can be choosen for cluster.
    /// The player can choose any area within a specific range around each corner the player posses
    /// THIS FUNCTION IS WRITTEN TERRIABBLY, ITS GOING TO BE RENEWED ASAP 
    /// </summary>
    /// <param name="pos"> the position of the corner point </param>
    void ClusterSkillEffectLocations(Vector2 pos)
    {
        var clusterRange = 3; // set the range that cluster can be applied

        for (int i = clusterRange - 1; i >= 0; i--)
        {
            if (pos.x + 2.5f < 100 && pos.y + (2.5f + 5 * i) < 100) CreateClusterEffect(new Vector2(pos.x + 2.5f, pos.y + (2.5f + 5 * i)));
            if (pos.x - 2.5f > 0 && pos.y + (2.5f + 5 * i) < 100) CreateClusterEffect(new Vector2(pos.x - 2.5f, pos.y + (2.5f + 5 * i)));
            if (pos.x + 2.5f < 100 && pos.y - (2.5f + 5 * i) > 0) CreateClusterEffect(new Vector2(pos.x + 2.5f, pos.y - (2.5f + 5 * i)));
            if (pos.x - 2.5f > 0 && pos.y - (2.5f + 5 * i) > 0) CreateClusterEffect(new Vector2(pos.x - 2.5f, pos.y - (2.5f + 5 * i)));
        }
        
        for (int i = clusterRange - 1; i > 0 ; i--)
        {
            if (pos.x + (2.5f + 5 * i) < 100 && pos.y + 2.5f < 100) CreateClusterEffect(new Vector2(pos.x + (2.5f + 5 * i), pos.y + 2.5f));
            if (pos.x - (2.5f + 5 * i) > 0 && pos.y + 2.5f < 100) CreateClusterEffect(new Vector2(pos.x - (2.5f + 5 * i), pos.y + 2.5f));
            if (pos.x + (2.5f + 5 * i) < 100 && pos.y - 2.5f > 0) CreateClusterEffect(new Vector2(pos.x + (2.5f + 5 * i), pos.y - 2.5f));
            if (pos.x - (2.5f + 5 * i) > 0 && pos.y - 2.5f > 0) CreateClusterEffect(new Vector2(pos.x - (2.5f + 5 * i), pos.y - 2.5f));
        }

        if (pos.x + 7.5f < 100 && pos.y + 7.5f < 100) CreateClusterEffect(new Vector2(pos.x + 7.5f, pos.y + 7.5f));
        if (pos.x - 7.5f > 0 && pos.y + 7.5f < 100) CreateClusterEffect(new Vector2(pos.x - 7.5f, pos.y + 7.5f));
        if (pos.x + 7.5f < 100 && pos.y - 7.5f > 0) CreateClusterEffect(new Vector2(pos.x + 7.5f, pos.y - 7.5f));
        if (pos.x - 7.5f > 0 && pos.y - 7.5f > 0) CreateClusterEffect(new Vector2(pos.x - 7.5f, pos.y - 7.5f));

    }

    /// <summary>
    /// Creates the cluster effects on possible areas
    /// </summary>
    /// <param name="pos"> the location where the effect is going to be created </param>
    void CreateClusterEffect(Vector2 pos)
    {
        if (GameObject.Find("A_" + pos.x.ToString() + "_" + pos.y.ToString()) == null && // create if there is no occupied area or already another cluster effect
            GameObject.Find("CE_" + pos.x.ToString() + "_" + pos.y.ToString()) == null)
        {
            var clusterEffectObject = Instantiate(clusterEffectPrefab, pos, Quaternion.identity) as Transform; // create the cluster effect
            clusterEffectObject.name = "CE_" + clusterEffectObject.transform.position.x.ToString() + "_" + clusterEffectObject.transform.position.y.ToString(); // change the objects name where CE means "cluster effect"
            clusterEffectObject.tag = "ClusterEffect"; // give this object a tag so it will be easy to clear all of them at once
            clusterEffectObject.transform.parent = cluster.transform; // place them under the parent object
        }
    }

    /// <summary>
    /// This function shows the players where they can apply the convert skill
    /// once its applied the player will directly convert the land. If there is
    /// any barricade on the selected area to convert, the skill will fail to convert.
    /// </summary>
    void ShowConvertSkillGuidelines(int playerTurn)
    {
        var playerLineObjects = GameObject.FindGameObjectsWithTag("PlayerLines" + playerTurn); // find all the lines with this specific tag

        foreach (GameObject lines in playerLineObjects)
        {
            if (Mathf.Abs(Mathf.Floor(lines.transform.position.x) - lines.transform.position.x) == 0.5) // if line is horizontal
            {
                ConvertSkillEffectLocations(new Vector2(lines.transform.position.x - 2.5f, lines.transform.position.y)); // the left point pos
                ConvertSkillEffectLocations(new Vector2(lines.transform.position.x + 2.5f, lines.transform.position.y)); // the right point pos
            }
            else // line is verticle
            {
                ConvertSkillEffectLocations(new Vector2(lines.transform.position.x, lines.transform.position.y + 2.5f)); // the top point pos
                ConvertSkillEffectLocations(new Vector2(lines.transform.position.x, lines.transform.position.y - 2.5f)); // the bottom point pos
            }
        }
    }

    /// <summary>
    /// This function defines the neighboor areas around the given corner location
    /// </summary>
    /// <param name="pos"> The location of the corner </param>
    void ConvertSkillEffectLocations(Vector2 pos)
    {
        CreateConvertSkillEffects(new Vector2(pos.x + 2.5f, pos.y + 2.5f)); // top right of the corner
        CreateConvertSkillEffects(new Vector2(pos.x + 2.5f, pos.y - 2.5f)); // bottom right of the corner
        CreateConvertSkillEffects(new Vector2(pos.x - 2.5f, pos.y + 2.5f)); // top left of the corner
        CreateConvertSkillEffects(new Vector2(pos.x - 2.5f, pos.y - 2.5f)); // bottom left of the corner
    }

    /// <summary>
    /// This function creates the convert effect on the specified location
    /// </summary>
    /// <param name="pos"> the location where the effect is wanted to be created </param>
    void CreateConvertSkillEffects(Vector2 pos)
    {
        if (GameObject.Find("A_" + pos.x.ToString() + "_" + pos.y.ToString()) == null && // if the area is not conquered or there is no conv effect already created, enter
            GameObject.Find("CONV_" + pos.x.ToString() + "_" + pos.y.ToString()) == null)
        {
            var convertEffectObject = Instantiate(convertEffectPrefab, pos, convertEffectPrefab.rotation) as Transform; // create the effect
            convertEffectObject.name = "CONV_" + convertEffectObject.transform.position.x.ToString() + "_" + convertEffectObject.transform.position.y.ToString(); // change the objects name, 'Conv' stands for convert
            convertEffectObject.tag = "ConvertEffect"; // give this object a tag so it will be easy to clear all of them at once
            convertEffectObject.transform.parent = convert.transform; // place them under the parent object
        }
    }

    /// <summary>
    /// This skill destroys all the neighboor enemy barricades at once
    /// The enemy barricades are being detected by using the drawable lines
    /// If there is any overlapping between enemy barricades and drawable lines
    /// these barricades are destroyed
    /// </summary>
    void ApplyPierceSkill(int playerTurn)
    {
        ShowNaturalSkillGuideLines(playerTurn); // the drawables are being shown to define if there is any enemy barricades overlapping drawable lines

        var drawables = GameObject.FindGameObjectsWithTag("DrawableLines"); // find all the lines with this specific tag

        foreach (GameObject d in drawables)
        {
            var possibleBarricade = GameObject.Find("B_" + d.transform.position.x.ToString() + "_" + d.transform.position.y.ToString()); // find the barricades overlapping drawable lines

            if (possibleBarricade != null && possibleBarricade.tag != "Barricade" + playerTurn.ToString()) // if the barricade is an enemy barricade, destroy it 
                Destroy(possibleBarricade);
        }

        flagSkillApplied = true; // skill is applied
    }

    /// <summary>
    /// This function removes all the skill guidlines from the map
    /// </summary>
    void RemoveAllGuideLines()
    {
        ClearNaturalSkillGuidelines(); // remove guidelines for barricade and draw line
        ClearClusterSkillGuidelines(); // remove guidelines for cluster skill 
        ClearConvertSkillGuidelines(); // remove guidelines for convert skill
    }


    /// <summary>
    /// This function cleares the drawable lines so that,
    /// the next player will only see his\her own drawable lines
    /// </summary>
    void ClearNaturalSkillGuidelines()
    {
        var drawableTaggedObjects = GameObject.FindGameObjectsWithTag("DrawableLines"); // find all the lines with this specific tag

        foreach (GameObject d in drawableTaggedObjects) // remove all the drawable lines
        {
            Destroy(d);
        }
    }

    /// <summary>
    /// This function cleares the Cluster skill guide lines.
    /// </summary>
    void ClearClusterSkillGuidelines()
    {
        var clusterTaggedObjects = GameObject.FindGameObjectsWithTag("ClusterEffect"); // find all the lines with this specific tag

        foreach (GameObject d in clusterTaggedObjects) // remove all the cluster skill guide lines
        {
            Destroy(d);
        }
    }

    /// <summary>
    /// This function cleares the Convert skill guide lines.
    /// </summary>
    void ClearConvertSkillGuidelines()
    {
        var clusterTaggedObjects = GameObject.FindGameObjectsWithTag("ConvertEffect"); // find all the lines with this specific tag

        foreach (GameObject d in clusterTaggedObjects) // remove all the convert skill guide lines
        {
            Destroy(d);
        }
    }

    /// <summary>
    /// This fucntion is called when the current players turn ends
    /// </summary>
    void EndTurn()
    {
        RemoveAllGuideLines(); // remove current players guidelines

        PlayerProperties.flagEndTurn = true; // ending player turn
    }
}
