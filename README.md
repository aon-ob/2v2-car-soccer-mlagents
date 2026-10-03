# Autonomous 2v2 Car Soccer AI — Multi-Agent Reinforcement Learning (MA-POCA)

![Unity](https://img.shields.io/badge/Unity-URP-blue?logo=unity)
![ML-Agents](https://img.shields.io/badge/Unity%20ML--Agents-v1.1.0-orange)
![PyTorch](https://img.shields.io/badge/PyTorch-CUDA%20Accelerated-ee4c2c?logo=pytorch)
![License](https://img.shields.io/badge/License-MIT-green)

An autonomous 2v2 multi-agent soccer environment built in Unity using the Universal Render Pipeline (URP) and trained using Multi-Agent Post-Operator Critic-Actor (**MA-POCA**). 

The agents start with zero prior knowledge of vehicle control, soccer tactics, or spatial awareness, and learn complex team play, rotational defense, passing, and striking over **55M+ environment simulation steps**.

---

## Technical Overview

* **Algorithm:** Multi-Agent POCA (Centralized Critic, Decentralized Execution) with Self-Play
* **Simulation Architecture:** 16 decentralized parallel stadiums running concurrent continuous physics
* **Observation Space:** 21 ego-centric continuous observations (normalized field positions, linear/angular velocities, relative ball kinematics, goal headings, teammate/adversary offsets, and boost tank fill)
* **Action Space:** Mixed continuous (Steering, Acceleration/Reverse) and discrete (Rocket Boost)
* **Training Throughput:** Scaled up to 25,000+ steps/second via headless GPU execution

---

## System Architecture & Training Methodology

### 1. The Multi-Agent Credit Assignment Problem
In cooperative 2v2 soccer, independent reinforcement learning agents suffer from selfish reward-seeking: both teammates dive headlong into the ball (the "double-commit" trap), leaving their own net wide open.

To enforce tactical division of labor without hardcoded heuristic state machines, the reward formulation incorporates **dynamic role gating**:
* **1st Man (Striker):** Dynamically computed as the agent nearest to the ball. The 1st man is eligible for ball-contact rewards (`+0.06`) and directional shot propulsion shaping (`Vector2.Dot(ballVelocity, goalDir) * 0.0005`).
* **2nd Man (Support/Anchor):** Gated out of ball-hunting rewards. Instead, the 2nd man receives positive return for holding defensive depth between the ball and home goal (`+0.0004/step`), while receiving negative penalties for crowding within $4.5$ units of the striker or blocking the primary shot trajectory.

### 2. Physical Stall Elimination & Kinematics
Standard simulated vehicle physics tie steering radius to linear velocity ($v / v_{\max}$). When agents struck perimeter walls head-on, velocity dropped to zero, locking the steering wheels and trapping the policy in continuous forward-throttle local optima.
* Implemented a clamped low-speed turning threshold (`Mathf.Clamp(speed / 6f, 0.35f, 1f)`), allowing cars to rotate their chassis from a dead stop.
* Added anti-stall throttle penalties for agents holding high acceleration inputs with near-zero forward momentum.
* Stripped collider surface friction via custom `PhysicsMaterial2D` assets to eliminate physical corner wedging.

### 3. Hyperparameter Configuration (`soccer_poca.yaml`)

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
    max_steps: 75000000
    time_horizon: 64
    summary_freq: 20000
