using System;
using System.IO;

namespace GameOfLife
{
    class RLEFile
    {
        uint[] pattern;
        uint pw, ph; // note: pw is in uints; width in bits is 32 this value.

        public uint W { get { return pw; } }
        public uint H { get { return ph; } }

        public RLEFile(string path)
        {
            StreamReader sr = new StreamReader("../../data/turing_js_r.rle");
            uint state = 0, n = 0, x = 0, y = 0;
            while (true)
            {
                String line = sr.ReadLine();
                if (line == null) break; // end of file
                int pos = 0;
                if (line[pos] == '#') continue; /* comment line */
                else if (line[pos] == 'x') // header
                {
                    String[] sub = line.Split(new char[] { '=', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    pw = (UInt32.Parse(sub[1]) + 31) / 32;
                    ph = UInt32.Parse(sub[3]);
                    pattern = new uint[pw * ph];
                    //second = new uint[pw * ph];
                }
                else while (pos < line.Length)
                    {
                        Char c = line[pos++];
                        if (state == 0) if (c < '0' || c > '9') { state = 1; n = Math.Max(n, 1); } else n = (uint)(n * 10 + (c - '0'));
                        if (state == 1) // expect other character
                        {
                            if (c == '$') { y += n; x = 0; } // newline
                            else if (c == 'o') for (int i = 0; i < n; i++) BitSet(x++, y); else if (c == 'b') x += n;
                            state = n = 0;
                        }
                    }
            }
            // swap buffers
            //for (int i = 0; i < pw * ph; i++) second[i] = pattern[i];

            /*
            StreamReader sr = new StreamReader(path);
            uint state = 0, n = 0, x = 0, y = 0;
            while (true)
            {
                String line = sr.ReadLine();
                if (line == null)
                    break; // end of file

                int pos = 0;
                if (line[pos] == '#')
                    continue; // comment line
                else if (line[pos] == 'x') // header
                {
                    String[] sub = line.Split(new char[] { '=', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    pw = (UInt32.Parse(sub[1]) + 31) / 32;
                    ph = UInt32.Parse(sub[3]);
                    pattern = new uint[pw * ph];
                }
                else while (pos < line.Length)
                {
                    Char c = line[pos++];
                    if (state == 0)
                    {
                        if (c < '0' || c > '9')
                        {
                            state = 1;
                            n = Math.Max(n, 1);
                        }
                        else
                        {
                            n = (uint)(n * 10 + (c - '0'));
                        }
                    }

                    if (state == 1) // expect other character
                    {
                        if (c == '$')
                        {
                            y += n;
                            x = 0;  // newline
                        }
                        else if (c == 'o')
                        {
                            for (int i = 0; i < n; i++)
                                BitSet(x++, y);
                        }
                        else if (c == 'b')
                            x += n;

                        state = n = 0;
                    }
                }
            }
                */
        }

        void BitSet(uint x, uint y)
        {
            pattern[y * pw + (x >> 5)] |= 1U << (int)(x & 31);
        }

        public OpenCLBuffer<uint> ToCLBuffer(OpenCLProgram program)
        {
            OpenCLBuffer<uint> buffer = new OpenCLBuffer<uint>(program, pattern.Length);
            for (int i = 0; i < pattern.Length; i++)
                buffer[i] = pattern[i];

            return buffer;
        }
    }
}
