# Motion_Analysis_Kinect_Sensor
Used Microsoft Kinect to track human motion of shot-put throw in real time. 
This project is a software prototype capable of capturing, recording, analyzing and comparing movement patterns using three-dimensional vector angles. 
It combines both the Kinect motion capturing and biomechanics analysis and develops a shot put game improvement solution by comparison with Lanka(Olympics gold medalist).

Output of the Kinect code. 
On clicking start button, player will start the motion of the shot put throw. On completion, the joint points are stored in TEXT file

![Image of Output-Image](https://github.com/manpreetkaurassi/Motion_Analysis_Kinect_Sensor/blob/master/output_image.png)

Algorithm for which coordinates are saved to a TEXT file

![Image of Workflow-Data-Collection](https://github.com/manpreetkaurassi/Motion_Analysis_Kinect_Sensor/blob/master/workflow_data_collection.png)

The overall steps for carrying out the entire process are explained below :

1. Shot put player is standing at a distance of 4 meters from the Kinect, which is placed at height of 1.5 meters from the ground level.
2. The whole skeleton of the player is visible on the screen without distortion of any part.
3. Click on start button on the screen by any person other than the player (need not be the coach).
4. Kinect captures the 3D coordinates at different trigger points and stores them in a text file.
5. Code to carry out data analysis of the text file is executed, which generates an output screen depicting the calculated angles and time durations.
6. Each player throws a set of 15 throws of shot put.
7. The average values of the angles and time duration of all the throws is calculated and a final graph is plotted along with the values of Lanka(2000).
8. This graph helps the player realize the erroneous stage which would help him improve his performance. 
