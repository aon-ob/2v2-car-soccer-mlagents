using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    [Tooltip("Drag the Stadium/GameManager object for this arena here")]
    public ArenaManager arena;

    [Tooltip("Check this if the ball entering this goal gives Blue a point (Orange's net).")]
    public bool isOrangeGoal; 

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ball"))
        {
            if (arena != null)
            {
                // Orange goal triggered means Blue scored and Blue goal triggered means Orange scored
                arena.ScoreGoal(isOrangeGoal ? "Blue" : "Orange");
            }
        }
    }
}