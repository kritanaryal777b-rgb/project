using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int  maxThrows   = 5;
    public int  throwsTaken = 0;
    public int  totalScore  = 0;
    public bool isGameOver  = false;

    private float gameEndTime = -1f;

    [Header("Feedback")]
    [SerializeField] private float feedDuration = 1.2f;
    private float feedEndTime = 0f;

    private Canvas           canvas;
    private TextMeshProUGUI  scoreText;
    private TextMeshProUGUI  throwsText;
    private TextMeshProUGUI  ballsLeftText;
    private TextMeshProUGUI  feedText;

    private GameObject      goPanel;
    private TextMeshProUGUI goTitleText;
    private TextMeshProUGUI goScoreText;
    private TextMeshProUGUI goStatsText;
    private Button          replayBtn;

    private bool IsMobile =>
#if UNITY_ANDROID || UNITY_IOS
        true;
#else
        false;
#endif

    void Awake()
    {
        Instance = this;
        SetupEventSystem();
        SetupUI();
    }

    void SetupEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
        }
    }

    void SetupUI()
    {
        GameObject canvasGO = new GameObject("GameUI");
        canvas = canvasGO.AddComponent<Canvas>();

#if UNITY_XR_ENABLED
        // world space canvas for vr
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<GraphicRaycaster>();
        RectTransform crt = canvasGO.GetComponent<RectTransform>();
        crt.sizeDelta              = new Vector2(800, 200);
        canvasGO.transform.position   = new Vector3(0, 3f, 4f);
        canvasGO.transform.localScale = Vector3.one * 0.004f;
#else
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();
#endif

        int hudSize     = IsMobile ? 52  : 36;
        int feedSize    = IsMobile ? 72  : 52;
        int goTitleSize = IsMobile ? 96  : 72;
        int goScoreSize = IsMobile ? 72  : 52;
        int goStatsSize = IsMobile ? 42  : 30;
        int btnTxtSize  = IsMobile ? 40  : 28;
        Vector2 btnSize = IsMobile ? new Vector2(380, 90) : new Vector2(260, 60);

        // score left, throws center, balls right
        scoreText = HUDLabel(canvasGO, "ScoreText", "SCORE: 0",
            hudSize, FontStyles.Bold, Color.white,
            new Vector2(0f, 1f), new Vector2(0.33f, 1f),
            TextAlignmentOptions.Left);

        throwsText = HUDLabel(canvasGO, "ThrowsText", "THROWS: 0/5",
            hudSize, FontStyles.Bold, Color.white,
            new Vector2(0.33f, 1f), new Vector2(0.67f, 1f),
            TextAlignmentOptions.Center);

        ballsLeftText = HUDLabel(canvasGO, "BallsText", "BALLS LEFT: 5",
            hudSize, FontStyles.Bold, Color.white,
            new Vector2(0.67f, 1f), new Vector2(1f, 1f),
            TextAlignmentOptions.Right);

        feedText = Label(canvasGO, "FeedbackText", "",
            feedSize, FontStyles.Bold, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 80), new Vector2(700, 100));
        feedText.alignment = TextAlignmentOptions.Center;
        feedText.gameObject.SetActive(false);

        goPanel = new GameObject("GameOverPanel");
        goPanel.transform.SetParent(canvasGO.transform, false);
        Image bg = goPanel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.78f);
        RectTransform panelRT = goPanel.GetComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        goTitleText = Label(goPanel, "GOTitle", "GAME OVER",
            goTitleSize, FontStyles.Bold, Color.red,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 180), new Vector2(800, 120));
        goScoreText = Label(goPanel, "GOScore", "FINAL SCORE: 0",
            goScoreSize, FontStyles.Bold, Color.yellow,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 60), new Vector2(800, 100));
        goStatsText = Label(goPanel, "GOStats", "",
            goStatsSize, FontStyles.Normal, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, -30), new Vector2(800, 60));

        goTitleText.alignment = TextAlignmentOptions.Center;
        goScoreText.alignment = TextAlignmentOptions.Center;
        goStatsText.alignment = TextAlignmentOptions.Center;

        GameObject btnGO = new GameObject("PlayAgainBtn");
        btnGO.transform.SetParent(goPanel.transform, false);
        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        replayBtn = btnGO.AddComponent<Button>();
        replayBtn.targetGraphic = btnImg;
        ColorBlock cb = replayBtn.colors;
        cb.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        cb.pressedColor     = new Color(0.05f, 0.05f, 0.05f, 1f);
        replayBtn.colors = cb;

        RectTransform btnRT    = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin        = new Vector2(0.5f, 0.5f);
        btnRT.anchorMax        = new Vector2(0.5f, 0.5f);
        btnRT.anchoredPosition = new Vector2(0, -130);
        btnRT.sizeDelta        = btnSize;

        TextMeshProUGUI btnLabel = Label(btnGO, "BtnLabel", "PLAY AGAIN",
            btnTxtSize, FontStyles.Bold, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, btnSize);
        btnLabel.alignment     = TextAlignmentOptions.Center;
        btnLabel.raycastTarget = false;

        replayBtn.onClick.AddListener(() =>
        {
#if !(UNITY_ANDROID || UNITY_IOS) && !UNITY_XR_ENABLED
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
#endif
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        });

        goPanel.SetActive(false);
    }

    private TextMeshProUGUI HUDLabel(GameObject parent, string name, string text,
        int fontSize, FontStyles style, Color color,
        Vector2 anchorMin, Vector2 anchorMax,
        TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text               = text;
        tmp.fontSize           = fontSize;
        tmp.fontStyle          = style;
        tmp.color              = color;
        tmp.alignment          = alignment;
        tmp.enableWordWrapping = false;
        tmp.overflowMode       = TextOverflowModes.Overflow;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(20f,  -100f);
        rt.offsetMax = new Vector2(-20f, -20f);

        return tmp;
    }

    private TextMeshProUGUI Label(GameObject parent, string name, string text,
        int fontSize, FontStyles style, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.fontStyle = style;
        tmp.color     = color;

        RectTransform rt    = go.GetComponent<RectTransform>();
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;

        return tmp;
    }

    public void RegisterThrow()
    {
        if (isGameOver || throwsTaken >= maxThrows) return;
        throwsTaken++;
        UpdateHUD();
        // delay game over to let ball land
        if (throwsTaken >= maxThrows)
            gameEndTime = Time.time + 3f;
    }

    public void RegisterScore(Pickupable ball, Transform hoop)
    {
        if (isGameOver) return;
        if (ball == null || !ball.isInFlight || ball.hasScoredThisShot) return;

        int points = 1;
        if (hoop != null)
        {
            // more points for longer shots
            float dist = Vector3.Distance(ball.shootPosition, hoop.position);
            if (dist > 15f)     points = 3;
            else if (dist > 8f) points = 2;
        }

        ball.MarkScored();
        totalScore += points;
        UpdateHUD();
        Feedback($"+{points} POINT{(points > 1 ? "S" : "")}!", Color.green);
    }

    public void RegisterMiss()
    {
        Feedback("MISS!", new Color(1f, 0.3f, 0.3f));
    }

    private void UpdateHUD()
    {
        if (scoreText)     scoreText.text     = $"SCORE: {totalScore}";
        if (throwsText)    throwsText.text    = $"THROWS: {throwsTaken}/{maxThrows}";
        if (ballsLeftText) ballsLeftText.text = $"BALLS LEFT: {maxThrows - throwsTaken}";
    }

    private void Feedback(string text, Color color)
    {
        if (feedText == null) return;
        feedText.text  = text;
        feedText.color = color;
        feedText.gameObject.SetActive(true);
        feedEndTime = Time.time + feedDuration;
    }

    void Update()
    {
        if (feedText != null && feedText.gameObject.activeSelf)
        {
            float remaining = feedEndTime - Time.time;
            if (remaining <= 0f)
                feedText.gameObject.SetActive(false);
            else
            {
                // fade out feedback text
                Color c = feedText.color;
                c.a = Mathf.Clamp01(remaining / feedDuration);
                feedText.color = c;
            }
        }

        if (!isGameOver && gameEndTime > 0f && Time.time >= gameEndTime)
        {
            isGameOver = true;

#if !(UNITY_ANDROID || UNITY_IOS) && !UNITY_XR_ENABLED
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
#endif

            if (goScoreText) goScoreText.text = $"FINAL SCORE: {totalScore}";
            if (goStatsText) goStatsText.text = $"SHOTS TAKEN: {throwsTaken}   |   BALLS LEFT: {maxThrows - throwsTaken}";
            if (goPanel)     goPanel.SetActive(true);
        }
    }
}