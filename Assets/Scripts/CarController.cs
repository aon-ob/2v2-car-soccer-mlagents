using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class CarController : Agent
{
    [Header("Car Movement")]
    public float accelerationFactor = 30f;
    public float turnFactor = 3.5f;
    public float driftFactor = 0.95f;
    public float maxSpeed = 9f;
    public float maxBoostSpeed = 15f;
    public float maxAccelChangePerDecision = 0.25f;

    [Header("Arena & Team")]
    public Vector2 arenaSize = new Vector2(29f, 20f);
    public bool isBlueTeam;
    public Transform ball;
    public Transform ownGoal;
    public Transform enemyGoal;
    public Transform teammate;
    public Transform[] enemies = new Transform[2];

    [Header("Boost")]
    public ParticleSystem boostParticles;
    public float maxBoost = 100f;
    public float currentBoost = 33f;

    private Rigidbody2D rb;
    private Rigidbody2D ballRb;
    private float accelerationInput = 0f;
    private float steeringInput = 0f;
    private float rotationAngle = 0f;
    private bool isBoosting = false;
    private float ballTouchCooldown = 0f;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        if (ball != null) ballRb = ball.GetComponent<Rigidbody2D>();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float teamSign = isBlueTeam ? -1f : 1f;

        // Normalized Field Position (2)
        sensor.AddObservation((transform.localPosition.x * teamSign) / (arenaSize.x * 0.5f));
        sensor.AddObservation(transform.localPosition.y / (arenaSize.y * 0.5f));

        // Ego-centric Car Velocities (2)
        Vector2 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        sensor.AddObservation(localVel.x / maxBoostSpeed);
        sensor.AddObservation(localVel.y / maxBoostSpeed);

        // Car Orientation (2)
        float adjustedAngle = (rotationAngle + (isBlueTeam ? 180f : 0f)) * Mathf.Deg2Rad;
        sensor.AddObservation(Mathf.Sin(adjustedAngle));
        sensor.AddObservation(Mathf.Cos(adjustedAngle));

        // Ball Position & Velocity relative to Car (4)
        if (ball != null && ballRb != null)
        {
            Vector2 localBallPos = transform.InverseTransformPoint(ball.position);
            sensor.AddObservation(localBallPos.x / arenaSize.x);
            sensor.AddObservation(localBallPos.y / arenaSize.y);

            Vector2 localBallVel = transform.InverseTransformDirection(ballRb.linearVelocity);
            sensor.AddObservation(localBallVel.x / maxBoostSpeed);
            sensor.AddObservation(localBallVel.y / maxBoostSpeed);
        }
        else
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }

        // Goals relative to Car (4)
        Vector2 localEnemyGoal = enemyGoal != null ? (Vector2)transform.InverseTransformPoint(enemyGoal.position) : Vector2.zero;
        sensor.AddObservation(localEnemyGoal.x / arenaSize.x);
        sensor.AddObservation(localEnemyGoal.y / arenaSize.y);

        Vector2 localOwnGoal = ownGoal != null ? (Vector2)transform.InverseTransformPoint(ownGoal.position) : Vector2.zero;
        sensor.AddObservation(localOwnGoal.x / arenaSize.x);
        sensor.AddObservation(localOwnGoal.y / arenaSize.y);

        // Teammate relative to Car (2)
        if (teammate != null)
        {
            Vector2 localTeammate = transform.InverseTransformPoint(teammate.position);
            sensor.AddObservation(localTeammate.x / arenaSize.x);
            sensor.AddObservation(localTeammate.y / arenaSize.y);
        }
        else
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }

        // Enemies relative to Car (4)
        for (int i = 0; i < 2; i++)
        {
            if (enemies != null && i < enemies.Length && enemies[i] != null)
            {
                Vector2 localEnemy = transform.InverseTransformPoint(enemies[i].position);
                sensor.AddObservation(localEnemy.x / arenaSize.x);
                sensor.AddObservation(localEnemy.y / arenaSize.y);
            }
            else
            {
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
            }
        }

        // Boost Level (1)
        sensor.AddObservation(currentBoost / maxBoost);

        // Total vector observation size is 21
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        AddReward(-0.0002f); // Time penalty

        float moveInput = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        steeringInput = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        int boostInput = actions.DiscreteActions[0];

        accelerationInput = Mathf.MoveTowards(accelerationInput, moveInput, maxAccelChangePerDecision);
        isBoosting = (boostInput == 1 && currentBoost > 2f);

        float myDistToBall = ball != null ? Vector2.Distance(transform.position, ball.position) : 0f;
        float mateDistToBall = (teammate != null && ball != null) ? Vector2.Distance(teammate.position, ball.position) : 999f;
        bool isFirstMan = myDistToBall <= mateDistToBall;

        // Only the 1st man gets rewarded for pushing the ball forward
        if (isFirstMan && ballRb != null && enemyGoal != null)
        {
            Vector2 dirToGoal = ((Vector2)enemyGoal.position - ballRb.position).normalized;
            float ballSpeedTowardsGoal = Vector2.Dot(ballRb.linearVelocity, dirToGoal);
            if (ballSpeedTowardsGoal > 0f)
            {
                AddReward(ballSpeedTowardsGoal * 0.0005f);
            }
        }

        // Spacing and rotation for the 2nd man (not the closest to the ball)
        if (!isFirstMan && ball != null && teammate != null && ownGoal != null && enemyGoal != null)
        {
            float distToTeammate = Vector2.Distance(transform.position, teammate.position);

            // Anti double comit penalty if the 2nd car is too close to the 1st car and the ball 
            if (myDistToBall < 4.5f && distToTeammate < 4.0f)
            {
                AddReward(-0.002f); // add penalty
            }

            // Defensive rotation reward for the 2nd man to be encouraged to rotate back and defend
            Vector2 dirBallToOwnGoal = ((Vector2)ownGoal.position - (Vector2)ball.position).normalized;
            Vector2 ballToMe = (Vector2)transform.position - (Vector2)ball.position;
            float defensiveDepth = Vector2.Dot(ballToMe, dirBallToOwnGoal);

            // If positioned between the ball and own net with healthy spacing (4 to 12 units back)
            if (defensiveDepth > 3.0f && defensiveDepth < 12.0f)
            {
                AddReward(0.0004f); // Encourages defending the backfield
            }

            // Shot block positioning penalty if the 2nd man is standing in the way between the ball and the enemy goal
            Vector2 dirBallToEnemyGoal = ((Vector2)enemyGoal.position - (Vector2)ball.position).normalized;
            float distAlongShotLine = Vector2.Dot(ballToMe, dirBallToEnemyGoal);
            float lateralDeviationFromShot = Mathf.Abs(ballToMe.x * dirBallToEnemyGoal.y - ballToMe.y * dirBallToEnemyGoal.x);

            if (distAlongShotLine > 1.5f && lateralDeviationFromShot < 1.2f)
            {
                AddReward(-0.0025f); // Clear out of the teammate's shooting lane
            }
        }
    }

    void FixedUpdate()
    {
        if (isBoosting)
        {
            currentBoost = Mathf.Max(0f, currentBoost - 25f * Time.fixedDeltaTime);
            if (currentBoost <= 0f) isBoosting = false;
        }
        else
        {
            currentBoost = Mathf.Min(maxBoost, currentBoost + 3f * Time.fixedDeltaTime);
        }

        if (boostParticles != null)
        {
            if (isBoosting && !boostParticles.isPlaying) boostParticles.Play();
            else if (!isBoosting && boostParticles.isPlaying) boostParticles.Stop();
        }

        ApplyEngineForce();
        KillOrthogonalVelocity();
        ApplySteering();

        // Anti stuck wheel spin penalty
        if (accelerationInput > 0.3f && rb.linearVelocity.magnitude < 0.2f)
        {
            AddReward(-0.002f);
        }

        if (ballTouchCooldown > 0f) ballTouchCooldown -= Time.fixedDeltaTime;
    }

    private void ApplyEngineForce()
    {
        float speed = Vector2.Dot(transform.up, rb.linearVelocity);
        float currentMax = isBoosting ? maxBoostSpeed : maxSpeed;

        if (isBoosting && speed < currentMax)
        {
            rb.AddForce(transform.up * 45f, ForceMode2D.Force);
        }
        else if (accelerationInput != 0f && speed < currentMax)
        {
            rb.AddForce(transform.up * (accelerationInput * accelerationFactor), ForceMode2D.Force);
        }
    }

    private void ApplySteering()
    {
        float speed = Vector2.Dot(transform.up, rb.linearVelocity);
        float speedFactor = Mathf.Clamp(rb.linearVelocity.magnitude / 6f, 0.35f, 1.0f);
        float reverseFactor = speed < -0.1f ? -1f : 1f;

        rotationAngle -= steeringInput * turnFactor * reverseFactor * speedFactor;
        rb.MoveRotation(rotationAngle);
    }

    private void KillOrthogonalVelocity()
    {
        Vector2 forwardVel = transform.up * Vector2.Dot(rb.linearVelocity, transform.up);
        Vector2 rightVel = transform.right * Vector2.Dot(rb.linearVelocity, transform.right);
        rb.linearVelocity = forwardVel + (rightVel * driftFactor);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ball") && ballTouchCooldown <= 0f)
        {
            float myDistToBall = ball != null ? Vector2.Distance(transform.position, ball.position) : 0f;
            float mateDistToBall = (teammate != null && ball != null) ? Vector2.Distance(teammate.position, ball.position) : 999f;

            // Only the 1st man gets rewarded for touching the ball
            if (myDistToBall <= mateDistToBall)
            {
                AddReward(0.06f);
            }
            ballTouchCooldown = 0.5f;
        }
        else if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.01f);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.003f);
        }
        else if (collision.gameObject.CompareTag("Car"))
        {
            AddReward(-0.001f);
        }
    }

    public void ResetCar(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rotationAngle = rotation.eulerAngles.z;
        accelerationInput = 0f;
        steeringInput = 0f;
        isBoosting = false;
        currentBoost = 33f;

        if (boostParticles != null && boostParticles.isPlaying)
        {
            boostParticles.Stop();
        }
    }

    public void GoalScored(bool scoredForMyTeam) { }
}