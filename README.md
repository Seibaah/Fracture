# Real-Time Fracture in Unity

![](https://github.com/Seibaah/Fracture/blob/main/Gifs/fracture.jpg)

## Abstract

This paper examines a method to compute object fractures in real time. The
technique uses stress and separation tensors computed over finite elements
to determine whether a crack should occur and how it should break the
mesh. The goal is to present the math in a digestible way such that anyone
reading can understand the principles and create their own version afterward.
We then present and test a working implementation in Unity. Finally, we
evaluate its performance and hint at how it could be further improved

## How to open the project?

To run the project please install Unity 2023.3.15f1 from the following link or the Unity Hub.
https://unity.com/releases/editor/whats-new/2021.3.15

Open the project using this version only to avoid compatibility issues.

## How to open a test scene?

![](https://github.com/Seibaah/Fracture/blob/main/Gifs/how_to_open_tests.gif)

The tests are in the Scene folder. To open it simply double click the scene file. 

## How to run a scene?

![](https://github.com/Seibaah/Fracture/blob/main/Gifs/how_to_run_test.gif)

Once the scene is opened, press the Play button on top. All the tests shoot a gun 3x automatically to demonstrate the fracture.

## How to change the simulation parameters?

![](https://github.com/Seibaah/Fracture/blob/main/Gifs/how_to_use_change_sim_params.gif)

Each scene has a global and instance parameter objects. To change the material properties simply change the value in the editor andd run the scene.

## How do I control where the projectiles are shot?

![](https://github.com/Seibaah/Fracture/blob/main/Gifs/how_to_use_sandbox_test.gif)

There are 2 ways you can control the gun in the project.

1) In any scene, you can find the Gun game object in the hierarchy and set automode to false.

2) You can load the Sandbox scene. This scene is already set up so you can run and play with it however you want. 
