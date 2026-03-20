using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonEvent : MonoBehaviour
{
    public void ReStart_Button()
    {
        SceneManager.LoadScene("Han_Scene");
    }
}
