# game531-assignments
Assignment 3 - This program was made using OpenTK. It makes it easier to use OpenGL for the graphics. The cube is built from 8 unique points (vertices) in 3D space. Instead of repeating points I use an index buffer to connect them to form the faces of the cube. The program uses three types of transformations: the model matrix rotates and moves the cube, the view matrix acts like a camera to position the scene, and the projection matrix gives perspective so the cube looks 3D. The program also includes rotation. The cube spins automatically on its own, and you can click and drag with the mouse to rotate it manually.


Midterm Project - I created a simple 3D interactive solar system built in C# with OpenTK, stbImageSharp and System.Drawing.Common. for the midterm project. 

Gameplay Instructions:

W / S – Move forward or backward
A / D – Move left or right
Space - Move 
E - To turn on and off the lighting
Mouse movement – Look around
Esc - Gives you cursor visibility so you can either move the solar system by clicking and dragging or exiting the game if you wish

Some features: 

Textured and rotating planets using UV-mapped spheres (Mesh.cs)
Camera system with smooth movement (Camera.cs)
Shader system for custom vertex and fragment shaders (Shader.cs)
Texture loading with STBImageSharp (Texture.cs)
Real-time transformations and rendering using OpenGL
Basic lighting and perspective

How to Build and Run: 
Have .NET 6.0 SDK or higher
Use the following NuGet Packages:
OpenTK (Windowing and OpenGL)
Use stbImageSharp
System.Drawing.Common for some texture handling


Credits: 
https://www.solarsystemscope.com/textures/ (For textures used for the planets, sun, and space)
https://www.youtube.com/watch?v=n8t7nvHCqek&t=62s (Help with doing textures)


Assignment 7 ----------------------------------------------------------------------------------------------------------
I added two new movement mechanics: Jumping and Running/Sprinting. Jumping uses simple physics (vertical velocity + gravity) with a grounded check to prevent double-jumping, and it plays a dedicated 6-frame jump strip. Sprinting is a run modifier triggered by holding Shift while moving left/right; it bumps horizontal speed and swaps to a faster 6-frame run animation. Controls: A/Left and D/Right to walk, Shift to run, Space to jump. The sprite renders pixel-perfect at 48×48 and mirrors horizontally when facing left (no UV hacks).

The animation controller is a tiny finite state machine with four states: Idle, Walk, Run, Jump. Transitions are input- and physics-driven: on ground, movement chooses Walk/Run; when speed falls below a small threshold it returns to Idle; pressing Space while grounded switches to Jump, and landing switches back to Walk/Run/Idle based on velocity. Frames advance only when the state is “active” (e.g., moving or airborne), so when input stops the animation holds the last visible frame, matching the base project behavior. Each state binds its own texture strip and sets the correct UV window for the current frame.

Main challenges were (1) asset loading and pathing—fixed by supporting both “next to the exe” and a /mnt/data fallback plus setting PNGs to Content/Copy Always; (2) OpenTK API quirks—switched to ClientSize and used positional args for CreateOrthographicOffCenter to avoid version mismatches; (3) compiler warnings—made enums public, exposed the LoadTexture helper, and null-forgave the character field created in OnLoad; and (4) animation bleed and mirroring—solved with Nearest filtering, ClampToEdge, and X-axis model scaling for left/right. The result is a clean, readable FSM that’s easy to extend (climb/crouch can drop in as new states).

Credits: 
https://craftpix.net/freebies/free-3-character-sprite-sheets-pixel-art/

https://github.com/mouraleonardo/SpriteGameOpenTk/tree/main

https://chatgpt.com/

AI was used during parts of my project to help troubleshoot issues with animation playback and smoothing. Specifically, I used AI to clarify why the character animation was jerking back between frames and to understand how to correctly manage frame timing so the walk and run cycles would look smooth. The coding and implementation decisions, including movement logic and animation state transitions, were completed by me. The AI support was limited to debugging guidance and explanation of why the animation was not displaying smoothly.

