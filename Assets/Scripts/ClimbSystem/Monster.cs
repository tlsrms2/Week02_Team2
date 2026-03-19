using UnityEngine;
public class Moster : MonoBehaviour
{
    public GameObject m_up;
    public GameObject m_down;

    public float m_speed;
    public float speed = 90f;   // 초당 회전 속도 (도/초)
    private float _currentY = 45f;
    private float _currentX = 0f;
    private float _direction = 1f;  // 1 = 증가, -1 = 감소
    private void Update()
    {
        var v = Vector3.up;
        transform.position += v * m_speed * Time.deltaTime;

        _currentX += speed * _direction * Time.deltaTime;
        _currentY += speed * _direction * Time.deltaTime;

        if (_currentX >= 135f)
        {
            _currentX = 135f;
            _direction = -1f;   // 감소로 전환
        }
        else if (_currentX <= 45f)
        {
            _currentX = 45f;
            _direction = 1f;    // 증가로 전환
        }
        if (_currentY >= 45)
        {
            _currentY = 45;
        }
        else if (_currentY <= -45)
        {
            _currentY = -45;
  // 증가로 전환
        }
        m_up.transform.rotation = Quaternion.Euler( 0, 0f,_currentX);
        m_down.transform.rotation = Quaternion.Euler(0, 0f, -_currentY);
    }



}