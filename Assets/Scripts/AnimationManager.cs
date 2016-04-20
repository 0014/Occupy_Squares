using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AnimationManager : MonoBehaviour {

    public static bool activatePanelMovement;
    public static bool flagRefreshSkillPoint;
    public static bool panelOpen;
    public float panelAnimationSpeed;
    public static int skillNaturalCost;
    public static int skillPierceCost;
    public static int skillConvertCost;
    public static int skillClusterCost;
    public static int skillBarricadeCost;
    public Button[] skillButtons; // {Barricade, Cluster, Convert, Pierce}
    public GameObject panel;
    public GameObject skillBarFilling;
    public Text txtSkillPoint;

    private float travelTime;
    private Vector3 panelStartingPos;
    private Vector3 panelEndingPos;

    void Awake()
    {
        // Set the skill costs:
        //skillNaturalCost = ;
        skillPierceCost = 300;
        skillConvertCost = 800;
        skillClusterCost = 400;
        skillBarricadeCost = 100;
    }

    // Use this for initialization
	void Start () 
    {
        activatePanelMovement = false;
        panelOpen = true;

        panelAnimationSpeed = 0.1F;
        panelStartingPos = panel.transform.position;
        panelEndingPos = new Vector3(panelStartingPos.x - Screen.height * 0.33f, panelStartingPos.y, panelStartingPos.z);
	}
	
	// Update is called once per frame
	void Update () 
    {
        if (activatePanelMovement)
        {
            var playerTurn = PlayerProperties.playerTurn; // get the player turn calling the function
            PanelAnimation(playerTurn);
        }
        if (flagRefreshSkillPoint)
        {
            RefreshSkillPoint(PlayerProperties.playerSkillPoints[PlayerProperties.playerTurn]);
            flagRefreshSkillPoint = false;
        }
	}

    void PanelAnimation(int playerTurn)
    {
        Transform panelButton = panel.transform.GetChild(0);

        travelTime += panelAnimationSpeed;

        // Panel is closed, and it is about to be oppened
        if (panelOpen)
        {
            var skillPoint = PlayerProperties.playerSkillPoints[playerTurn];

            SetButtonAvailability(skillPoint); // set the buttons if they are active or not
            
            panel.transform.position = Vector3.Lerp(panelStartingPos, panelEndingPos, travelTime);
            panelButton.transform.localScale = Vector3.Lerp(new Vector3(1, 1, 1), new Vector3(-1, 1, 1), travelTime);

            if (panel.transform.position.x <= panelEndingPos.x + 0.1f)
                StopPanelAnimation();    
        }

        // Panel is open, and it is about to be closed
        else
        {
            panel.transform.position = Vector3.Lerp(panelEndingPos, panelStartingPos, travelTime);
            panelButton.transform.localScale = Vector3.Lerp(new Vector3(-1, 1, 1), new Vector3(1, 1, 1), travelTime);

            if (panel.transform.position.x >= panelStartingPos.x - 0.1f)
            {
                StopPanelAnimation();
                gameObject.GetComponent<SkillManager>().ClearBeforeApplySkill(0);
            }
            
        }
    }

    void SetButtonAvailability(int skillPoint)
    {
        if (skillPoint >= skillConvertCost)
        {
            // set all the skills to interactable
            skillButtons[0].interactable = true; // barricade
            skillButtons[1].interactable = true; // cluster
            skillButtons[2].interactable = true; // convert
            skillButtons[3].interactable = true; // pierce
        }
        else if (skillPoint >= skillClusterCost && skillPoint < skillConvertCost)
        {
            skillButtons[0].interactable = true; // barricade
            skillButtons[1].interactable = true; // cluster
            skillButtons[2].interactable = false; // convert
            skillButtons[3].interactable = true; // pierce
        }
        else if (skillPoint >= skillPierceCost && skillPoint < skillClusterCost)
        {
            skillButtons[0].interactable = true; // barricade
            skillButtons[1].interactable = false; // cluster
            skillButtons[2].interactable = false; // convert
            skillButtons[3].interactable = true; // pierce
        }
        else if (skillPoint >= skillBarricadeCost && skillPoint < skillPierceCost)
        {
            skillButtons[0].interactable = true; // barricade
            skillButtons[1].interactable = false; // cluster
            skillButtons[2].interactable = false; // convert
            skillButtons[3].interactable = false; // pierce
        }
        else
        {
            skillButtons[0].interactable = false; // barricade
            skillButtons[1].interactable = false; // cluster
            skillButtons[2].interactable = false; // convert
            skillButtons[3].interactable = false; // pierce
        }
    }

    public void StartPanelAnimation()
    {
        activatePanelMovement = true;
    }

    void StopPanelAnimation()
    {
        activatePanelMovement = false;
        panelOpen ^= true;
        travelTime = 0;
    }

    void RefreshSkillPoint(int skillPoint)
    {
        txtSkillPoint.text = skillPoint.ToString();
        skillBarFilling.transform.localScale = new Vector3((4.4f * skillPoint) / 1000, 1, 1);
    }
}
