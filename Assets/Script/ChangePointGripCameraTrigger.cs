using UnityEngine;
using UnityEngine.Animations.Rigging;

[DisallowMultipleComponent]
public class ChangePointGripCameraTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ClimbingRigController playerController;
    [SerializeField] private GripCameraDirector cameraDirector;
    [SerializeField] private Collider changePointCollider;

    [Header("Grip Detection")]
    [SerializeField] private float gripCheckRadius = 0.2f;
    [SerializeField] private float gripConfirmationWindow = 0.2f;

    private float pendingGripDeadline = -1f;
    private bool hasActivated;

    private void Reset()
    {
        changePointCollider = GetComponent<Collider>();
    }

    private void Awake()
    {
        if (changePointCollider == null)
        {
            changePointCollider = GetComponent<Collider>();
        }

        if (playerController == null)
        {
            playerController = FindObjectOfType<ClimbingRigController>();
        }

        if (cameraDirector == null && Camera.main != null)
        {
            cameraDirector = Camera.main.GetComponent<GripCameraDirector>();
        }
    }

    private void Update()
    {
        if (hasActivated || playerController == null || cameraDirector == null || changePointCollider == null)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            pendingGripDeadline = Time.time + gripConfirmationWindow;
        }

        bool isGripMaintained = IsAnyLimbOnChangePoint();
        if (pendingGripDeadline >= Time.time && isGripMaintained)
        {
            hasActivated = true;
            cameraDirector.ActivateGripView();
            pendingGripDeadline = -1f;
        }
    }

    private bool IsAnyLimbOnChangePoint()
    {
        return IsTargetNearChangePoint(playerController.leftArmIK)
            || IsTargetNearChangePoint(playerController.rightArmIK)
            || IsTargetNearChangePoint(playerController.leftLegIK)
            || IsTargetNearChangePoint(playerController.rightLegIK);
    }

    private bool IsTargetNearChangePoint(TwoBoneIKConstraint limb)
    {
        if (limb == null || limb.data.target == null)
        {
            return false;
        }

        Vector3 targetPosition = limb.data.target.position;
        Vector3 closestPoint = changePointCollider.ClosestPoint(targetPosition);
        float sqrDistance = (targetPosition - closestPoint).sqrMagnitude;
        return sqrDistance <= gripCheckRadius * gripCheckRadius;
    }
}
