# WPF application for Kinect2#

This application uses RGB input from Kinect Sensor as background and displays tracked bodies skelton & faces upto 6 people & talking with Arduino over serial.

##### Language 
c#

##### Environment
Visual Studio Express 2013 for Desktop

##### Required
Microsoft Kinect SDK 2.0, [here]

#####Usage
* Build & Run application in Visual Studio
* Toggle buttons to reveal body skeletons & tracked faces
* Change Serial port line 153 at "MainWindow.xaml.cs" if needed. 
* parse & use serial input in Arduino

#####Note
some codes of Extension.cs were taken from [Vangos Pterneas]'s blog. 

#####Version
1.0.0

### Todo's
* Make buttons to stop/start trakcing process instead of hiding
* Display additional face info ontop of the image
* Make serial command more flexible

License
----

**Freel free to mess around.**

[here]:http://www.microsoft.com/en-us/download/details.aspx?id=44561
[Vangos Pterneas]:http://pterneas.com/2014/03/21/kinect-for-windows-version-2-hand-tracking/
