using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scene Names")]
    [SerializeField] private string mainMenuScene  = "MainMenu";
    [SerializeField] private string mapScene        = "Map";
    [SerializeField] private string combatScene     = "Combat";
    [SerializeField] private string shopScene       = "Shop";
    [SerializeField] private string rewardScene     = "Reward";
    public GameState CurrentState { get; private set; } = GameState.MainMenu;

    [Header("Run Info (read-only in inspector)")]
    [SerializeField, ReadOnly] private int   currentFloor    = 0;
    [SerializeField, ReadOnly] private int   runSeed         = 0;
    [SerializeField, ReadOnly] private float playTimeSeconds = 0f;
    [SerializeField, ReadOnly] private bool  runActive       = false;

    public int   CurrentFloor     => currentFloor;
    public int   RunSeed          => runSeed;
    public float PlayTimeSeconds  => playTimeSeconds;
    public bool  RunActive        => runActive;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (runActive && CurrentState != GameState.Paused)
            playTimeSeconds += Time.deltaTime;
    }
    public void TransitionTo(GameState newState)
    {
        if (newState == CurrentState) return;

        GameState prev = CurrentState;
        CurrentState = newState;

        OnStateExit(prev);
        OnStateEnter(newState);

        GameEvents.Raise(new OnGameStateChangedEvent(prev, newState));
        Debug.Log($"[GameManager] State: {prev} → {newState}");
    }

    private void OnStateExit(GameState state)
    {
        switch (state)
        {
            case GameState.Combat:
                // Combat teardown (DeckManager reset, etc.) handled by CombatManager
                break;
        }
    }

    private void OnStateEnter(GameState state)
    {
        switch (state)
        {
            case GameState.Map:
                LoadScene(mapScene);
                break;
            case GameState.Combat:
                LoadScene(combatScene);
                break;
            case GameState.Shop:
                LoadScene(shopScene);
                break;
            case GameState.Reward:
                LoadScene(rewardScene);
                break;
            case GameState.MainMenu:
                LoadScene(mainMenuScene);
                break;
            case GameState.GameOver:
                HandleGameOver();
                break;
            case GameState.Victory:
                HandleVictory();
                break;
        }
    }

    public void StartNewRun(int seed = -1)
    {
        runSeed          = seed < 0 ? Random.Range(0, int.MaxValue) : seed;
        currentFloor     = 0;
        playTimeSeconds  = 0f;
        runActive        = true;

        Random.InitState(runSeed);
        PlayerInventory.Instance?.InitializeForNewRun();
        TransitionTo(GameState.Map); //at the start of a new run, always begin from the map. 
    }

    public void AdvanceFloor()
    {
        currentFloor++;
        GameEvents.Raise(new OnFloorCompletedEvent(currentFloor));
    }

    public void EndRun(bool playerWon)
    {
        runActive = false;
        TransitionTo(playerWon ? GameState.Victory : GameState.GameOver);
    }
    public void Pause()
    {
        if (CurrentState == GameState.Paused) return;
        TransitionTo(GameState.Paused);
        Time.timeScale = 0f;
    }

    public void Unpause()
    {
        if (CurrentState != GameState.Paused) return;
        Time.timeScale = 1f;
        TransitionTo(GameState.Map);
    }

    private void LoadScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

    private void HandleGameOver()
    {
        Debug.Log($"[GameManager] Game Over. Run lasted {playTimeSeconds:F1}s over {currentFloor} floors.");
    }

    private void HandleVictory()
    {
        Debug.Log($"[GameManager] Victory! Run lasted {playTimeSeconds:F1}s over {currentFloor} floors.");
    }
}
public enum GameState
{
    MainMenu,
    Map,
    Combat,
    Shop,
    Reward,
    Event,
    Rest,
    Paused,
    GameOver,
    Victory
}

#if UNITY_EDITOR
[UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : UnityEditor.PropertyDrawer
{
    public override void OnGUI(Rect pos, UnityEditor.SerializedProperty prop, GUIContent label)
    {
        GUI.enabled = false;
        UnityEditor.EditorGUI.PropertyField(pos, prop, label);
        GUI.enabled = true;
    }
}
#endif

public class ReadOnlyAttribute : PropertyAttribute { }
