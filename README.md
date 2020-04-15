# Augmented-Reality-for-Structural-Visualization
CS Capstone 2019-2020

## Description
SVAR is a system that takes in sensor data and produces real time scientific visualization for structural engineering.
This tool will be developed to display through Microsoft’s Augmented Reality (AR) device, the HoloLens. Augmented
reality will allow for data visualization on top of physical experiments.

## Hardware
To run this software you will need a HoloLens Gen 1, and a computer that can run the mixed reality toolkit
## Setup
The setup of this repo is long and complicated. This is readme contains a **very** stripped down version of the development enviroment setup. Here a link to the pdf we used developed to setup the development enviroment
[Link to Setup pdf](https://drive.google.com/a/oregonstate.edu/file/d/1oUkUq7KiI3Z6mWifp752n-0yzJQIxaVc/view?usp=sharing)

### Required componets
1. [mixed reality toolkit](https://docs.microsoft.com/en-us/windows/mixed-reality/install-the-tools)
2. [Visual Studio 2019]( https://visualstudio.microsoft.com/thank-you-downloading-visual-studio/?sku=Community&rel=16)
3. Windows 10 
4. Windows 10 SDK 10.0.18362.0 or newer
5. [Unity 2019.2.8f1]( https://unity3d.com/unity/whats-new/2019.2.8 )
6. Unity Universal Windows Platform addin
7. Mixed reality portal

### Installing Visual Studios
Inorder to run the mixed reality toolkit it is vital to include the Windows 10 SDK 10.0.18362.0. c++ support, .net desktop support, UWP support, Unity support, .nety cross platform development. in the individual components you need c++ atl for latex 142 build tools(x86 & x64)

### Installing the mixed reality toolkit
Instructions from micosoft can bee seen here [MRTK setup instructions](https://microsoft.github.io/MixedRealityToolkit-Unity/Documentation/GettingStartedWithTheMRTK.html#import-mrtk-packages-into-your-unity-project)

### Setting up Unity
While many of the setting are set up in this repo. Some are specific to the unity installation. Verify that all of the settings under Chaptor 3 from this link is set up correctly [MXR in Unity setup](https://docs.microsoft.com/en-us/windows/mixed-reality/holograms-100)

### Setting correct build
Go back to File/Build Settings and change the target device to HoloLens, the architecture to x86 and click the Build button. There will be a window that asks where you build the project. I created a folder titled SVAR and pointed it there. I unselected everything but my minimal build.

### Building for visual studio
Alright, 2 things. Along the top change the drop down menu to x86 and to HoloLens Emulator 10.xxx. After that, right click on the project and select manage NuGet packages..
Search for holographic and find Microsoft.Holographic.Remoting select SVAR and the other one and install it. Make sure you select version 1.0.0. Version 2.x.x is for HoloLens2.

### Running
At this point, the framework for remoting is in place but the code is not. Visual Studio will launch the emulator and display. To deploy the app to the HoloLens I had to install Windows Mixed Reality Portal and make sure it was running. 
