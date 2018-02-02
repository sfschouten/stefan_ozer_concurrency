uint get_bit(__global uint* p, uint pw, uint x, uint y)
{
	return (p[y * pw + (x >> 5)] >> (x & 31U)) & 1U;
}

__kernel void simulateAndDraw(write_only image2d_t a, __global uint* ptrn, __global uint* prev, uint pw, uint ph, int2 res, uint xoffset, uint yoffset, float zoom)
{
	uint w = pw * 32;

	uint idx = get_global_id(0);
	uint idy = get_global_id(1);
	
	if (idx < pw && idy < ph)
	{
		uint v = 0;
		for (int i = 0; i < 32; i++)
		{
			uint x = idx * 32 + i, y = idy;

			bool b = false;
			if (x >= 1 && x < w - 1 && y >= 1 && y < ph - 1)
			{
				uint n = get_bit(prev, pw, x - 1, y - 1)
					+ get_bit(prev, pw, x, y - 1)
					+ get_bit(prev, pw, x + 1, y - 1)
					+ get_bit(prev, pw, x - 1, y)
					+ get_bit(prev, pw, x + 1, y)
					+ get_bit(prev, pw, x - 1, y + 1)
					+ get_bit(prev, pw, x, y + 1)
					+ get_bit(prev, pw, x + 1, y + 1);

				if ((get_bit(prev, pw, x, y) && n == 2) || n == 3)
				{
					b = true;
					v |= (1U << i);
				}
			}

			//Draw
			if (x - xoffset < zoom * res.x && y - yoffset < zoom * res.y)
			{
				float3 col = (float3)(0.f, 0.f, 0.f);
				if (b)
					col = (float3)(16.f, 16.f, 16.f);

				int2 pos = (int2)(x - xoffset, y - yoffset);
				write_imagef(a, pos, (float4)(col * (1.0f / 16.0f), 1.0f));
			}
		}

		ptrn[idx + pw * idy] = v;
	}
}