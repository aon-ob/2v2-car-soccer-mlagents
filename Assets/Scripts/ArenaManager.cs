using UnityEngine;
using UnityEngine.UI;
using Unity.MLAgents;

public class ArenaManager : MonoBehaviour
{
    [Header("UI (Optional - Assign on Primary Arena)")]
    public Text statsText;

    [Header("Ball")]
    public Rigidbody2D ball;
    public Transform ballSpawn;

    [Header("Spawns (Keep your original 6 spawns)")]
    public Transform[] carSpawns;

    [Header("Cars (0=Blue1, 1=Orange1, 2=Blue2, 3=Orange2)")]
    public CarController[] cars;

    [Header("Episode Settings")]
    public float maxEpisodeTime = 30f;
    private float episodeTimer = 0f;

    private float startTime;
    private int roundsPlayed = 0;
    private int blueScore = 0;
    private int orangeScore = 0;

    private SimpleMultiAgentGroup blueGroup;
    private SimpleMultiAgentGroup orangeGroup;

    void Awake()
    {
        blueGroup = new SimpleMultiAgentGroup();
        orangeGroup = new SimpleMultiAgentGroup();

        foreach (var car in cars)
        {
            if (car != null)
            {
                if (car.isBlueTeam) blueGroup.RegisterAgent(car);
                else orangeGroup.RegisterAgent(car);
            }
        }
    }

    void Start()
    {
        startTime = Time.time;
        UpdateStatsText();
        ResetRound();
    }

    void Update()
    {
        episodeTimer += Time.deltaTime;
        UpdateStatsText();

        if (episodeTimer >= maxEpisodeTime)
        {
            StalemateReset();
        }
    }

    public void ScoreGoal(string scoringTeam)
    {
        if (scoringTeam == "Blue")
        {
            blueScore++;
            blueGroup.AddGroupReward(1.0f);
            orangeGroup.AddGroupReward(-1.0f);
        }
        else if (scoringTeam == "Orange")
        {
            orangeScore++;
            orangeGroup.AddGroupReward(1.0f);
            blueGroup.AddGroupReward(-1.0f);
        }

        roundsPlayed++;
        blueGroup.EndGroupEpisode();
        orangeGroup.EndGroupEpisode();
        ResetRound();
    }

    private void StalemateReset()
    {
        roundsPlayed++;
        blueGroup.AddGroupReward(-0.1f);
        orangeGroup.AddGroupReward(-0.1f);

        blueGroup.EndGroupEpisode();
        orangeGroup.EndGroupEpisode();
        ResetRound();
    }

    public void ResetRound()
    {
        episodeTimer = 0f;

        // Reset ball to center
        if (ball != null && ballSpawn != null)
        {
            ball.transform.localPosition = ballSpawn.localPosition;
            ball.linearVelocity = Vector2.zero;
            ball.angularVelocity = 0f;
        }

        // Restored exact original spawn rotation
        int random = Random.Range(0, 3);
        if (random == 0)
        {
            cars[0].ResetCar(carSpawns[0].position, carSpawns[0].rotation); // Blue1
            cars[1].ResetCar(carSpawns[1].position, carSpawns[1].rotation); // Orange1
            cars[2].ResetCar(carSpawns[2].position, carSpawns[2].rotation); // Blue2
            cars[3].ResetCar(carSpawns[5].position, carSpawns[5].rotation); // Orange2
        }
        else if (random == 1)
        {
            cars[0].ResetCar(carSpawns[0].position, carSpawns[0].rotation); // Blue1
            cars[1].ResetCar(carSpawns[1].position, carSpawns[1].rotation); // Orange1
            cars[2].ResetCar(carSpawns[4].position, carSpawns[4].rotation); // Blue2
            cars[3].ResetCar(carSpawns[5].position, carSpawns[5].rotation); // Orange2
        }
        else if (random == 2)
        {
            cars[0].ResetCar(carSpawns[4].position, carSpawns[4].rotation); // Blue1
            cars[1].ResetCar(carSpawns[3].position, carSpawns[3].rotation); // Orange1
            cars[2].ResetCar(carSpawns[2].position, carSpawns[2].rotation); // Blue2
            cars[3].ResetCar(carSpawns[5].position, carSpawns[5].rotation); // Orange2
        }
    }

    private void UpdateStatsText()
    {
        if (statsText == null) return;

        float elapsedTime = Time.time - startTime;
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        statsText.text = $"Score: Blue {blueScore} - {orangeScore} Orange   |   Rounds: {roundsPlayed}   |   Time: {minutes:00}:{seconds:00}";
    }
}