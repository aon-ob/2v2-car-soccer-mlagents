using UnityEngine;
using UnityEngine.Rendering.Universal;


public class BoostPad : MonoBehaviour
{
    public bool isBigPad = true; // Set this to false in the inspector for small pads
    public float respawnTime = 5f;

    public bool isActive = true;
    private float timer = 0f;
    private SpriteRenderer sr;
    private Light2D light;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        light = GetComponent<Light2D>();
    }

    void Update()
    {
        if (!isActive)
        {
            timer += Time.deltaTime;
            if (timer >= respawnTime)
            {
                isActive = true;
                sr.color = new Color(1, 212/255f, 0, 1);
                light.enabled = true; // Turn on the light
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if a car touched the pad
        if (other.CompareTag("Car") && isActive)
        {
            CarController car = other.GetComponent<CarController>();
            if (car != null)
            {
                // If the car is already at 100% boost, then do nothing.
                if (car.currentBoost >= car.maxBoost) return; 

                if (isBigPad) car.currentBoost = car.maxBoost; // Full boost
                else car.currentBoost = Mathf.Clamp(car.currentBoost + 15f, 0f, car.maxBoost); // Small boost
                
                isActive = false;
                sr.color = new Color(123/255f, 106/255f, 23/255f, 255/255f); // Hide the pad
                light.enabled = false; // Turn off the light
                timer = 0f;
            }
        }
    }
}