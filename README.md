# Ganz VTuber Framework

## Overview
This project aims to provide a general framework that intermediate VTubers could use to build a custom VTuber avatar solution off of. It uses a Python backend for all of the face detection aspects, then pipes over the data to Unity for finetuning and displaying the avatar. It was a bit of a rush job, but I may continue work on it depending on things...<br><br>
This means that you have the entire Unity pipeline at your disposal for VTubing and the simplicity of a Python backend as well. Currently, this project is powered by Google MediaPipe Face and requires only 1 RGB Webcam to use and no fancy GPU. Additional features may be added/developed upon request.<br><br>
![image showing waving](http://ganeshsaraswat.ca/InternetImages/facetracking.gif)


## Installation
1. Install Python and Unity (2021.3.17f1 was used, but any version close to that should be fine).
2. pip install mediapipe
3. Clone/download this repository.
4. Run main.py using Python.
5. Run the Unity project (SampleScene.scene)
6. Your avatar will be automatically calibrated upon playing Unity.

Demo is available (.exe for testing without installing libraries). This is my first time trying this method, so let me know if you have problems.

## Controls
* Press TAB to swap backgrounds.
* Press SHIFT to swap characters. 
* Press ESC to quit.

### Limitations
* I do not have the rights to distribute the 3D model source. You will need to manually [add it](https://github.com/hinzka/52blendshapes-for-VRoid-face) to the project.<br>

If you are interested in commissioning an artist to create a custom vroid 3d model with 52 blendshapes, consider donating to my [ko-fi](https://ko-fi.com/ganthefan) (end product will be embedded in this project and open source for the community).

### Notes:
* See global_vars.py for some basic configuration options particularly in relation to the camera.
* See AvatarFace script in the Unity inspector for configuration options (stylized behavior vs realistic, etc.)
* Good natural lighting goes a long way to aid face detection (reference the Face Tracking window).
* This project aims primarily to create an appealing tracking result over an accurate one.
