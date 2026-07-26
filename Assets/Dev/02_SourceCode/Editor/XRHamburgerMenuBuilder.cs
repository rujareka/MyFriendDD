using Oculus.Interaction.Surfaces;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// XRHamburgerMenu의 UI(햄버거 버튼/Dropdown/Tools 패널)를 코드가 아니라
/// 실제 하이어라키 GameObject로 생성해주는 에디터 툴.
///
/// 사용법: XRHamburgerMenu 컴포넌트 헤더 우클릭(⋮) -> "Build UI In Scene".
/// 다시 실행하면 이전에 만든 UI를 지우고 새로 만든다 (Undo 가능).
/// 생성 후에는 Hierarchy/Scene 뷰에서 HamburgerButton, Dropdown, ToolsPanel의
/// RectTransform을 직접 드래그해서 위치를 조정하면 된다.
/// </summary>
public static class XRHamburgerMenuBuilder
{
    private static readonly string[] ToolNames = { "Ball", "Snack", "Brush", "Toy", "Photo", "Map" };

    [MenuItem("CONTEXT/XRHamburgerMenu/Build UI In Scene")]
    private static void BuildUI(MenuCommand command)
    {
        var menu = (XRHamburgerMenu)command.context;
        var canvas = menu.GetComponentInChildren<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[XRHamburgerMenuBuilder] 하위에 Canvas가 없습니다. EmptyUIBackplateWithCanvas 인스턴스 위에 이 컴포넌트를 붙여주세요.");
            return;
        }
        RectTransform canvasRoot = canvas.GetComponent<RectTransform>();

        var so = new SerializedObject(menu);
        Vector2 canvasSize = so.FindProperty("canvasSize").vector2Value;

        Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Build XR Hamburger Menu UI");

        RemoveExisting(canvasRoot, "HamburgerButton");
        RemoveExisting(canvasRoot, "Dropdown");
        RemoveExisting(canvasRoot, "ToolsPanel");

        canvasRoot.sizeDelta = canvasSize;

