# handinput

Hand tracking and gesture recognition

## Develop Environment
* Windows 7 64bit
* Visual Studio 2012
* [Visual C++ Compiler November 2012 CTP] (http://www.microsoft.com/en-us/download/details.aspx?id=35515)
* NuGet 2.7 ([with package restore during build enabled] (http://docs.nuget.org/docs/workflows/using-nuget-without-committing-packages))

## Dependencies
* [Kinect SDK 1.7] (http://www.microsoft.com/en-us/kinectforwindowsdev/Downloads.aspx)
* [Emgu] (http://sourceforge.net/projects/emgucv/files/emgucv/2.4.9-alpha/libemgucv-windows-universal-gpu-2.4.9.1847.zip/download) (version: Windows, universal, GPU, 2.4.9.1847)
  * Using x64 version dlls, which requires a rebuild of the source by changing the platform to x64 instead of Any CPU.
* [Modified Kinect Toolbox] (https://github.com/uShadow/kinect-toolbox)
* Microsoft.Kinect.Toolkit (placed in the same directory as this solution).

## Run
* Use Visual Studio to build the solution.
* GestuerViewer.exe in the GestureViewer project is the main interface to run the program. 

## Modules
* GestureViewer: UI Interface.
* Util: reusable utility functions.



