using System.Collections.Generic;

namespace LuaBinding
{
	public static class LuaParser
	{
		public static string[] GetLocals(string lua, int position)
		{
			// hacky, just parse backwards, through strings, and stuff, check for inside strings in here, in future
			var ret = new List<string>();



			return ret.ToArray();
		}
	}
}

