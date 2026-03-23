using UnityEngine;

public class DestroyItSelf : MonoBehaviour
{
    void Start()
    {
        Destroy(gameObject, 2f);
    }

    void Update()
    {
        
    }
}
