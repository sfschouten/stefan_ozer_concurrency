using System.Diagnostics;
using OpenTK.Graphics.OpenGL;
using System;

namespace GameOfLife
{
    class Game
    {
        static readonly int2 res = new int2(768, 768);

        // load the OpenCL program; this creates the OpenCL context
        static OpenCLProgram ocl = new OpenCLProgram("../../program.cl");

        // find the kernel named 'device_function' in the program
        OpenCLKernel kernel = new OpenCLKernel(ocl, "simulateAndDraw");

        RLEFile rle;
        OpenCLBuffer<uint> pattern1;
        OpenCLBuffer<uint> pattern2;
        
        OpenCLImage<int> image;

        bool tock = false;

        public Surface screen;

        float zoom = 2f/3f;

        Stopwatch timer = new Stopwatch();
        int generation = 0;

        // mouse handling: dragging functionality
        uint rleW, rleH;
        uint xoffset = 0, yoffset = 0;
        bool lastLButtonState = false;
        int dragXStart, dragYStart, offsetXStart, offsetYStart;
        float lastWheel;
        public void SetMouseState(int x, int y, bool pressed, float wheel)
        {
            float dWheel = lastWheel - wheel;
            zoom += 0.01f * dWheel;
            if (zoom > 1)
                zoom = 1f;

            lastWheel = wheel;


            if (pressed)
            {
                if (lastLButtonState)
                {
                    int deltax = x - dragXStart, deltay = y - dragYStart;
                    xoffset = (uint)Math.Min(rleW * 32 - screen.width, Math.Max(0, offsetXStart - deltax));
                    yoffset = (uint)Math.Min(rleH - screen.height, Math.Max(0, offsetYStart - deltay));
                }
                else
                {
                    dragXStart = x;
                    dragYStart = y;
                    offsetXStart = (int)xoffset;
                    offsetYStart = (int)yoffset;
                    lastLButtonState = true;
                }
            }
            else lastLButtonState = false;
        }

        public void Init()
        {
            rle = new RLEFile("../../data/turing_js_r.rle");
            rleW = rle.W;
            rleH = rle.H;
            //rle = new RLEFile("../../data/metapixel-galaxy.rle");
            pattern1 = new OpenCLBuffer<uint>(ocl, (int)(rle.W * rle.H));
            pattern1.CopyToDevice();
            pattern2 = rle.ToCLBuffer(ocl);
            pattern2.CopyToDevice();

            //create an OpenGL texture to which OpenCL can send data
            image = new OpenCLImage<int>(ocl, res.x, res.y);
            //image = new OpenCLImage<int>(ocl, (int)rleW, (int)rleH);
            
        }

        public void Tick()
        {
            timer.Restart();
            GL.Finish();
            
            screen.Clear(0);
            
            //Set image argument
            kernel.SetArgument(0, image);

            if (!tock)//tick
            {
                kernel.SetArgument(1, pattern1);
                kernel.SetArgument(2, pattern2);
            }
            else      //tock
            {
                kernel.SetArgument(1, pattern2);
                kernel.SetArgument(2, pattern1);
            }
            
            kernel.SetArgument(3, rle.W );
            kernel.SetArgument(4, rle.H );
            kernel.SetArgument(5, res);
            kernel.SetArgument(6, xoffset);
            kernel.SetArgument(7, yoffset);
            kernel.SetArgument(8, zoom);

            // execute kernel
            long[] workSize = { rle.W + (4 - rle.W % 4),
                                rle.H + (4 - rle.H % 4)     };
            long[] localSize = { 4, 4 };

            // Use OpenCL to fill an OpenGL texture; this will be used in the
            // Render method to draw a screen filling quad.
            // lock the OpenGL texture for use by OpenCL
            kernel.LockOpenGLObject(image.texBuffer);
            // execute the kernel
            kernel.Execute(workSize, localSize);
            // unlock the OpenGL texture so it can be used for drawing a quad
            kernel.UnlockOpenGLObject(image.texBuffer);

            tock = !tock;
            Console.WriteLine("generation " + generation++ + ": " + timer.ElapsedMilliseconds + "ms");
        }

        public void Render()
        {
            // use OpenGL to draw a quad using the texture that was filled by OpenCL
            GL.LoadIdentity();
            GL.BindTexture(TextureTarget.Texture2D, image.OpenGLTextureID);
            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(0.0f, zoom); GL.Vertex2(-1.0f, -1.0f);
            GL.TexCoord2(zoom, zoom); GL.Vertex2(1.0f, -1.0f);
            GL.TexCoord2(zoom, 0.0f); GL.Vertex2(1.0f, 1.0f);
            GL.TexCoord2(0.0f, 0.0f); GL.Vertex2(-1.0f, 1.0f);
            GL.End();
        }
    }
}