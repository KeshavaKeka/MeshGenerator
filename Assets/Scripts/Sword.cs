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