# handinput

Real-time hand tracking and gesture recognition system based on PhD thesis: [Real-time Continuous Gesture Recognition for Natural
Multimodal Interaction](http://groups.csail.mit.edu/mug/pubs/Yin2014Thesis.pdf).

## Dependencies
### To run
* Windows 7 64bit
* .NET 4.5.1
* [Kinect SDK 1.8] (http://www.microsoft.com/en-us/kinectforwindowsdev/Downloads.aspx)
* Matlab 2013b 64bit 
  * Define $MATLAB evironment variable pointing to the main folder of the installation, e.g. C:\Program Files\MATLAB\R2013b.
  * Make sure `%MATLAB%\bin\win64` is in `Path`.
* [Gesture recognition training code in Matlab] (https://github.com/uShadow/gesture-recog)
  * Make sure these folders are in Matlab path. 

### To compile
* Use Visual Studio 2012 to build the solution
* [Visual C++ Compiler November 2012 CTP] (http://www.microsoft.com/en-us/download/details.aspx?id=35515)
* NuGet 2.7 ([with package restore during build enabled] (http://docs.nuget.org/docs/workflows/using-nuget-without-committing-packages))
* [Emgu] (http://sourceforge.net/projects/emgucv/files/emgucv/2.4.9-alpha/libemgucv-windows-universal-gpu-2.4.9.1847.zip/download) (version: Windows, universal, GPU, 2.4.9.1847)
  * Using x64 version dlls, which requires a rebuild of the source by changing the platform to x64 instead of Any CPU.
  * Put the Emgu folder in `lib`.
  * Need to copy opencv_*.dll files to the build output folder.
* [Modified Kinect Toolbox] (https://github.com/uShadow/kinect-toolbox)
* [Eigen 3.2.0] (http://eigen.tuxfamily.org/index.php?title=Main_Page) for linear algebra.

## Run
GesturesViewer.exe in the GesturesViewer project is the main interface to run the program. Once the program starts, the "Keys" panel shows the shortcut keys for certain actions. The most important keys are:

* S: Start the kinect, no gesture recognition.
* T: Start tracking and gesture recognition. The gestures are defined in [gesture_def.txt](https://github.com/uShadow/handinput/blob/master/GesturesViewer/Data/gesture_def.txt). See the [illustraion](https://groups.csail.mit.edu/mug/projects/gesture_kinect/images/gesture.png) of how to do these gestures.

To improve the accuracy of gesture recognition, you need to train your own model.

1. Record training examples 
  1. Click "Capture Gesture" button.
  2. Follow the prompt to give training gesture examples. The gesture raw data will be saved in the {data_dir}/PID-{user_pid}/{time} directory. data_dir s specified in the GesturesViewer/App.config file.
  3. In the end, the program will process and train a new model using all the data recorded in the data_dir directory.
2. Press "T" to start tracking and gesture recognition  

### How to interpret the recognition result
The gesture tracking and recognition result outputs the follow result in a JSON string for each frame:
{ eventType: \<type of geseture event: StartNucleus|StopNucleus\>, gesture: \<name of the gesture\>, phase: \<PreStroke|Nucleus|PostStroke\>, rightX: \<x coordinate of right hand\>, rightY: \<y coordinate of right hand\>} 

## Modules
* GesturesViewer: UI Interface for recording geseture training examples and viewing debug information.
* Util: reusable utility functions.



