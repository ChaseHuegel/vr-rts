using System;

namespace Swordfish
{
	//	A 2 dimensional coordinate
	[Serializable]
	public class Coord2D
	{
		public int x = 0;
		public int y = 0;

		public Coord2D(int _x, int _y)
		{
			x = _x;
			y = _y;
		}

		public static Coord2D fromVector3(UnityEngine.Vector3 _vector)
		{
			return new Coord2D((int)_vector.x, (int)_vector.z);
		}
	}
}