# 🚁 3D Autonomous Search & Rescue Drone (Unity ML-Agents)

![Unity](https://img.shields.io/badge/Unity-6000.6.3f1-black?logo=unity)
![ML-Agents](https://img.shields.io/badge/ML--Agents-1.1.0-blue)
![Algorithm](https://img.shields.io/badge/Algorithm-PPO-orange)
![PyTorch](https://img.shields.io/badge/PyTorch-2.x-red?logo=pytorch)
![Model](https://img.shields.io/badge/Inference-ONNX-green)

An autonomous 3D Search & Rescue drone trained using **Deep Reinforcement Learning (Proximal Policy Optimization - PPO)** in Unity 6. The drone navigates an indoor multi-room obstacle environment using 360° raycast perception and vector observations to locate and reach a stranded target without colliding with walls, floors, or ceilings.

---

## 📋 Table of Contents
- [Project Overview](#-project-overview)
- [Environment & Agent Architecture](#-environment--agent-architecture)
  - [Observation Space](#observation-space)
  - [Action Space](#action-space)
  - [Reward Shaping & Solving Reward Hacking](#reward-shaping--solving-reward-hacking)
- [Training Configuration (PPO)](#-training-configuration-ppo)
- [Training Results & TensorBoard Analysis](#-training-results--tensorboard-analysis)
- [Project Structure](#-project-structure)
- [Getting Started](#-getting-started)
- [Authors](#-authors)

---

## 🎯 Project Overview

In search-and-rescue scenarios, autonomous drones must navigate complex indoor layouts where GPS is unavailable and obstacles block direct paths. In this project:
- A custom 3D indoor environment (`TrainingArea`) was built in Unity with outer boundary walls, interior dividing walls, and height limits.
- **4 parallel training environments** were run simultaneously to accelerate data collection and stabilize policy updates.
- The trained neural network was exported to **`RescueDrone.onnx`** and deployed in **Inference Only** mode, enabling real-time standalone autonomous flight in Unity without an active Python backend.

---

## 🧠 Environment & Agent Architecture

### Observation Space
The agent perceives its surroundings through a hybrid observation setup combining spatial raycasts and numerical state vectors:
1. **360° `RayPerceptionSensor3D`:**
   - **Max Ray Degrees:** `180°` (full 360° horizontal coverage)
   - **Ray Length:** `7m` (tuned to match indoor room dimensions for high-resolution obstacle distance sensing)
   - **Detectable Tags:** `Wall`, `Target`
2. **Vector Observations (`Space Size = 9`):**
   - Normalized 3D relative vector to target: `(target.localPosition - transform.localPosition) / 20f` *(3 floats)*
   - Normalized 3D velocity: `rb.linearVelocity / moveSpeed` *(3 floats)*
   - Normalized 3D local position: `transform.localPosition / 10f` *(3 floats)*

### Action Space
- **Continuous Actions (`Size = 3`):**
  - `actions[0]`: X-axis velocity (`[-1, 1]`)
  - `actions[1]`: Y-axis vertical altitude velocity (`[-1, 1]`)
  - `actions[2]`: Z-axis velocity (`[-1, 1]`)
- Movements are applied smoothly via `Rigidbody.linearVelocity` (`moveSpeed = 7f`) with gravity disabled and rotational axes frozen (`X, Y, Z`) to simulate stabilized quadcopter flight.

### Reward Shaping & Solving Reward Hacking
During experimentation, we iteratively refined our reward function across three training runs:
- **Run 1 (`DroneRun1`):** Sparse terminal rewards with standard force-based movement caused the agent to struggle navigating around interior walls (`Mean Reward: -0.82`).
- **Run 2 (`DroneRun2` - Reward Hacking):** Adding a continuous horizontal speed bonus encouraged the agent to fly in safe circles to farm positive rewards indefinitely without reaching the target (`Episode Length` maxed out at `400`).
- **Run 3 (`DroneRun3` - Optimal Policy):** Replaced speed bonuses with a **record-progress (`bestDistance`) reward**:
  - **Progress Toward Target:** `+0.1 * (bestDistance - currentDistance)` *(awarded only when beating the closest distance achieved so far, allowing the drone to detour around walls without penalty)*
  - **Target Reached (Rescue Complete):** `+2.0` and `EndEpisode()`
  - **Wall Collision or Out-of-Bounds (`y < 0.25` or `y > 4.2`):** `-1.0` and `EndEpisode()`
  - **Step Time Penalty:** `-0.001` per step to encourage the shortest, fastest path

---

## ⚙️ Training Configuration (PPO)

Training was configured in `config/rescue_drone.yaml` using Proximal Policy Optimization (PPO):

| Hyperparameter | Value | Purpose |
| :--- | :--- | :--- |
| `trainer_type` | `ppo` | Proximal Policy Optimization algorithm |
| `batch_size` | `256` | Number of experiences per gradient descent update |
| `buffer_size` | `4096` | Experiences collected before updating the policy |
| `learning_rate` | `0.0003` | Step size for Adam optimizer |
| `learning_rate_schedule` | `constant` | Maintains steady learning throughout exploration |
| `beta` | `0.025` | Entropy regularization to encourage maze exploration |
| `epsilon` | `0.2` | PPO clipping threshold for stable policy updates |
| `lambd` | `0.95` | GAE (Generalized Advantage Estimation) parameter |
| `gamma` | `0.99` | Discount factor prioritizing long-term target rescue |
| `hidden_units` / `num_layers` | `256` / `2` | 2-layer fully connected neural network with observation normalization |

---

## 📈 Training Results & TensorBoard Analysis

Training progress was monitored live via **TensorBoard** across all runs:

- **Mean Cumulative Reward (`DroneRun3`):** Climbed smoothly from `-1.0` and converged at **`+2.21`**.
- **Mean Episode Length (`DroneRun3`):** Dropped from `~140` steps down to **`~28` steps**, demonstrating that the drone consistently avoids all interior obstacles and reaches randomized target locations via near-optimal paths.

---

## 📁 Project Structure

```text
├── Assets/
│   ├── RescueDrone.onnx          # Final trained neural network model for Inference Mode
│   ├── Scenes/
│   │   └── SampleScene.unity     # 3D Search & Rescue indoor arena
│   └── Scripts/
│       ├── DroneAgent.cs         # RL Agent logic (observations, actions, rewards, heuristic)
│       └── PropellerScript.cs    # Visual rotor animation script
├── config/
│   └── rescue_drone.yaml         # ML-Agents PPO hyperparameter configuration
├── results/
│   └── DroneRun3/                # TensorBoard event logs and training checkpoints
└── .gitignore                    # Excludes Unity Library/, Temp/, and Python venv/
