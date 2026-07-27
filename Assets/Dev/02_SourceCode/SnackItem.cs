using UnityEngine;

/// <summary>
/// 손으로 집는 간식. Grabbable/Rigidbody는 그대로 두고, 이 스크립트는
/// "손으로 집혔는지"만 판정해서 DogEat에 알려준다.
///
/// 판정 방법: Rigidbody.isKinematic이 false(자유 상태) -> true(잡힌 상태)로 바뀌는
/// 순간을 감지해서 "집힘"으로 본다 (FetchBall의 착지 판정과 반대 방향).
/// 집힌 뒤로는 강아지가 손 위치(간식이 붙어 있는 곳)를 계속 쫓아가다가 도착하면 먹는다.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SnackItem : MonoBehaviour
{
    private Rigidbody rb;
    private bool wasKinematicLastFrame;
    private bool held;

    /// <summary>손으로 집힌 적이 있는가 (놓아도 계속 true, 강아지가 도착해서 먹을 때까지 유지)</summary>
    public bool IsHeld => held;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        wasKinematicLastFrame = rb.isKinematic;
    }

    private void Update()
    {
        bool isKinematicNow = rb.isKinematic;

        // 자유 상태(dynamic)였다가 방금 손에 잡힌(kinematic) 순간 감지
        if (!wasKinematicLastFrame && isKinematicNow)
            held = true;

        wasKinematicLastFrame = isKinematicNow;
    }

    /// <summary>강아지가 이 간식을 쫓기 시작할 때 호출 (같은 집힘을 중복으로 다시 잡지 않도록)</summary>
    public void ConsumeHeld()
    {
        held = false;
    }

    /// <summary>강아지가 도착해서 간식을 먹었을 때 호출. 간식 오브젝트를 없앤다.</summary>
    public void Eat()
    {
        Destroy(gameObject);
    }
}
