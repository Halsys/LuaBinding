using System;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects;

namespace LuaBinding
{
	public enum LangVersion
	{
		Lua,
		Lua51,
		Lua52,
		LuaJIT,
		GarrysMod,
		Moai,
		Love
	}

	public class LuaConfiguration : ProjectConfiguration
	{
		[ItemProperty("MainFile", DefaultValue = "main.lua")]
		string _MainFile = "main.lua";
		[ItemProperty("InterpreterArgs")]
		string _InterpreterArgs = String.Empty;
		[ItemProperty("LangVersion", DefaultValue = LangVersion.Lua)]
		LangVersion _LangVersion = LangVersion.Lua;
		[ItemProperty("Launcher", DefaultValue = "")]
		string _Launcher = "";

		public LuaConfiguration()
		{
		}

		public LuaConfiguration(string name)
		{
			Name = name;
		}

		public string MainFile {
			get { return _MainFile; }
			set { _MainFile = value ?? String.Empty; }
		}

		public string InterpreterArguments {
			get { return _InterpreterArgs; }
			set { _InterpreterArgs = value ?? String.Empty; }
		}

		public LangVersion LangVersion {
			get { return _LangVersion; }
			set { _LangVersion = value; }
		}

		public string Launcher {
			get {
				return _Launcher;
			}
			set {
				_Launcher = value;
			}
		}


		public override void CopyFrom(ItemConfiguration conf)
		{
			var LuaConfig = conf as LuaConfiguration;
			if (LuaConfig == null)
				throw new ArgumentException("Not a Lua configuration", "conf");

			base.CopyFrom(conf);

			_MainFile = LuaConfig._MainFile;
			_InterpreterArgs = LuaConfig._InterpreterArgs;
			_LangVersion = LuaConfig._LangVersion;
			_Launcher = LuaConfig._Launcher;
		}
	}
}
