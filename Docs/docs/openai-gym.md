# Reinforcement Learning with OpenAI Gym

[OpenAI Gym](https://gym.openai.com) is a toolkit for developing reinforcement learning algorithms. Gym provides a collection of test problems called environments which can be used to train an agent using a reinforcement learning. Each environment defines the reinforcement learnign problem the agent will try to solve.

To facilitate developing reinforcement learning algorithms with the [LGSVL Simulator](https://www.lgsvlsimulator.com), we have developed `gym-lgsvl`, a custom environment that using the openai gym interface. `gym-lgsvl` can be used with general reinforcement learning algorithms implementations that are compatible with openai gym. Developers can modify the environment to define the specific reinforcement learning problem they are trying to solve.

## Requirements

* Python >= 3.5
* Pip
* lgsvl simulator API
* openai gym
* numpy
* opencv


## Setup

1. Clone the LGSVL Simulator repository:
```
git clone https://github.com/lgsvl/simulator.git
```
2. Install Python API using [this guide](https://github.com/lgsvl/simulator/tree/master/Api).

3. Install openai gym, numpy, and opencv:
```
pip install --user gym, numpy, opencv-python
```

4. Clone this repository:
```
git clone https://github.com/lgsvl/gym-lgsvl.git
```

5. Install gym-lgsvl using pip:
```
cd gym-lgsvl/
pip install --user -e .
```

## Getting Started
The simulator must be running on the `menu` scene to be used with the `gym-lgsvl`. The scene can be loaded either in the Unity Editor or the simulator binary build (download [latest release](https://github.com/lgsvl/simulator/releases)). The binary build will have superior performance.

The script named `random_agent.py` will launch a random agent which will sample acceleration and steering values for the ego vehicle for every step of the episode. Run the agent to test your setup:

```
./random_agent.py
```

After a few seconds the `SanFrancisco` scene should load and spawn the ego vehicle along with some NPC vehicles. The ego vehicle will drive randomly.

`gym_lgsvl` can be used with RL libraries that support openai gym environments. Below is an example of training using the A2C implementation from [baselines](https://github.com/openai/baselines):
```
python -m baselines.run --alg=a2c --env=gym_lgsvl:lgsvl-v0 --num_timesteps=1e5
```


## Customizing the environment
The specifics of the environment you will need will depend on the reinforcement learning problem you are trying to solve. By default, the `gym-lgsvl` environment has a simple setup intended to be a starting point for building more advanced problems. Training an agent with the default environment would be difficult without modiification. In the default configuration, the vehicle uses a single front facing camera as observation and uses continuous control parameters for driving the vehicle. For more advanced state representations, modifications will be needed. The entire environment is defined in [lgsvl_env.py](/gym_lgsvl/envs/lgsvl_env.py).

### CONFIG
Some of the basic configuration are passed to the environment through `CONFIG`. `action_space` and `observation_space` definitions are required and are defined using `gym.spaces`.

The default `action_space` is:
```
"action_space" :
  spaces.Box(
    np.array([-1,-1]), 
    np.array([+1,+1,]),
    dtype=np.float32,
  ),
```
which defines a continuous action space defined as a 2-D array. The first element is the steering value and the second is braking/throttle (negative values are braking and positive are throttle).

The observation space is defined as a single camera image from the front camera using the Box space from gym:

```
"observation_space" : 
  spaces.Box(
      low=0,
      high=255,
      shape=(297, 528, 3),
      dtype=np.uint8
    ) # RGB image from front camera
```

The shape tuple specifies the image size. The simulator API will always send 1920x1080 images. If any other size is used to define the observation space, the camera images will be resized to the specified size before being passed on as an observation.

### Reward calculation

The environment also calculates a reward function based on the distance travelled by the ego vehicle and collisions. The reward is calculated in the `_calculate_reward()` function based on distance travelled. The collision penalty is added on at the end (if applicable) when the collision callback is invoked (`_on_collision`). The collision callback will also terminate the episode and start a new episode.

### Sensors

By default the ego vehicle will only use the front facing camera. The ego vehicle is setup using `_setup_ego()`. Sensors are also defined here. To define more sensors, grab the sensor from the `sensors` list and invoke its specific methods to save data. To collect observations, `_get_observations()` is called.


### NPC Behavior

The NPCs are defined in `_setup_npc`. The NPCs are spawned randomly around the ego vehicle spawn point and will follow the lanes and traffic rules to move around.


## Copyright and License

Copyright (c) 2018 LG Electronics, Inc.

This software contains code licensed as described in LICENSE.