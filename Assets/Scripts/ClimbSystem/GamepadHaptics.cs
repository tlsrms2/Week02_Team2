using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 게임패드 진동을 관리하는 전용 클래스.
/// PlayerController 등 외부에서 Play___() 메서드를 호출하면 됩니다.
/// </summary>
public class GamepadHaptics : MonoBehaviour
{
    [System.Serializable]
    public class HapticProfile
    {
        [Range(0f, 1f)] public float lowFrequency = 0.3f;   // 왼쪽 모터 (무거운 진동)
        [Range(0f, 1f)] public float highFrequency = 0.3f;   // 오른쪽 모터 (날카로운 진동)
        public float duration = 0.15f;
    }

    [Header("그랩 성공")]
    public HapticProfile grab = new()
    {
        lowFrequency = 0.2f,
        highFrequency = 0.4f,
        duration = 0.1f
    };

    [Header("그랩 실패 (복귀)")]
    public HapticProfile grabFail = new()
    {
        lowFrequency = 0.1f,
        highFrequency = 0.15f,
        duration = 0.08f
    };

    [Header("슬라이드")]
    public HapticProfile slideStart = new()
    {
        lowFrequency = 0.5f,
        highFrequency = 0.3f,
        duration = 0.25f
    };

    [Range(0f, 1f)] public float slideSustainLow = 0.15f;
    [Range(0f, 1f)] public float slideSustainHigh = 0.1f;

    [Header("스턴")]
    public HapticProfile stun = new()
    {
        lowFrequency = 0.8f,
        highFrequency = 0.6f,
        duration = 0.4f
    };

    private Coroutine activeRoutine;
    private bool isSlideSustain;

    // ── 외부 호출 API ─────────────────────────────

    public void PlayGrab()
    {
        Play(grab);
    }

    public void PlayGrabFail()
    {
        Play(grabFail);
    }

    public void PlaySlideStart()
    {
        // 초기 강한 진동 → 지속 진동으로 전환
        StopCurrent();
        activeRoutine = StartCoroutine(SlideRoutine());
    }

    public void StopSlide()
    {
        isSlideSustain = false;
        StopCurrent();
        StopMotors();
    }

    public void PlayStun(float stunDuration)
    {
        StopCurrent();
        activeRoutine = StartCoroutine(StunRoutine(stunDuration));
    }

    // ── 내부 ──────────────────────────────────────

    void Play(HapticProfile profile)
    {
        StopCurrent();
        activeRoutine = StartCoroutine(PulseRoutine(profile));
    }

    IEnumerator PulseRoutine(HapticProfile profile)
    {
        SetMotors(profile.lowFrequency, profile.highFrequency);
        yield return new WaitForSeconds(profile.duration);
        StopMotors();
        activeRoutine = null;
    }

    IEnumerator SlideRoutine()
    {
        // 1) 초기 충격
        SetMotors(slideStart.lowFrequency, slideStart.highFrequency);
        yield return new WaitForSeconds(slideStart.duration);

        // 2) 지속 진동 (StopSlide 호출까지)
        isSlideSustain = true;
        SetMotors(slideSustainLow, slideSustainHigh);

        while (isSlideSustain)
            yield return null;

        StopMotors();
        activeRoutine = null;
    }

    IEnumerator StunRoutine(float stunDuration)
    {
        float elapsed = 0f;

        // 강한 시작 → 시간에 따라 감쇠
        while (elapsed < stunDuration)
        {
            float t = elapsed / stunDuration;          // 0 → 1
            float fade = 1f - t;                        // 1 → 0

            SetMotors(
                stun.lowFrequency * fade,
                stun.highFrequency * fade
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        StopMotors();
        activeRoutine = null;
    }

    // ── 모터 제어 ─────────────────────────────────

    void SetMotors(float low, float high)
    {
        var gp = Gamepad.current;
        if (gp == null) return;
        gp.SetMotorSpeeds(low, high);
    }

    void StopMotors()
    {
        var gp = Gamepad.current;
        if (gp == null) return;
        gp.SetMotorSpeeds(0f, 0f);
    }

    void StopCurrent()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }
        isSlideSustain = false;
    }

    void OnDisable()
    {
        StopCurrent();
        StopMotors();
    }
}