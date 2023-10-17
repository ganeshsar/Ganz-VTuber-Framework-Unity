# Ganz VTubing Framework

## Overview
This project aims to provide a general framework that intermediate VTubers/programmers could use to build a custom VTuber avatar solution off of. It uses a Python backend for all of the face detection aspects, then pipes over the data to Unity for finetuning and displaying the avatar. <br><br>
This means that you have the entire Unity pipeline at your disposal for VTubing and the simplicity of a Python backend as well. Currently, this project is powered by Google MediaPipe Face and requires only 1 RGB Webcam to use and no fancy GPU. Additional features may be added/developed upon request.<br><br>
![image showing waving](http://ganeshsaraswat.ca/InternetImages/facetracking.gif)


## Installation
1. Install Python and Unity (2021.3.17f1 was used, but any version close to that should be fine).
2. pip install mediapipe
3. Clone/download this repository.
4. Run main.py using Python.
5. Run the Unity project (SampleScene.scene)
6. Your avatar will be automatically calibrated upon playing Unity.

### Limitations
* I do not have the rights to distribute the 3D model for demoing purposes. You will need to manually [add it](https://github.com/hinzka/52blendshapes-for-VRoid-face). <br>
* For Programmers: Expect SWEEPING CHANGES to the architecture and code base going forward (currently not in a stable state). If you're just doing some experiments/learn this should be no problem, but I would not currently recommend using the present system for a long term project just yet.

If you are interested in commissioning an artist to create a custom vroid 3d model with 52 blendshapes, consider donating to my [ko-fi](https://ko-fi.com/ganthefan) (end product will be embedded in this project and open source for the community).

### Notes:
* See global_vars.py for some basic configuration options to speed up/improve precision of the detection.
* See AvatarFace script in the Unity inspector for configuration options (stylized behavior vs realistic, etc.)
* Good natural lighting goes a long way to aid face detection.
* This project aims primarily to create an appealing tracking result over an accurate one.
