using UnityEngine;
using System.Collections;

public class GenerateLevel : MonoBehaviour
{

    public Transform cornerPrefab; // This is the prefab for the points that is planned to be created

    private Transform cornerObject; // With this variable we can reach each created point
    private Renderer rend; // Using this property we can assign different colors to the points

    public static Vector2[] linePoints = new Vector2[880]; // this is the array which will store all created points
    
    // Use this for initialization
    void Start()
    {
        Screen.sleepTimeout = (int)SleepTimeout.NeverSleep; // dont let the screen turn off while playing

        var pointCounter = 0; // numbers of points created

        var corner = new GameObject("Corners"); // Create a parrent object for the points

        for (int y = 0; y <= 100; y += 5)
        {
            for (int x = 0; x <= 100; x += 5)
            {
                cornerObject = Instantiate(cornerPrefab, new Vector3(x, y, 0), Quaternion.identity) as Transform; // create the points(corners)
                cornerObject.name = "C_" + x.ToString() + "_" + y.ToString(); //change corner name, 'C' stands for corner
                cornerObject.tag = "Corners"; // give this object a tag so it will be easy locate it
                cornerObject.transform.parent = corner.transform; //attach the created corner under the parent object
                
                rend = cornerObject.GetComponent<Renderer>();  
                rend.material.color = Color.white; // change the color of the created point

                if (x != 100) // stop adding the points to the array when it is filled up
                {
                    linePoints[pointCounter] = new Vector2(x + 2.5f, y); 
                    pointCounter++;
                }

                if (y != 100) // stop adding the points to the array when it is filled up
                {
                    linePoints[pointCounter] = new Vector2(x, y + 2.5f); 
                    pointCounter++;
                }
            }
        }
    }
}
