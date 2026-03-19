using UnityEngine;

public class RopeSpawn : MonoBehaviour
{
    [SerializeField] GameObject partPrefab, parentPrefab;

    [SerializeField] int length = 1;
    [SerializeField] float partDistance = 0.21f;
    [SerializeField] bool reset, spawn, snapFirst, snapLast;

    private void Update()
    {
        if (reset)
        {
            foreach (GameObject tmp in GameObject.FindGameObjectsWithTag("Player"))
            {
                Destroy(tmp);
            }
        }
        if (spawn)
        {
            Spawn();
            spawn = false;
        }
    }

    public void Spawn()
    {
        int count = (int)(length / partDistance);
        for (int x = 0; x < count; x++)
        {
            GameObject tmp;
            tmp = Instantiate(partPrefab, new Vector3(transform.position.x, transform.position.y, transform.position.z), Quaternion.identity, parentPrefab.transform);
            tmp.transform.eulerAngles = new Vector3(100, 0, 0);

            tmp.name = parentPrefab.transform.childCount.ToString();

            if(x == 0)
            {
                Destroy(tmp.GetComponent<CharacterJoint>());
            }
            else
            {
                tmp.GetComponent<CharacterJoint>().connectedBody = parentPrefab.transform.Find((parentPrefab.transform.childCount - 1).ToString()).GetComponent<Rigidbody>();
            }
        }
    }
}
