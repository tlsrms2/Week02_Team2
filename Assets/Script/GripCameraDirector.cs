using Unity.Cinemachine;
using UnityEngine;

[DisallowMultipleComponent]
public class GripCameraDirector : MonoBehaviour
{
    [Header("Cinemachine")]
    [SerializeField] private CinemachineCamera gripCamera;
    [SerializeField] private int activePriority = 100;

    [Header("Optional Disable While Active")]
    [SerializeField] private Behaviour[] behavioursToDisable = new Behaviour[0];
    [SerializeField] private GameObject[] objectsToDisable = new GameObject[0];

    private int defaultGripPriority;
    private bool priorityCaptured;
    private bool isGripViewActive;
    private bool[] cachedBehaviourStates;
    private bool[] cachedObjectStates;

    public bool IsGripViewActive
    {
        get { return isGripViewActive; }
    }

    private void Awake()
    {
        if (gripCamera == null)
        {
            gripCamera = GetComponent<CinemachineCamera>();
        }

        CapturePriority();
    }

    public void ActivateGripView()
    {
        if (gripCamera == null || isGripViewActive)
        {
            return;
        }

        CapturePriority();
        isGripViewActive = true;
        gripCamera.gameObject.SetActive(true);
        gripCamera.Priority.Value = activePriority;
        gripCamera.Prioritize();
        SetControlledTargetsEnabled(false);
    }

    public void ResetToDefaultView()
    {
        if (gripCamera == null || !isGripViewActive)
        {
            return;
        }

        isGripViewActive = false;
        gripCamera.Priority.Value = defaultGripPriority;
        SetControlledTargetsEnabled(true);
    }

    private void CapturePriority()
    {
        if (priorityCaptured || gripCamera == null)
        {
            return;
        }

        defaultGripPriority = gripCamera.Priority.Value;
        priorityCaptured = true;
    }

    private void SetControlledTargetsEnabled(bool enabled)
    {
        if (!enabled)
        {
            cachedBehaviourStates = new bool[behavioursToDisable.Length];
            for (int i = 0; i < behavioursToDisable.Length; i++)
            {
                Behaviour target = behavioursToDisable[i];
                if (target == null)
                {
                    continue;
                }

                cachedBehaviourStates[i] = target.enabled;
                target.enabled = false;
            }

            cachedObjectStates = new bool[objectsToDisable.Length];
            for (int i = 0; i < objectsToDisable.Length; i++)
            {
                GameObject target = objectsToDisable[i];
                if (target == null)
                {
                    continue;
                }

                cachedObjectStates[i] = target.activeSelf;
                target.SetActive(false);
            }

            return;
        }

        for (int i = 0; i < behavioursToDisable.Length; i++)
        {
            Behaviour target = behavioursToDisable[i];
            if (target == null || cachedBehaviourStates == null || i >= cachedBehaviourStates.Length)
            {
                continue;
            }

            target.enabled = cachedBehaviourStates[i];
        }

        for (int i = 0; i < objectsToDisable.Length; i++)
        {
            GameObject target = objectsToDisable[i];
            if (target == null || cachedObjectStates == null || i >= cachedObjectStates.Length)
            {
                continue;
            }

            target.SetActive(cachedObjectStates[i]);
        }
    }
}
