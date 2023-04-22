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

Paper: https://github.com/Seibaah/Fracture/blob/main/Real-Time%20Fracture%20in%20Unity.pdf
Video: https://www.youtube.com/watch?v=HzDzOsPxKlg&t=1s

## How to open the project?

To run the project please install Unity 2023.3.15f1 from the following link or the Unity Hub.
https://unity.com/releases/editor/whats-new/2021.3.15

:warning: **ONLY USE UNITY 2023.3.15 TO OPEN THE PROJECT. USING A NEWER VERSION MIGHT WORK BUT IT IS NOT GUARANTEED. USING AN OLDER VERSION IS ALMOST GUARANTEED TO FAIL**

## How to open a test scene?

The tests are in the Scene folder. To open it simply double click the scene file. 

![](https://github.com/Seibaah/Fracture/blob/main/Gifs/how_to_open_tests.gif)

## How to run a scene?

Once the scene is opened, press the Play button on top. All the tests shoot a gun 3x automatically to demonstrate the fracture. 

:warning: **BEWARE! THE LARGE MESH TEST IS VERY INTENSIVE AND TAKES LONGER THAN THE REST TO LOAD**

![](https://github.com/Seibaah/Fracture/blob/main/Gifs/how_to_run_test.gif)

## How to change the simulation parameters?

Each scene has a global and instance parameter objects. To change the material properties simply change the value in the editor andd run the scene.

![](https://github.com/Seibaah/Fracture/blob/main/Gifs/how_to_use_change_sim_params.gif)

## How do I control where the projectiles are shot?

There are 2 ways you can control the gun in the project:

1) You can load the Sandbox scene. This scene is already set up so you can run and play with it however you want. To shoot a projectile you use left click. 

2) To use the gun in any other scene you must find the Gun game object in the hierarchy and set automode to false. Then you can use left click to shoot.

![](https://github.com/Seibaah/Fracture/blob/main/Gifs/how_to_use_sandbox_test.gif)
