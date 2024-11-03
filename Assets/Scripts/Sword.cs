//using UnityEngine;

//public class Sword : MonoBehaviour
//{
//    public float moveSpeed = 5f;

//    void Update()
//    {
//        float moveX = Input.GetAxis("Horizontal");
//        float moveZ = Input.GetAxis("Vertical");

//        float moveY = 0;
//        if (Input.GetKey(KeyCode.Q))
//        {
//            moveY = 1;
//        }
//        else if (Input.GetKey(KeyCode.E))
//        {
//            moveY = -1;
//        }

//        Vector3 move = new Vector3(moveX, moveY, moveZ) * moveSpeed * Time.deltaTime;

//        transform.Translate(move, Space.World);
//    }
//}



using UnityEngine;

public class Sword : MonoBehaviour
{
    public float moveSpeed = 5f;
    public bool stit = false;
    private GameObject currentObject; // Track the currently active object
    private GameObject sword; // Reference to the sword object
    private GameObject knife; // Reference to the knife object

    void Start()
    {
        // Find the sword and knife objects in the scene
        sword = GameObject.Find("sword");
        knife = GameObject.Find("stitch");

        // Set the current object to the sword initially
        stit = false;
        currentObject = sword;
    }

    void Update()
    {
        // Switch between sword and knife when 'C' is pressed
        if (Input.GetKeyDown(KeyCode.C))
        {
            SwitchControl();
        }

        // Move the current object if it is not null
        if (currentObject != null)
        {
            Move(currentObject);
        }
    }

    void Move(GameObject obj)
    {
        // Get movement input
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        float moveY = 0;
        if (Input.GetKey(KeyCode.Q))
        {
            moveY = 1;
        }
        else if (Input.GetKey(KeyCode.E))
        {
            moveY = -1;
        }

        // Calculate movement vector
        Vector3 move = new Vector3(moveX, moveY, moveZ) * moveSpeed * Time.deltaTime;

        // Move the current object
        obj.transform.Translate(move, Space.World);
    }

    void SwitchControl()
    {
        // Switch active object
        if (currentObject == sword)
        {
            currentObject = knife; // Switch to knife
            stit = true;
        }
        else
        {
            currentObject = sword; // Switch back to sword
            stit = false;
        }
    }
}