        var boundsClipper = menu.GetComponentInChildren<BoundsClipper>();
        if (boundsClipper != null)
        {
            Vector3 size = boundsClipper.Size;
            boundsClipper.Size = new Vector3(canvasSize.x, canvasSize.y, size.z);
        }

        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        RectTransform hamburger = CreateButton(canvasRoot, "HamburgerButton", "≡", uiFont, 70, 70,
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -16));
        Button hamburgerButton = hamburger.GetComponent<Button>();
        UnityEventTools.AddVoidPersistentListener(hamburgerButton.onClick, menu.ToggleDropdown);

        RectTransform dropdown = BuildDropdown(canvasRoot, uiFont, menu, hamburger);
        RectTransform toolsPanel = BuildToolsPanel(canvasRoot, uiFont, menu, canvasSize);

        so.FindProperty("hamburgerButton").objectReferenceValue = hamburgerButton;
        so.FindProperty("dropdown").objectReferenceValue = dropdown;
        so.FindProperty("dropdownGroup").objectReferenceValue = dropdown.GetComponent<CanvasGroup>();
        so.FindProperty("toolsPanel").objectReferenceValue = toolsPanel;
        so.FindProperty("toolsGroup").objectReferenceValue = toolsPanel.GetComponent<CanvasGroup>();

        var tennisBallProp = so.FindProperty("tennisBallPrefab");
        if (tennisBallProp.objectReferenceValue == null)
        {
            tennisBallProp.objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Dev/03_PrePebs/InteractTennis.prefab");
        }

        var dogFetchProp = so.FindProperty("dogFetch");
        if (dogFetchProp.objectReferenceValue == null)
        {
            dogFetchProp.objectReferenceValue = Object.FindFirstObjectByType<DogFetch>();
        }

        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(menu);
        EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);

        Debug.Log("[XRHamburgerMenuBuilder] UI 생성 완료. Hierarchy에서 HamburgerButton / Dropdown / ToolsPanel의 " +
            "위치를 Scene 뷰나 RectTransform에서 직접 드래그해 조정하세요.");
    }

    private static void RemoveExisting(RectTransform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);
    }

    // ---------- Dropdown (State / Tools / Settings) ----------

    private static RectTransform BuildDropdown(RectTransform canvasRoot, Font uiFont, XRHamburgerMenu menu, RectTransform hamburger)
    {
        var dropdown = CreatePanel(canvasRoot, "Dropdown", 220, 10);
        // 햄버거 버튼 바로 아래, 우측 정렬. 이후 여기 anchoredPosition을 드래그해서 조정하면 된다.
        AnchorTo(dropdown, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -96));

        var bg = dropdown.gameObject.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.6f);
        dropdown.gameObject.AddComponent<CanvasGroup>();

        var layout = dropdown.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8;
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        var fitter = dropdown.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var stateBtn = CreateMenuButton(dropdown, "State", uiFont);
        UnityEventTools.AddVoidPersistentListener(stateBtn.onClick, menu.OnStateButtonClicked);

        var toolsBtn = CreateMenuButton(dropdown, "Tools", uiFont);
        UnityEventTools.AddVoidPersistentListener(toolsBtn.onClick, menu.OnToolsButtonClicked);

        var settingsBtn = CreateMenuButton(dropdown, "Settings", uiFont);
        UnityEventTools.AddVoidPersistentListener(settingsBtn.onClick, menu.OnSettingsButtonClicked);

        return dropdown;
    }

    // ---------- Tools 패널 (가로 스크롤 아이콘 목록) ----------

    private static RectTransform BuildToolsPanel(RectTransform canvasRoot, Font uiFont, XRHamburgerMenu menu, Vector2 canvasSize)
    {
        float panelWidth = canvasSize.x / 3f;
        var panel = CreatePanel(canvasRoot, "ToolsPanel", panelWidth, 260);
        // 화면 정중앙에서 살짝 아래. 이후 여기 anchoredPosition을 드래그해서 조정하면 된다.
        AnchorTo(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -120f));

        var bg = panel.gameObject.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.75f);
        panel.gameObject.AddComponent<CanvasGroup>();

        var scrollGO = new GameObject("ToolsScroll", typeof(RectTransform));
        var scrollRT = (RectTransform)scrollGO.transform;
        scrollRT.SetParent(panel, false);
        StretchFull(scrollRT, new Vector2(16, 16));
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;

        var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
        var viewportRT = (RectTransform)viewportGO.transform;
        viewportRT.SetParent(scrollRT, false);
        StretchFull(viewportRT, Vector2.zero);
        viewportGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);

        var contentGO = new GameObject("Content", typeof(RectTransform));
        var contentRT = (RectTransform)contentGO.transform;
        contentRT.SetParent(viewportRT, false);
        contentRT.anchorMin = new Vector2(0, 0);
        contentRT.anchorMax = new Vector2(0, 1);
        contentRT.pivot = new Vector2(0, 0.5f);
        contentRT.anchoredPosition = Vector2.zero;

        var hlg = contentGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16;
        hlg.padding = new RectOffset(16, 16, 16, 16);
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewportRT;
        scrollRect.content = contentRT;

        foreach (var toolName in ToolNames)
        {
            var icon = CreateButton(contentRT, $"Tool_{toolName}", toolName, uiFont, 140, 190,
                Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, setAnchors: false);
            var le = icon.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 140;
            le.preferredHeight = 190;

            Button button = icon.GetComponent<Button>();
            UnityAction<string> callback = menu.OnToolIconClicked;
            UnityEventTools.AddStringPersistentListener(button.onClick, callback, toolName);
        }

        return panel;
    }

    // ---------- UI 생성 헬퍼 ----------

    private static RectTransform CreatePanel(RectTransform parent, string name, float width, float height)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Build XR Hamburger Menu UI");
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(width, height);
        return rt;
    }

    private static RectTransform CreateButton(RectTransform parent, string name, string label, Font uiFont,
        float width, float height,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, bool setAnchors = true)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(go, "Build XR Hamburger Menu UI");
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(width, height);
        if (setAnchors)
            AnchorTo(rt, anchorMin, anchorMax, pivot, anchoredPos);

        var img = go.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.15f);

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        var textGO = new GameObject("Label", typeof(RectTransform));
        textGO.transform.SetParent(rt, false);
        var textRT = (RectTransform)textGO.transform;
        StretchFull(textRT, new Vector2(4, 4));
        var text = textGO.AddComponent<Text>();
        text.text = label;
        text.font = uiFont;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.fontSize = 26;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;

        return rt;
    }

    private static Button CreateMenuButton(RectTransform parent, string label, Font uiFont)
    {
        var rt = CreateButton(parent, $"Menu_{label}", label, uiFont, 200, 56,
            Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, setAnchors: false);
        var le = rt.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 56;
        return rt.GetComponent<Button>();
    }

    private static void AnchorTo(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, Vector2 anchoredPos)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
    }

    private static void StretchFull(RectTransform rt, Vector2 padding)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = padding;
        rt.offsetMax = -padding;
    }
}
