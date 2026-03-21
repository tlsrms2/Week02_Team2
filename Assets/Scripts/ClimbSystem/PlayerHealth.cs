using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerHealth : MonoBehaviour
{
    public PlayerController playerController;


    void Start()
    {
    }

    public void TakeStun(float stunTime)
    {
        if (playerController != null)
            playerController.Stun(stunTime);
    }

    public void TakeSlide(float slideTime)
    {
        if(playerController != null)
            playerController.Slide(slideTime);
    }

}