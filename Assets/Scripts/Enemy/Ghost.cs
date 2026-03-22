using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

public class Ghost : MonoBehaviour
{
    public float speed = 2.0f;       // 유령 이동 속도
    public bool isStart = false;
    [Header("Pathfinding")]
    public MazeNode startNode;       // 유령이 처음 시작할 위치(노드)

    [Header("Sprites")]
    public Sprite upSprite;          // 위쪽을 볼 때의 스프라이트
    public Sprite downSprite;        // 아래쪽을 볼 때의 스프라이트
    public Sprite leftSprite;        // 왼쪽을 볼 때의 스프라이트
    public Sprite rightSprite;       // 오른쪽을 볼 때의 스프라이트

    private SpriteRenderer spriteRenderer;
    private Vector3 moveDirection = Vector3.up; // 현재 이동 방향 (로컬 기준)
    
    private MazeNode currentNode;
    private MazeNode targetNode;
    private MazeNode previousNode;

    [Header("Stun")]
    [SerializeField] LayerMask targerLayer;
    [SerializeField] float stunTime;

    void Start()
    {
        // 유령 오브젝트가 가지고 있는 SpriteRenderer 컴포넌트를 가져옵니다.
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        Init();
    }

    void Update()
    {
        if (!isStart) return;
        if (targetNode != null)
        {
            // 목표 노드를 향해 부드럽게 이동합니다.
            transform.position = Vector3.MoveTowards(transform.position, targetNode.transform.position, speed * Time.deltaTime);

            // 목표 노드에 거의 도착했는지 확인합니다.
            if (Vector3.Distance(transform.position, targetNode.transform.position) < 0.01f)
            {
                transform.position = targetNode.transform.position; // 위치를 정확하게 보정
                currentNode = targetNode;
                ChooseNextNode();
            }
        }
    }

    public void Init()
    {
        isStart = true;
        gameObject.SetActive(true);
        if (startNode != null)
        {
            // 시작 노드로 유령 위치를 맞추고 다음 이동할 목표를 계산합니다.
            transform.position = startNode.transform.position;
            currentNode = startNode;
            ChooseNextNode();
        }
    }

    // 갈림길(노드)에 도착했을 때 다음에 갈 방향을 선택하는 함수
    void ChooseNextNode()
    {
        if (currentNode == null) return;

        List<MazeNode> availableNodes = new List<MazeNode>();
        List<Vector3> availableDirections = new List<Vector3>();

        // 방금 왔던 길(previousNode)을 제외하고 갈 수 있는 모든 방향을 리스트에 추가합니다.
        if (currentNode.upNode != null && currentNode.upNode != previousNode)
        {
            availableNodes.Add(currentNode.upNode);
            availableDirections.Add(Vector3.up);
        }
        if (currentNode.downNode != null && currentNode.downNode != previousNode)
        {
            availableNodes.Add(currentNode.downNode);
            availableDirections.Add(Vector3.down);
        }
        if (currentNode.leftNode != null && currentNode.leftNode != previousNode)
        {
            availableNodes.Add(currentNode.leftNode);
            availableDirections.Add(Vector3.left);
        }
        if (currentNode.rightNode != null && currentNode.rightNode != previousNode)
        {
            availableNodes.Add(currentNode.rightNode);
            availableDirections.Add(Vector3.right);
        }

        // 갈 수 있는 길이 있다면 그 중 하나를 랜덤하게 선택합니다.
        if (availableNodes.Count > 0)
        {
            int index = Random.Range(0, availableNodes.Count);
            targetNode = availableNodes[index];
            moveDirection = availableDirections[index];
        }
        else
        {
            // 다른 길이 없고 막다른 길이면, 왔던 길로 180도 돌아갑니다.
            targetNode = previousNode;
            moveDirection = -moveDirection;
        }

        previousNode = currentNode;
        UpdateSprite(); // 방향이 바뀌었으므로 스프라이트 갱신
    }

    private void OnTriggerEnter(Collider other)
    {
        
        if((targerLayer.value &(1 << other.gameObject.layer)) != 0)
        {
            other.gameObject.GetComponent<PlayerHealth>().TakeStun(stunTime);
        }
    }

    // 현재 이동 방향을 확인하여 스프라이트 이미지를 교체하는 함수
    void UpdateSprite()
    {
        if (spriteRenderer == null) return;

        if (moveDirection == Vector3.up)
            spriteRenderer.sprite = upSprite;
        else if (moveDirection == Vector3.down)
            spriteRenderer.sprite = downSprite;
        else if (moveDirection == Vector3.right)
            spriteRenderer.sprite = rightSprite;
        else if (moveDirection == Vector3.left)
            spriteRenderer.sprite = leftSprite;
    }
}
