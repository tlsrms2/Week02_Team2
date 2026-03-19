using UnityEngine;

public class TestMoveController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private void Update()
    {
        float x = Input.GetAxisRaw("Horizontal"); // A,D
        float z = Input.GetAxisRaw("Vertical");   // W,S

        Vector3 move = new Vector3(x, 0f, z).normalized;
        transform.position += move * moveSpeed * Time.deltaTime;
    }
}
