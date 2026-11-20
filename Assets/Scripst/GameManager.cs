using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private int score = 0;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private GameObject gameOverUi;
    [SerializeField] private GameObject gameWinUi;
    private bool isGameOver = false;

    void Start()
    {
        UpdateScore();
        gameOverUi.SetActive(false);
        gameWinUi.SetActive(false);

        // thêm nhẹ để chắc chắn vào scene mới không bị đứng do timeScale = 0 từ scene trước
        Time.timeScale = 1;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void AddScore(int points)
    {
        score += points;
        UpdateScore();
    }

    private void UpdateScore()
    {
        scoreText.text = score.ToString();
    }

    public void GameOver()
    {
        if (isGameOver) return; // chặn gọi lặp
        isGameOver = true;
        RunManager.Get().BankCells();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SFX_Die();
        }

        score = 0;
        Time.timeScale = 0;
        ProCombatHUD.ShowRunSummary(isVictory: false);
        if (gameOverUi != null) gameOverUi.SetActive(true);
    }


    public void GameWin()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SFX_Win();
        }

        SaveSystem.SubmitScore(score);
        SaveSystem.UnlockLevel(SceneManager.GetActiveScene().buildIndex + 1);
        RunManager.Get().BankCells();
        score = 0;
        Time.timeScale = 0;
        ProCombatHUD.ShowRunSummary(isVictory: true);
        if (gameWinUi != null) gameWinUi.SetActive(true);
    }


    public void Replay()
    {
        isGameOver = false;
        score = 0;
        UpdateScore();
        //ko nhận inputs
        Time.timeScale = 1;
        KillStreakManager.Get().ResetStats();
        if (SceneManager.GetActiveScene().name == "Run")
        {
            RunManager.Get().ResetRun();
            SceneTransition.Load("Run");
            return;
        }
        SceneTransition.Load("map 1");
    }

    public void MainMenu()
    {
        isGameOver = false;
        score = 0;
        UpdateScore();
        //ko nhận inputs
        Time.timeScale = 1;
        KillStreakManager.Get().ResetStats();
        SceneTransition.Load("MainMenu");
    }

    public void Continue()
    {
        isGameOver = false;
        score = 0;
        UpdateScore();
        //ko nhận inputs
        Time.timeScale = 1;
        gameWinUi.SetActive(false);
        if (SceneManager.GetActiveScene().name == "Run")
        {
            RunManager.Get().NextDepth();
            SceneTransition.Load("Run");
            return;
        }

        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextIndex >= SceneManager.sceneCountInBuildSettings)
        {
            SceneTransition.Load("MainMenu");
        }
        else
        {
            SceneTransition.Load(System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(nextIndex)));
        }
    }
}
