using System.Collections;
using Oculus.Interaction.Surfaces;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// XR 전용 상단 메뉴.
///
/// 햄버거 버튼 / Dropdown(State,Tools,Settings) / Tools 패널은 더 이상 코드가 만들지 않는다.
/// 하이어라키에 실제 GameObject로 존재하며, 이 컴포넌트 우클릭(⋮) 메뉴의
/// "Build UI In Scene"으로 최초 1회 생성한다 (Assets/Dev/02_SourceCode/Editor/XRHamburgerMenuBuilder.cs).
/// 그 다음부터 위치/크기는 Scene 뷰에서 RectTransform을 직접 드래그해서 조정하면 된다.
///
/// 이 스크립트는 열기/닫기 애니메이션, 클릭 시 동작, 헤드 추적만 담당한다.
/// </summary>
[DisallowMultipleComponent]
public class XRHamburgerMenu : MonoBehaviour
{
    [Header("추적 (비우면 Camera.main 사용)")]
    [SerializeField] private Transform headCamera;
    [SerializeField] private Vector3 viewOffset = new Vector3(0.13f, 0.09f, 0.5f); // 카메라 기준 우, 상, 앞 (m)
    [SerializeField] private float followLerp = 10f;

    [Header("열기/닫기 애니메이션")]
    [SerializeField] private float slideDuration = 0.2f;

    [Header("Poke/Ray 인터랙션 영역 자동 보정용 캔버스 크기")]
    [SerializeField] private Vector2 canvasSize = new Vector2(1600, 900);

    [Header("씬 하이어라키의 UI 참조 (Build UI In Scene 실행 시 자동 연결됨)")]
    [SerializeField] private Button hamburgerButton;
    [SerializeField] private RectTransform dropdown;
    [SerializeField] private CanvasGroup dropdownGroup;
    [SerializeField] private RectTransform toolsPanel;
    [SerializeField] private CanvasGroup toolsGroup;

    [Header("Ball 스폰")]
    [SerializeField] private GameObject tennisBallPrefab;
    [SerializeField] private Vector3 ballSpawnOffset = new Vector3(0f, 0f, 0.6f); // 카메라 기준 우, 상, 앞 (m)
    [SerializeField] private DogFetch dogFetch; // 스폰된 공을 이 강아지가 쫓아가도록 연결

    [Header("Snack 스폰")]
    [SerializeField] private GameObject snackPrefab;
    [SerializeField] private Vector3 snackSpawnOffset = new Vector3(0f, 0f, 0.6f); // 카메라 기준 우, 상, 앞 (m)
    [SerializeField] private DogEat dogEat; // 스폰된 간식을 이 강아지가 먹으러 가도록 연결

    private bool dropdownOpen;
    private bool toolsOpen;
    private Coroutine dropdownAnim;
    private Coroutine toolsAnim;
    private BoundsClipper pokeBoundsClipper;

    private void Awake()
    {
        if (headCamera == null && Camera.main != null)
            headCamera = Camera.main.transform;

        Debug.Log(headCamera != null
            ? $"[XRHamburgerMenu] headCamera = {headCamera.name} (초기 위치 {headCamera.position})"
            : "[XRHamburgerMenu] headCamera가 없습니다! Head Camera 필드를 CenterEyeAnchor로 연결하거나, 씬에 MainCamera 태그가 붙은 카메라가 있어야 합니다.");

        var canvas = GetComponentInChildren<Canvas>();
        if (canvas != null)
            canvas.GetComponent<RectTransform>().sizeDelta = canvasSize;

        // EmptyUIBackplateWithCanvas 프리팹은 500x500 캔버스를 기준으로 포크(Poke)/레이(Ray)
        // 인터랙션 영역(BoundsClipper)이 고정돼 있다. 캔버스를 그보다 키우면 그 영역 밖의
        // 버튼은 손 포크로 닿아도 반응하지 않으므로, 캔버스 크기에 맞춰 같이 늘려준다.
        pokeBoundsClipper = GetComponentInChildren<BoundsClipper>();
        if (pokeBoundsClipper != null)
        {
            Vector3 size = pokeBoundsClipper.Size;
            pokeBoundsClipper.Size = new Vector3(canvasSize.x, canvasSize.y, size.z);
        }

        if (dropdown == null || toolsPanel == null)
        {
            Debug.LogError("[XRHamburgerMenu] Dropdown/ToolsPanel이 연결되지 않았습니다. " +
                "컴포넌트 우클릭(⋮) 메뉴에서 'Build UI In Scene'을 먼저 실행하세요.");
            enabled = false;
            return;
        }

        SetOpen(dropdown, dropdownGroup, false, instant: true);
        SetOpen(toolsPanel, toolsGroup, false, instant: true);
    }

    private void LateUpdate()
    {
        if (headCamera == null) return;

        Vector3 targetPos = headCamera.position
            + headCamera.right * viewOffset.x
            + headCamera.up * viewOffset.y
            + headCamera.forward * viewOffset.z;
        Quaternion targetRot = Quaternion.LookRotation(headCamera.forward, Vector3.up);

        float t = 1f - Mathf.Exp(-followLerp * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPos, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t);
    }

    // ---------- 하이어라키의 각 버튼 OnClick()에 연결되는 핸들러 ----------
    // (Build UI In Scene이 자동으로 연결해준다. 새 버튼을 직접 추가할 때도 이 public 메서드들을
    // Inspector의 Button > On Click() 리스트에 그대로 드래그해서 연결하면 된다)

