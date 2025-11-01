# game531-assignments
Assignment 3 - This program was made using OpenTK. It makes it easier to use OpenGL for the graphics. The cube is built from 8 unique points (vertices) in 3D space. Instead of repeating points I use an index buffer to connect them to form the faces of the cube. The program uses three types of transformations: the model matrix rotates and moves the cube, the view matrix acts like a camera to position the scene, and the projection matrix gives perspective so the cube looks 3D. The program also includes rotation. The cube spins automatically on its own, and you can click and drag with the mouse to rotate it manually.


Midterm Project - I created a simple 3D interactive solar system built in C# with OpenTK, stbImageSharp and System.Drawing.Common. for the midterm project. 

Gameplay Instructions:

W / S – Move forward or backward
A / D – Move left or right
Space - Move up
Mouse movement – Look around
Esc - Gives you cursor visibility so you can either move the solar system by clicking and dragging or exiting the game if you wish

Some features: 

Textured and rotating planets using UV-mapped spheres (Mesh.cs)
Camera system with smooth movement (Camera.cs)
Shader system for custom vertex and fragment shaders (Shader.cs)
Texture loading with STBImageSharp (Texture.cs)
Real-time transformations and rendering using OpenGL
Basic lighting and perspective

--------- How to Build and Run ---------
Have .NET 6.0 SDK or higher
Use the following NuGet Packages:
OpenTK (Windowing and OpenGL)
Use stbImageSharp
System.Drawing.Common for some texture handling
----------------------------------------

Credits: 
https://www.solarsystemscope.com/textures/ (For textures used for the planets, sun, and space)
https://www.youtube.com/watch?v=n8t7nvHCqek&t=62s (Help with doing textures)
