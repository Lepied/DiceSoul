using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

public class SceneController : MonoBehaviour
{
    public static SceneController Instance;
    
    [Header("씬 이름")]
    public string mainMenuSceneName = "MainMenu";
    public string gameSceneName = "Game";
    
    [Header("페이드 설정")]
    public float fadeDuration = 1.0f;
    
    // 런타임에 생성될 페이드 UI
    private Canvas fadeCanvas;
    private CanvasGroup fadeCanvasGroup;
    private Image fadeImage;
    
    private bool isTransitioning = false;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 페이드 UI 동적 생성
            CreateFadeUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void CreateFadeUI()
    {
        // Canvas 생성
        GameObject canvasGO = new GameObject("SceneController_FadeCanvas");
        canvasGO.transform.SetParent(transform);
        
        fadeCanvas = canvasGO.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 80;  //GameOverDirector보다 낮게.
        
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        
        // CanvasGroup 추가
        fadeCanvasGroup = canvasGO.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
        
        // 검은색 이미지 생성
        GameObject imageGO = new GameObject("FadeImage");
        imageGO.transform.SetParent(canvasGO.transform);
        
        fadeImage = imageGO.AddComponent<Image>();
        fadeImage.color = Color.black;
        
        // 화면 전체를 덮도록 설정
        RectTransform rect = fadeImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
    
    // 페이드 없이 씬 전환
    public void LoadScene(string sceneName, System.Action onSceneLoaded = null)
    {
        if (!isTransitioning)
        {
            StartCoroutine(TransitionWithoutFade(sceneName, onSceneLoaded));
        }
    }
    
    // 페이드와 함께 씬 전환
    public void LoadSceneWithFade(string sceneName, System.Action onSceneLoaded = null)
    {
        if (!isTransitioning)
        {
            StartCoroutine(TransitionToScene(sceneName, onSceneLoaded));
        }
    }
    
    private IEnumerator TransitionToScene(string sceneName, System.Action callback)
    {
        isTransitioning = true;
        
        // 페이드 아웃
        yield return FadeOut();
        
        //BGM 페이드 아웃
        if (SoundManager.Instance != null && SoundManager.Instance.bgmSource != null)
        {
            SoundManager.Instance.bgmSource.DOFade(0f, fadeDuration * 0.5f);
        }
        
        // 씬 전환 전 정리 작업
        OnBeforeSceneUnload(SceneManager.GetActiveScene().name);
        
        // 씬 로드
        yield return SceneManager.LoadSceneAsync(sceneName);
        
        // 씬 로드 후 초기화 
        yield return new WaitForEndOfFrame();
        OnAfterSceneLoaded(sceneName);
        
        // 콜백 실행
        callback?.Invoke();
        
        //BGM 페이드 인
        if (SoundManager.Instance != null && SoundManager.Instance.bgmSource != null)
        {
            SoundManager.Instance.bgmSource.DOFade(0.5f, fadeDuration * 0.5f);
        }
        
        //페이드 인 (화면을 밝게)
        yield return FadeIn();
        
        isTransitioning = false;
    }
    
    private IEnumerator FadeOut()
    {
        if (fadeCanvasGroup == null) yield break;
        
        fadeCanvasGroup.blocksRaycasts = true;
        fadeCanvasGroup.DOKill();
        
        yield return fadeCanvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.InOutQuad).WaitForCompletion();
    }
    
    private IEnumerator FadeIn()
    {
        if (fadeCanvasGroup == null) yield break;
        
        fadeCanvasGroup.DOKill();
        
        yield return fadeCanvasGroup.DOFade(0f, fadeDuration).SetEase(Ease.InOutQuad).WaitForCompletion();
        
        fadeCanvasGroup.blocksRaycasts = false;
    }
    
    private IEnumerator TransitionWithoutFade(string sceneName, System.Action callback)
    {
        isTransitioning = true;
        
        // 씬 전환 전 정리 작업
        OnBeforeSceneUnload(SceneManager.GetActiveScene().name);
        
        // 씬 로드
        yield return SceneManager.LoadSceneAsync(sceneName);
        
        // 씬 로드 후 초기화
        yield return new WaitForEndOfFrame();
        OnAfterSceneLoaded(sceneName);
        
        // 콜백 실행
        callback?.Invoke();
        
        isTransitioning = false;
    }
    
    // 씬 전환 전 정리
    private void OnBeforeSceneUnload(string currentScene)
    {
        if (currentScene == gameSceneName)
        {
            // Game 씬을 떠날 때 정리 작업 (필요시 추가)
        }
    }
    
    // 씬 로드 후 초기화
    private void OnAfterSceneLoaded(string newScene)
    {
        if (newScene == gameSceneName)
        {
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartNewRun();
            }
        }
    }
    
    // 편의 메서드
    public void LoadMainMenu() => LoadScene(mainMenuSceneName);
    public void LoadGame() => LoadScene(gameSceneName);
    public void LoadMainMenuWithFade() => LoadSceneWithFade(mainMenuSceneName);
    public void LoadGameWithFade() => LoadSceneWithFade(gameSceneName);
}
