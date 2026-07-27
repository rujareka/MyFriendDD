using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class DogAnimationHotkeys : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [SerializeField]
    private string[] stateNames = new string[]
    {
        "1 type_Idle Breathing",   // 1
        "1 type_Idle_Playing",     // 2
        "1 type_Walk_Turn_Left",   // 3
        "1 type_Walk_Turn_Right",  // 4
        "1 type_Run Lean Left",    // 5
        "1 type_Run Lean Right",   // 6
        "1 type_Run Loop",         // 7
        "1 type_Walk_Turn_Right 0",// 8
        "RookOwner",               // 9
    };

    [SerializeField]
    private bool[] freezeOnComplete = new bool[]
    {
        false, false, false, false, false, false, false, false, true,
    };

    private Coroutine freezeRoutine;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        for (int i = 0; i < stateNames.Length; i++)
        {
            bool topRowPressed = keyboard[Key.Digit1 + i].wasPressedThisFrame;
            bool numpadPressed = i < 9 && keyboard[Key.Numpad1 + i].wasPressedThisFrame;

            if (topRowPressed || numpadPressed)
            {
                PlayState(i);
                break;
            }
        }
    }

    private void PlayState(int index)
    {
        if (freezeRoutine != null)
        {
            StopCoroutine(freezeRoutine);
            freezeRoutine = null;
        }

        animator.speed = 1f;
        animator.Play(stateNames[index], 0, 0f);

        if (freezeOnComplete[index])
            freezeRoutine = StartCoroutine(FreezeAfterOnePlay());
    }

    private IEnumerator FreezeAfterOnePlay()
    {
        yield return null;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        while (state.normalizedTime < 1f)
        {
            yield return null;
            state = animator.GetCurrentAnimatorStateInfo(0);
        }

        animator.speed = 0f;
        freezeRoutine = null;
    }
}
