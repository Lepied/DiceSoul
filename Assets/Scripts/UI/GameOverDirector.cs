using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class GameOverDirector : MonoBehaviour
{
    public static GameOverDirector Instance { get; private set; }

    [Header("연출 대상")]
    public Transform wallObject;
    public Transform enemyTransform;
    public Image transitionImage;
    public ParticleSystem speedLineEffect;

    public Canvas myCanvas;

    [Header("설정")]
    public float monsterRetreatDuration = 1.0f; // 몬스터가 물러나는 시간
    public float wallCrumbleDuration = 1.5f;   // 성벽 와르르 연출
    public float fadeDuration = 2.0f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (myCanvas != null)
            {
                myCanvas.sortingOrder = 1000;
            }
        }
        else
        {
            Destroy(gameObject);
        }

    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (myCanvas != null && Camera.main != null)
        {
            myCanvas.worldCamera = Camera.main;
            myCanvas.planeDistance = 30;
        }

        if (scene.name == "MainMenu")
        {
            PlayArrivalSequence();
        }
    }
    //게임오버 시퀀스 시작
    public void PlayGameOverSequence(int earnedCurrency)
    {

        // 모든 UI 및 주사위 숨기기
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideAllInGameUI();
        }
        
        if (DiceController.Instance != null)
        {
            DiceController.Instance.HideAllDice();
        }

        // 모든 적 오브젝트 비활성화
        Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy enemy in allEnemies)
        {
            if (enemy != null && enemy.gameObject != null)
            {
                enemy.gameObject.SetActive(false);
            }
        }
        Debug.Log($"[GameOverDirector] {allEnemies.Length}개의 적 비활성화 완료");

        if (enemyTransform == null)
        {
            Enemy activeEnemy = FindFirstObjectByType<Enemy>();
            if (activeEnemy != null) enemyTransform = activeEnemy.transform;
        }

        Sequence seq = DOTween.Sequence();
        //몬스터 관련 연출
        if (enemyTransform != null)
        {
            seq.Append(enemyTransform.DOMoveY(enemyTransform.position.y + 5.0f, monsterRetreatDuration).SetEase(Ease.OutQuad));
            seq.Append(enemyTransform.DOScale(Vector3.zero, 0.2f));
        }
        //성벽이 와르르 연출
        if (wallObject != null)
        {
            //강하게 진동 
            seq.Append(wallObject.DOShakePosition(1.0f, strength: 0.5f, vibrato: 30, randomness: 90));

            // 아래로 뚝 떨어짐 + 원자 분해 느낌
            seq.Join(wallObject.DOMoveY(wallObject.position.y - 20f, wallCrumbleDuration).SetEase(Ease.InBack));
            seq.Join(wallObject.DOScale(wallObject.localScale * 1.2f, wallCrumbleDuration));

            SpriteRenderer sr = wallObject.GetComponent<SpriteRenderer>();
            if (sr != null) seq.Join(sr.DOFade(0, wallCrumbleDuration));
        }
        //화면 전환 (위로 슈슉) ---
        if (transitionImage != null)
        {
            // 전환 패널 초기화
            transitionImage.gameObject.SetActive(true);
            transitionImage.rectTransform.anchoredPosition = Vector2.zero;
            Color c = transitionImage.color;
            c.a = 0; // 투명
            transitionImage.color = c;
            
            // 성벽 파괴 후 게임오버 스크린 표시하고 멈춤
            seq.AppendCallback(() =>
            {
                if (GameOverScreen.Instance != null)
                {
                    GameOverScreen.TriggerGameOver(0.5f);
                }
                else
                {
                }
            });
            
            // 여기서 멈춤 - 사용자 버튼 입력 대기
        }
    }
    //메인화면 도착 연출
    private void PlayArrivalSequence()
    {
        if (transitionImage == null) return;

        // 연출 시작 시 MainMenu UI 비활성화
        HideMainMenuUI();

        // 이제 패널을 투명하게 해서 마을을 보여줌.
        if (myCanvas != null && Camera.main != null)
        {
            myCanvas.worldCamera = Camera.main;
            myCanvas.planeDistance = 30; 
        }
        Sequence seq = DOTween.Sequence();

        // 잠시 숨고르기
        seq.AppendInterval(0.5f);

        // 파티클 서서히 정지
        seq.AppendCallback(() =>
        {
            if (speedLineEffect != null) speedLineEffect.Stop(); // 서서히 멈춤
        });

        // 다시 밝아지기
        seq.Append(transitionImage.DOFade(0f, 1.0f).SetEase(Ease.Linear));

        // 파티클 잔여물이 사라질 때까지 대기
        seq.AppendInterval(2.0f);
        
        // 연출이 다 끝나면 UI 표시 후 Director 파괴
        seq.OnComplete(() =>
        {
            ShowMainMenuUI();
            Destroy(gameObject);
        });
    }
    void OnDestroy()
    {
        if (Instance == this)
        {
            // 쓰레기치우기
            Instance = null;
        }
    }

    private void HideMainMenuUI()
    {
        MainMenuManager mainMenu = FindFirstObjectByType<MainMenuManager>();
        if (mainMenu != null)
        {
            // 모든 UI GameObject 비활성화
            if (mainMenu.deckSelectionPanel != null) mainMenu.deckSelectionPanel.SetActive(false);
            if (mainMenu.upgradeShopPanel != null) mainMenu.upgradeShopPanel.SetActive(false);
            if (mainMenu.generalStorePanel != null) mainMenu.generalStorePanel.SetActive(false);
            
            // 메인 버튼 GameObject들 비활성화
            if (mainMenu.startGameButton != null) mainMenu.startGameButton.gameObject.SetActive(false);
            if (mainMenu.continueButton != null) mainMenu.continueButton.gameObject.SetActive(false);
            if (mainMenu.openUpgradeButton != null) mainMenu.openUpgradeButton.gameObject.SetActive(false);
            if (mainMenu.openDeckButton != null) mainMenu.openDeckButton.gameObject.SetActive(false);
            if (mainMenu.openStoreButton != null) mainMenu.openStoreButton.gameObject.SetActive(false);
            if (mainMenu.quitGameButton != null) mainMenu.quitGameButton.gameObject.SetActive(false);
            if (mainMenu.metaCurrencyText != null) mainMenu.metaCurrencyText.gameObject.SetActive(false);
        }
    }

    private void ShowMainMenuUI()
    {
        MainMenuManager mainMenu = FindFirstObjectByType<MainMenuManager>();
        if (mainMenu != null)
        {
            // 모든 UI GameObject 활성화
            if (mainMenu.startGameButton != null) mainMenu.startGameButton.gameObject.SetActive(true);
            if (mainMenu.continueButton != null) mainMenu.continueButton.gameObject.SetActive(true);
            if (mainMenu.openUpgradeButton != null) mainMenu.openUpgradeButton.gameObject.SetActive(true);
            if (mainMenu.openDeckButton != null) mainMenu.openDeckButton.gameObject.SetActive(true);
            if (mainMenu.openStoreButton != null) mainMenu.openStoreButton.gameObject.SetActive(true);
            if (mainMenu.quitGameButton != null) mainMenu.quitGameButton.gameObject.SetActive(true);
            if (mainMenu.metaCurrencyText != null) mainMenu.metaCurrencyText.gameObject.SetActive(true);
        }
    }
    
    // 메인 메뉴로 전환
    public void PlayTransitionToMainMenu()
    {
        if (transitionImage == null) return;
        
        Sequence seq = DOTween.Sequence();
        
        if (speedLineEffect != null)
        {
            seq.AppendCallback(() =>
            {
                speedLineEffect.gameObject.SetActive(true);
                speedLineEffect.Play();
            });
        }
        
        seq.AppendInterval(0.4f);
        seq.AppendCallback(() =>
        {
            transitionImage.gameObject.SetActive(true);
            Color c = transitionImage.color;
            c.a = 0;
            transitionImage.color = c;
        });
        
        seq.Append(transitionImage.DOFade(1f, fadeDuration).SetEase(Ease.Linear));
        seq.AppendInterval(0.5f);
        seq.AppendCallback(() =>
        {
            if (SceneController.Instance != null)
            {
                SceneController.Instance.LoadMainMenu();
            }
            else
            {
                SceneManager.LoadScene("MainMenu");
            }
        });
    }
    
    // 게임 재시작
    public void PlayTransitionToRestart()
    {
        if (transitionImage == null) return;
        
        Sequence seq = DOTween.Sequence();
        
        // 슈슈슥 파티클 시작
        if (speedLineEffect != null)
        {
            seq.AppendCallback(() =>
            {
                speedLineEffect.gameObject.SetActive(true);
                speedLineEffect.Play();
            });
        }
        

        seq.AppendInterval(0.4f); 
        seq.AppendCallback(() =>
        {
            transitionImage.gameObject.SetActive(true);
            Color c = transitionImage.color;
            c.a = 0;
            transitionImage.color = c;
        });
        
        seq.Append(transitionImage.DOFade(1f, fadeDuration).SetEase(Ease.Linear));
        seq.AppendInterval(0.5f);
        seq.AppendCallback(() =>
        {
            // 새 런 시작
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartNewRun();
            }
            
            // 씬 재로드
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        });
    }

}