    public void ToggleDropdown()
    {
        dropdownOpen = !dropdownOpen;
        SetOpen(dropdown, dropdownGroup, dropdownOpen);
        if (!dropdownOpen && toolsOpen)
        {
            toolsOpen = false;
            SetOpen(toolsPanel, toolsGroup, false);
        }
    }

    public void OnStateButtonClicked()
    {
        Debug.Log("[Menu] State 선택");
        CloseDropdown();
    }

    public void OnToolsButtonClicked()
    {
        CloseDropdown();
        ToggleTools();
    }

    public void OnSettingsButtonClicked()
    {
        Debug.Log("[Menu] Settings 선택");
        CloseDropdown();
    }

    public void OnToolIconClicked(string toolName)
    {
        Debug.Log($"[Tools] {toolName} 선택");

        if (toolName == "Ball")
        {
            CloseTools();
            SpawnTennisBall();
        }
        else if (toolName == "Snack")
        {
            CloseTools();
            SpawnSnack();
        }
    }

    // ---------- 토글 / 애니메이션 ----------

    private void CloseDropdown()
    {
        if (!dropdownOpen) return;
        dropdownOpen = false;
        SetOpen(dropdown, dropdownGroup, false);
    }

    private void ToggleTools()
    {
        toolsOpen = !toolsOpen;
        SetOpen(toolsPanel, toolsGroup, toolsOpen);
    }

    private void CloseTools()
    {
        if (!toolsOpen) return;
        toolsOpen = false;
        SetOpen(toolsPanel, toolsGroup, false);
    }

    private void SpawnTennisBall()
    {
        if (tennisBallPrefab == null)
        {
            Debug.LogError("[XRHamburgerMenu] Tennis Ball Prefab이 연결되지 않았습니다. " +
                "인스펙터에서 InteractTennis 프리팹을 드래그해 연결하세요.");
            return;
        }

        Transform cam = headCamera != null ? headCamera : (Camera.main != null ? Camera.main.transform : null);
        if (cam == null) return;

        Vector3 spawnPos = cam.position
            + cam.right * ballSpawnOffset.x
            + cam.up * ballSpawnOffset.y
            + cam.forward * ballSpawnOffset.z;
        Quaternion spawnRot = Quaternion.LookRotation(cam.forward, Vector3.up);
        GameObject clone = Instantiate(tennisBallPrefab, spawnPos, spawnRot);

        FetchBall clonedBall = clone.GetComponent<FetchBall>();
        if (clonedBall != null)
        {
            // 던져져서 착지한 게 아니라 눈앞에 바로 등장한 공이므로, 곧바로 쫓아갈 수 있는 상태로 표시한다.
            clonedBall.MarkAsLanded();
            if (dogFetch != null)
                dogFetch.SetBall(clonedBall);
        }
    }

    private void SpawnSnack()
    {
        if (snackPrefab == null)
        {
            Debug.LogError("[XRHamburgerMenu] Snack Prefab이 연결되지 않았습니다. " +
                "인스펙터에서 Snack 프리팹을 드래그해 연결하세요.");
            return;
        }

        Transform cam = headCamera != null ? headCamera : (Camera.main != null ? Camera.main.transform : null);
        if (cam == null) return;

        Vector3 spawnPos = cam.position
            + cam.right * snackSpawnOffset.x
            + cam.up * snackSpawnOffset.y
            + cam.forward * snackSpawnOffset.z;
        Quaternion spawnRot = Quaternion.LookRotation(cam.forward, Vector3.up);
        GameObject clone = Instantiate(snackPrefab, spawnPos, spawnRot);

        SnackItem clonedSnack = clone.GetComponent<SnackItem>();
        if (clonedSnack != null && dogEat != null)
        {
            // 강아지는 플레이어가 이 간식을 손으로 집는 순간부터 쫓아간다 (SnackItem.IsHeld).
            dogEat.SetSnack(clonedSnack);
        }
    }

    private void SetOpen(RectTransform rt, CanvasGroup group, bool open, bool instant = false)
    {
        if (rt == null || group == null) return;
        rt.gameObject.SetActive(true);

        IEnumerator routine = SlideRoutine(rt, group, open, instant);
        if (rt == dropdown)
        {
            if (dropdownAnim != null) StopCoroutine(dropdownAnim);
            dropdownAnim = StartCoroutine(routine);
        }
        else
        {
            if (toolsAnim != null) StopCoroutine(toolsAnim);
            toolsAnim = StartCoroutine(routine);
        }
    }

    private IEnumerator SlideRoutine(RectTransform rt, CanvasGroup group, bool open, bool instant)
    {
        // pivot은 씬에 배치된 값을 그대로 쓴다 (Dropdown은 top pivot, ToolsPanel은 center pivot).
        float duration = instant ? 0f : slideDuration;
        float from = group.alpha;
        float to = open ? 1f : 0f;
        Vector3 fromScale = rt.localScale;
        Vector3 toScale = open ? Vector3.one : new Vector3(1f, 0.01f, 1f);

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            float e = 1f - Mathf.Pow(1f - p, 3f); // ease-out
            group.alpha = Mathf.Lerp(from, to, e);
            rt.localScale = Vector3.Lerp(fromScale, toScale, e);
            yield return null;
        }

        group.alpha = to;
        rt.localScale = toScale;
        group.interactable = open;
        group.blocksRaycasts = open;
        if (!open) rt.gameObject.SetActive(false);
    }
}
