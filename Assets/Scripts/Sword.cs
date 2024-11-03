using UnityEngine;

public class Sword : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float verticalSpeed = 3f;

    void Update()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        float moveY = 0;

        if (Input.GetKey(KeyCode.Q))
        {
            moveY = verticalSpeed * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.E))
        {
            moveY = -verticalSpeed * Time.deltaTime;
        }

        Vector3 move = new Vector3(moveX, moveY, moveZ) * moveSpeed * Time.deltaTime;
        transform.Translate(move, Space.World);
    }
}