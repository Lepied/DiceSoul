using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using DG.Tweening;

public class SceneController : MonoBehaviour
{
    public static SceneController Instance;
    
    [Header("씬 이름")]
    public string mainMenuSceneName = "MainMenu";
    public string gameSceneName = "Game";
    
    [Header("페이드 설정")]
    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 1.0f;
    
    private bool isTransitioning = false;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 페이드 캔버스 초기화
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 0f;
                fadeCanvasGroup.blocksRaycasts = false;
            }
        }
        else
        {
            Destroy(gameObject);
        }
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
