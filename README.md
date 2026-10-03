# 2v2 Autonomous Car Soccer using Reinforcement Learning

A 2D top-down vehicle soccer environment built in Unity and trained using Multi-Agent Reinforcement Learning (Unity ML-Agents). Four autonomous cars learn to drive, defend, rotate, and score goals from complete scratch using self-play over 1.7 billion simulation steps.

---

## Project Overview

* **Engine:** Unity (Universal Render Pipeline)
* **ML Framework:** Unity ML-Agents (Release 21 / v1.1.0) with PyTorch
* **Algorithm:** MA-POCA (Multi-Agent POst-operator Critic-Actor) with Self-Play
* **Setup:** 16 training arenas running side-by-side to gather data fast
* **Trained Steps:** 55.5M+ environment steps

---

## How It Works

### Observations (What each car sees)
Each car receives 21 normalized numeric inputs every decision step:
* Own position, velocity, and facing direction
* Relative distance and direction to the ball
* Ball velocity and heading towards both goals
* Positions and relative distances of the teammate and opponents
* Current boost meter level

### Actions (What each car controls)
* **Steering:** Continuous input (turn left / turn right)
* **Throttle:** Continuous input (drive forward / reverse)
* **Boost:** Discrete trigger (burst of speed when boost is available)

---

## Key Challenges & Solutions

### Stopping Teammates from Chasing the Same Ball ("Double-Committing")
Early in training, both cars on the same team would chase the ball at the same time like little kids in peewee soccer. This left our own goal completely unguarded and caused teammates to bump into each other.

To fix this, I added a dynamic role system in the C# environment code:
* **First Man (Closest to ball):** Gets rewarded for touching the ball and hitting it toward the opponent's goal.
* **Second Man (Support / Defender):** Does not get points for rushing the ball. Instead, they get small rewards for staying behind the play on defense and receive penalties if they crowd too close to their teammate.
* This taught the agents natural rotation: one attacks while the other covers the backfield.

### Fixing Cars Getting Stuck on Arena Walls
Because steering speed was tied to forward velocity, cars that hit the arena walls head-on dropped to zero speed. This locked their wheels so they couldn't turn to back up, leaving them trapped holding the throttle against the wall.
* Added a minimum turning radius so cars can pivot even at low speeds or a complete stop.
* Added an anti-stall penalty for holding forward throttle while staying stationary.
* Used zero-friction 2D physics materials along the walls so cars don't wedge into corners.

---

## Training Configuration (`soccer_poca.yaml`)

```yaml
behaviors:
  SoccerCar:
    trainer_type: poca
    hyperparameters:
      batch_size: 2048
      buffer_size: 20480
      learning_rate: 0.0003
      beta: 0.005
      epsilon: 0.2
      lambd: 0.95
      num_epoch: 3
    network_settings:
      normalize: true
      hidden_units: 256
      num_layers: 2
    reward_signals:
      extrinsic:
        gamma: 0.99
        strength: 1.0
    self_play:
      save_steps: 40000
      team_change: 150000
      swap_steps: 20000
      window: 10
      play_against_latest_model_ratio: 0.5
      initial_elo: 1200.0
    max_steps: 400000000
    time_horizon: 64
    summary_freq: 20000
