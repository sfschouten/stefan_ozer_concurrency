using System.Diagnostics;
using OpenTK.Graphics.OpenGL;

namespace GameOfLife
{
    class Game
    {
        // when GLInterop is set to true, we render directly to an OpenGL texture
        const bool GLInterop = true;

        static readonly int2 res = new int2(512, 512);

        // load the OpenCL program; this creates the OpenCL context
        static OpenCLProgram ocl = new OpenCLProgram("../../program.cl");

        // find the kernel named 'device_function' in the program
        OpenCLKernel kernel = new OpenCLKernel(ocl, "simulateAndDraw");

        RLEFile rle;
        OpenCLBuffer<uint> pattern1;
        OpenCLBuffer<uint> pattern2;
        
        OpenCLBuffer<int> buffer;
        OpenCLImage<int> image;

        bool tock = false;

        public Surface screen;
        Stopwatch timer = new Stopwatch();

        public void Init()
        {
            rle = new RLEFile("../../data/turing_js_r.rle");
            //rle = new RLEFile("../../data/metapixel-galaxy.rle");
            pattern1 = new OpenCLBuffer<uint>(ocl, (int)(rle.W * rle.H));
            pattern1.CopyToDevice();
            pattern2 = rle.ToCLBuffer(ocl);
            pattern2.CopyToDevice();

            if (GLInterop)
                //create an OpenGL texture to which OpenCL can send data
                image = new OpenCLImage<int>(ocl, res.x, res.y);
            else
                //create a regular buffer; by default this resides on both the host and the device
                buffer = new OpenCLBuffer<int>(ocl, res.x * res.y);
        }

        public void Tick()
        {
            GL.Finish();
            
            screen.Clear(0);
            
            //Set image argument
            if (GLInterop)
                kernel.SetArgument(0, image);
            else
                kernel.SetArgument(0, buffer);

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
            
            kernel.SetArgument(3, rle.W);
            kernel.SetArgument(4, rle.H);
            kernel.SetArgument(5, res);

            // execute kernel
            long[] workSize = { rle.W + (32 - rle.W % 32),
                                rle.H + (4 - rle.H % 4)     };
            long[] localSize = { 32, 4 };
            if (GLInterop)
            {
                // INTEROP PATH:
                // Use OpenCL to fill an OpenGL texture; this will be used in the
                // Render method to draw a screen filling quad. This is the fastest
                // option, but interop may not be available on older systems.
                // lock the OpenGL texture for use by OpenCL
                kernel.LockOpenGLObject(image.texBuffer);
                // execute the kernel
                kernel.Execute(workSize, localSize);
                // unlock the OpenGL texture so it can be used for drawing a quad
                kernel.UnlockOpenGLObject(image.texBuffer);
            }
            else
            {
                // NO INTEROP PATH:
                // Use OpenCL to fill a C# pixel array, encapsulated in an
                // OpenCLBuffer<int> object (buffer). After filling the buffer, it
                // is copied to the screen surface, so the template code can show
                // it in the window.
                // execute the kernel
                kernel.Execute(workSize, localSize);
                // get the data from the device to the host
                buffer.CopyFromDevice();
                // plot pixels using the data on the host
                for (int y = 0; y < res.y; y++) for (int x = 0; x < res.x; x++)
                {
                    screen.pixels[x + y * res.x] = buffer[x + y * (int)rle.W];
                }
            }

            tock = !tock;
        }

        public void Render()
        {
            // use OpenGL to draw a quad using the texture that was filled by OpenCL
            if (GLInterop)
            {
                GL.LoadIdentity();
                GL.BindTexture(TextureTarget.Texture2D, image.OpenGLTextureID);
                GL.Begin(PrimitiveType.Quads);
                GL.TexCoord2(0.0f, 1.0f); GL.Vertex2(-1.0f, -1.0f);
                GL.TexCoord2(1.0f, 1.0f); GL.Vertex2(1.0f, -1.0f);
                GL.TexCoord2(1.0f, 0.0f); GL.Vertex2(1.0f, 1.0f);
                GL.TexCoord2(0.0f, 0.0f); GL.Vertex2(-1.0f, 1.0f);
                GL.End();
            }
        }
    }
}