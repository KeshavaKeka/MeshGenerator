/*
using UnityEngine;

public class Sword : MonoBehaviour
{
    public float moveSpeed = 5f;

    void Update()
    {
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

        Vector3 move = new Vector3(moveX, moveY, moveZ) * moveSpeed * Time.deltaTime;

        transform.Translate(move, Space.World);
    }
}
*/
using UnityEngine;

public class Sword : MonoBehaviour
{
    public float moveSpeed = 5f;
    public KeyCode stitchKey = KeyCode.R; // Press 'R' to stitch
    public GameObject targetObject; // Assign this in the Inspector or via code

    private MeshStitcher meshStitcher;

    void Start()
    {
        if (targetObject != null)
        {
            meshStitcher = targetObject.GetComponent<MeshStitcher>();
            if (meshStitcher == null)
            {
                Debug.LogError("MeshStitcher component not found on the target object!");
            }
        }
        else
        {
            Debug.LogError("Target object not assigned!");
        }
    }

    void Update()
    {
        // Movement code
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
        Vector3 move = new Vector3(moveX, moveY, moveZ) * moveSpeed * Time.deltaTime;
        transform.Translate(move, Space.World);

        // Stitching trigger
        if (Input.GetKeyDown(stitchKey) && meshStitcher != null)
        {
            meshStitcher.StitchMesh();
            Debug.Log("Mesh stitching attempted");
        }
    }

    // Call this method after generating your mesh
    public void SetTargetObject(GameObject target)
    {
        targetObject = target;
        meshStitcher = targetObject.GetComponent<MeshStitcher>();
        if (meshStitcher == null)
        {
            Debug.LogError("MeshStitcher component not found on the target object!");
        }
    }
}