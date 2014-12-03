
using System;
using System.Collections.Generic;
using MonoDevelop.Ide;

using Mono.TextEditor.Highlighting;

namespace LuaBinding
{
	class LuaSyntaxMode : SyntaxMode
	{
		string GetSyntaxMode()
		{
			if (Document == null)
				return "LuaSyntaxMode.xml";
				
			var project = IdeApp.Workspace.GetProjectsContainingFile(Document.FileName) as LuaProject;

			if (project != null) {
				var config = project.DefaultConfiguration as LuaConfiguration;

				Console.WriteLine("Using {0} Lua highlighting", config.LangVersion);
				switch (config.LangVersion) {
					case LangVersion.Lua: // TODO: Make these use their own, maybe
					case LangVersion.Lua52:
					case LangVersion.Lua51:
					case LangVersion.LuaJIT:
					case LangVersion.Love:
					case LangVersion.Moai:
						return "LuaSyntaxMode.xml";
					case LangVersion.GarrysMod:
						return "GarrysModLuaSyntaxMode.xml";
				}
			}

			return "LuaSyntaxMode.xml";
		}

		public LuaSyntaxMode()
		{
			DocumentSet += delegate {
				if (Document == null)
					return;

				Document.FileNameChanged += delegate {
					var provider = new ResourceStreamProvider(typeof(LuaSyntaxMode).Assembly, GetSyntaxMode());

					var reader = provider.Open();
					var basemode = SyntaxMode.Read(reader);

					rules = new List<Rule>(basemode.Rules);
					keywords = new List<Keywords>(basemode.Keywords);
					spans = basemode.Spans;
					matches = basemode.Matches;
					prevMarker = basemode.PrevMarker;
					SemanticRules = basemode.SemanticRules;
					keywordTable = basemode.keywordTable;
					properties = basemode.Properties;
				};
			};
		}


	}
}

