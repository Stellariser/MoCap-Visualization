# Comparison of motion data in regard to the perception of spatiotemporal distances in VR
In this repository is a Unity 2021.3.3f1. project created to build a VR application for experimentation in the course DM2799 Advanced Projectcourse in Interactive Media Technology. The application was built for the Oculus Quest 2 headset in November-December 2022. 

[![Video](https://user-images.githubusercontent.com/40071882/207350809-9c5ccbda-de19-49ef-a606-82cd614f951e.png)](https://youtu.be/ix-346xUZFo
)
## Abstract
Several previous studies on depth perception have focused on the perception of ego-centric distances in extrapersonal space; however, perception in peripersonal space has only been addressed in part, with contradicting results. This paper explores the accuracy of spatiotemporal distance perception during surgical procedure virtual reality (VR). Using surgical motion capture data a user research was conducted to investigate the differences in depth perception of raw motion capture data, average filtered motion capture data, and synthetic data. Participants used a VR headset to view a catheter entering and exiting a transparent plane, and their impressions were examined via questionnaires during and after the study. This study revealed that there was not a significant difference in perception between the three motion capture data types.

## Instructions
Experimenter:
- choose category by looking at the according button and press the X-button on the left controller
- press X-button if participant is ready for experiment
- press Y-button to save the experiment data in a file at the end of the experiment

Participant:
- press A-button when catheter punctures plane to record distance to plane

The application needs to be restarted for each participant. 
The data is saved on the HMD at: `\Quest 2\Internal shared storage\Android\data\com.DefaultCompany.MoCapDemo\files`.


## Run the project in Unity
1. Install Unity 2021.3.3f1. 
2. Open the main scene, which can be found in /Assets/Scenes/Main
3. Press play!
To run in from Unity on the Quest 2 connect your Quest with the PC and enable Quest Link.

## Build the project from Unity for the Quest 2
1. Open the Unity project as described above
2. Go to Build Settings
3. Select Android Platform
4. Connect the Quest to the computer
5. Press Build and Run
6. After a successful build the application will start on the Quest

## Installation apk file directly Oculus Quest 2
The .apk file can be found [here](https://github.com/LariWa/MoCap-Visualization/releases/tag/final). This file has to be installed on the Oculus Quest 2 (e.g. like [this](https://headjack.io/knowledge-base/how-to-easily-sideload-a-vr-app-to-oculus-quest-2/))


## File structure
important files of the project can be found in the Assets folder:
- /Animations
  - contains the animation sequences for the synthesized motion category and the tutorial
  - AnimationScript.cs: handles the animations (looping and stopping)
- /MoCapData
  - contains recored motion capture data, we use catheter008.txt for the experiment
- /Scripts
  - BlurController.cs: blurs view of participant, if they move
  - DataManager.cs: records the data of the experiment and save it to a file (distance from plane to catheter tip, when the button is pressed)
  - Simulator: handles movement of the catheter through the raw motion capture data, filtered motion capture data or synthesized data
