#define GLINTEROP

void bit_set(__global uint* p, uint pw, uint x, uint y)
{
	p[y * pw + (x >> 5)] |= (1U << (x & 31U));
}

uint get_bit(__global uint* p, uint pw, uint x, uint y)
{
	return (p[y * pw + (x >> 5)] >> (x & 31U)) & 1U;
}

#ifdef GLINTEROP
__kernel void simulateAndDraw(write_only image2d_t a, __global uint* ptrn, __global uint* prev, uint pw, uint ph, int2 res)
#else
__kernel void simulateAndDraw(__global int* a,	      __global uint* ptrn, __global uint* prev, uint pw, uint ph, int2 res)
#endif
{
	uint w = pw * 32;

	uint x = get_global_id(0);
	uint y = get_global_id(1);
	int i = x + w * y;
	


	//Simulate
	/*
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

		if ( ( get_bit(prev, pw, x, y) && n == 2 ) || n == 3 )
			bit_set(ptrn, pw, x, y);
	}*/
	bool b = false;
	if (get_bit(prev, pw, x, y))
	{
		b = true;
		bit_set(ptrn, pw, x, y);
	}

	//Draw
	if (x < res.x && y < res.y)
	{
		float3 col = (float3)(0.f, 0.f, 0.f);
		if (b)
			col = (float3)(16.f, 16.f, 16.f);

#ifdef GLINTEROP
		int2 pos = (int2)(x, y);
		write_imagef(a, pos, (float4)(col * (1.0f / 16.0f), 1.0f));
#else
		int r = (int)clamp(16.0f * col.x, 0.f, 255.f);
		int g = (int)clamp(16.0f * col.y, 0.f, 255.f);
		int b = (int)clamp(16.0f * col.z, 0.f, 255.f);
		a[i] = (r << 16) + (g << 8) + b;
#endif
	}
}