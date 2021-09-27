[![Made with Unity](https://img.shields.io/badge/Made%20with-Unity-57b9d3.svg?style=flat&logo=unity)](https://unity3d.com)


# SkinFlapSimulation
This is a Unity based MPM simulation. MPM stands for Material Point Method, which is an algorithm that can simulate continuum materials like solids and liquids. In this case, it is used to simulate skin deformation and cutting, for applications in skin surgery. For more information on MPM, check [Wikipedia](https://www.wikiwand.com/en/Material_point_method). 

So far this program can simulate up to 100,000 particles at around 40fps by taking advantage of the Unity Job System (for multithreading) and Burst Compiler (for efficient compiling). There is a laser that can cut into the material, and you can make the laser follow a bezier curve by editing the bezier curve handles. Here are a few gifs showing operation.

![image](https://user-images.githubusercontent.com/32085355/134945284-0771da93-aa70-4c7a-97b5-9ae2afe78d63.png)


![image](https://user-images.githubusercontent.com/32085355/134945163-d2789e6c-62b2-4fee-9aa8-62b3653b1c44.png)




## Usage & Installation
This simulation runs on Unity 2020.3.14f1. It probably works on older versions and newer versions, but we can't guarantee that it'll work.

Begin by installing the project's unity version. Other versions may or may not work.

If not already installed, install [git](https://git-scm.com/).

Clone the repository to your local machine
```
git clone https://github.com/EsteBran/SkinFlapSimulation.git
```
Move into the simulation directory and checkout the 3D branch

```
cd SkinFlapSimulation
git checkout 3D
```
Open Unity Hub, and click on ADD. Navigate to where you cloned the SkinFlapSimulation repo and click "Select Folder"
![image](https://user-images.githubusercontent.com/15898988/131772190-01be6238-1f5d-4000-a60c-424fe7fd16eb.png)
![image](https://user-images.githubusercontent.com/15898988/131772224-c79fb23d-2508-4263-b46b-30ac90df032f.png)


When you first open the project, you'll have a random "New Scene" open in the editor. Navigate to the Assets folder and open "SampleScene" (not a very good name, we know) and open the scene by double clicking on it.
![image](https://user-images.githubusercontent.com/15898988/131772377-8c19aff6-284b-46e1-9b68-62cb468f238c.png)

To run the simulation, click on the play button at the top of the scene window. 
![image](https://user-images.githubusercontent.com/15898988/131772453-c29725d0-8f31-4067-a893-d2058504ebde.png)

### Controls
Q: Toggles camera control on and off

When camera is enabled: Mouse to look, WASD to move, SPACE to move vertically up, LShift to move vertically down.

Since automatic laser movement is not yet implemented, move the laser by maniupalting the position values in the inspector window.
![laserCut](https://user-images.githubusercontent.com/15898988/131772926-dbe607ff-c949-45a7-a9e7-45c9430b53aa.gif)


## To Do

- [ ] Refactor program into a more modular version in order to be able to add more features
- - [x] Make it use 3D coordinates instead of 2D
- [ ] Interacting between mesh and particles
- [x] Be able to import custom objects/meshes (obj files)
- [ ] Maybe using unity particle system
- [x] Move simulation into 3D (might have to start with 3D depending on what we want to do)
- [ ] Use shaders to parallelize simulation
- [x] Cut with laser and then maybe knife
- [ ] Figure out global position of particle
- [x] Cut objects into smaller objects and have them interact with one another real time 
- [ ] Mouse interaction

- [x] Better mouse interaction or cut with laser?
- [ ] Use knife to cut (probably use particles) using predetermined paths?
- - [ ] Probably need to create a system that simulates multiple objects
- [x] Try it out in 3D (have an idea of how much work we need to do)
- [ ] Refactor program into something more modular
