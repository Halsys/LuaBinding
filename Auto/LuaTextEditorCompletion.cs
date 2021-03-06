
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.CodeCompletion;

using ICSharpCode.NRefactory.Completion;
using System.Reflection;

namespace LuaBinding
{
	public class GlobalCompletionCategory : CompletionCategory
	{
		public GlobalCompletionCategory()
		{
			DisplayText = "Global";
		}

		public override int CompareTo(CompletionCategory other)
		{
			return (other.DisplayText == DisplayText) ? 1 : 0;
		}
	}

	public class LocalCompletionCategory : CompletionCategory
	{
		public LocalCompletionCategory()
		{
			DisplayText = "Local";
		}

		public override int CompareTo(CompletionCategory other)
		{
			return (other.DisplayText == DisplayText) ? 1 : 0;
		}
	}


	public class LuaParameterDataProvider : ParameterDataProvider
	{
		/// <summary>
		/// Turn things like "message [, level]" into
		/// message
		/// message, level
		/// </summary>
		/// <param name="input">Input.</param>
		public static List<string> Unpack(string input)
		{
			int depth = 0;
			string required = "";
			var optional = new List<Tuple<int, List<string>>>();

			int i = 0; 
			while (i < input.Length) {
				char x = input[i];

				if (x == '[') {
					int start = i;
					depth = 1;
					i++;

					while (i < input.Length && depth > 0) {
						if (input[i] == '[')
							depth++;
						if (input[i] == ']')
							depth--;
						i++;
					}
					i--;

					string child = input.Substring(start + 1, (i - (start + 1)));
					int pos = required.Length;
					optional.Add(new Tuple<int, List<string>>(pos, Unpack(child)));
				} else
					required += x;

				i++;
			}

			var ret = new List<string>();

			if (optional.Count == 0) {
				ret.Add(required);
				return ret;
			}

			var argument = new int[optional.Count];
			var argument_max = new int[optional.Count];

			for (i = 0; i < argument.Length; i++) // Reset this one
				argument[i] = 0;

			for (i = 0; i < argument_max.Length; i++)
				argument_max[i] = optional[i].Item2.Count;

			bool done = false;
			while (!done) {
				string itt = required;
				// Add an itteration
				for (i = argument.Length; i-- > 0;) // work backwards so the pointers are still fine
					if (argument[i] != 0) {
						int pos = optional[i].Item1;
						string str = optional[i].Item2[argument[i] - 1];
						itt = itt.Insert(pos, str);
					}

				// The generated sequence is in itt now, but we need to clean it up a bit...
				itt = itt.Trim(" \t,".ToCharArray());
				var split = new List<string>(itt.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries));

				for (i = split.Count; i-- > 0;)
					if (string.IsNullOrWhiteSpace(split[i]))
						split.RemoveAt(i);

				string comma = "";
				itt = "";
				foreach (string str in split) {
					itt += comma + str.Trim();
					comma = ", ";
				}

				ret.Add(itt);

				// Okay, now increase our base-mixed-numbery thing by one
				argument[0]++;

				for (i = 0; i < argument.Length; i++) {
					if (argument[i] > argument_max[i]) { // carry
						if (i + 1 >= argument.Length) { // no other place to carry
							done = true;
							break;
						}
						argument[i] = 0;
						argument[i + 1]++;
					}
				}
			}

			// add all the combinations of items

			return ret;
		}

		public override int Count {
			get { return Overloads.Count; }
		}

		readonly List<string> Overloads = null;
		readonly string FuncName, FuncArgs;

		public LuaParameterDataProvider(string funcname, string args)
			: base(1)
		{
			Overloads = Unpack(args);
			FuncName = funcname;
			FuncArgs = args;
		}

		public override bool AllowParameterList(int overload)
		{
			return overload < Count;
		}

		public override int GetParameterCount(int overload)
		{
			return Overloads[overload].Split(",".ToCharArray()).Length;
		}

		public override string GetParameterName(int overload, int paramIndex)
		{
			return Overloads[overload].Split(",".ToCharArray())[paramIndex].Trim();
		}

		static string GetHeading(int overload, string[] parameterDescription, int currentParameter)
		{
			return "HEADING";
		}

		static string GetDescription(int overload, int currentParameter)
		{
			return "DESCRIPT";
		}

		public override TooltipInformation CreateTooltipInformation(int overload, int currentParameter, bool smartWrap)
		{
			var info = new TooltipInformation();

			string[] args = Overloads[overload].Split(",".ToCharArray());
			string markup = "";
			string comma = "";
			int current = 1;
			foreach (string arg in args) {
				markup += current == currentParameter ? string.Format("{0}<b><i>{1}</i></b>", comma, arg) : string.Format("{0}{1}", comma, arg);

				comma = ", ";
				current++;
			}

			info.SignatureMarkup = FuncName + "(" + FuncArgs + ")";
			info.AddCategory("Parameters", string.Format("{0}( {1} )", FuncName, markup));

			return info;
			//return base.CreateTooltipInformation(overload, currentParameter, smartWrap);
		}
	}

	public class LuaTextEditorCompletion : CompletionTextEditorExtension
	{
		static bool ValidVarnameChar(char x)
		{
			return char.IsLetterOrDigit(x) || x == '_';
		}

		string[] Globals = { // TODO: Fill this in
			// Keywords
			"_G\tand",
			"_G\tbreak",
			"_G\tdo",
			"_G\telse",
			"_G\telseif",
			"_G\tend",
			"_G\tfalse",
			"_G\tfor",
			"_G\tfunction",
			"_G\tgoto",
			"_G\tif",
			"_G\tin",
			"_G\tlocal",
			"_G\tnil",
			"_G\tnot",
			"_G\tor",
			"_G\trepeat",
			"_G\treturn",
			"_G\tthen",
			"_G\ttrue",
			"_G\tuntil",
			"_G\twhile",
			// Globals
			"_G\tassert\t(value [, message])",
			"_G\tcollectgarbage\t(opt [, arg])",
			"_G\tdofile\t(filename)",
			"_G\terror\t(message [, level])",
			"_G\tgetmetatable\t(object)",
			"_G\tipairs(\t(table)",
			"_G\tload\t(func [, chunkname])",
			"_G\tloadfile\t([filename])",
			"_G\tloadstring\t(string [, chunkname])",
			"_G\tnext\t(table [, index])",
			"_G\tpairs\t(table)",
			"_G\tpcall\t(func, ...)",
			"_G\tprint\t(...)",
			"_G\trawequal\t(v1, v2)",
			"_G\trawget\t(table, index)",
			"_G\trawset\t(table, index, value)",
			"_G\tselect\t(index, ...)",
			"_G\tsetmetatable\t(table, metatable)",
			"_G\ttonumber\t(value [, base])",
			"_G\ttostring\t(value)",
			"_G\ttype\t(value)",
			"_G\tunpack\t(list [, i [, j]])",
			"_G\txpcall\t(func, err)",
			"_G\t_G\t{}",
			"_G\t_VERSION\t#",
			// coroutine libary
			"_G\tcoroutine\t{}",
			"coroutine\tcreate\t(func)",
			"coroutine\tresume\t(co [, val1, ...])",
			"coroutine\trunning\t()",
			"coroutine\tstatus\t(co)",
			"coroutine\twrap\t(func)",
			"coroutine\tyield\t(...)",
			// debug libary
			"_G\tdebug\t{}",
			"debug\tdebug\t()",
			"debug\tgethook\t([thread])",
			"debug\tgetinfo\t([thread,] function [, what])",
			"debug\tgetlocal\t([thread,] level, local)",
			"debug\tgetmetatable\t(object)",
			"debug\tgetregistry\t()",
			"debug\tgetupvalue\t(func, up)",
			"debug\tsethook\t([thread,] hook, mask [, count])",
			"debug\tsetlocal\t([thread,] level, local, value)",
			"debug\tsetmetatable\t(object, table)",
			"debug\tsetupvalue\t(func, up, value)",
			"debug\ttraceback\t([thread,] [message] [, level])",
			// io libary
			"_G\tio\t{}",
			"io\tclose\t([file])",
			"io\tflush\t()",
			"io\tinput\t([file])",
			"io\tlines\t([filename])",
			"io\topen\t(filename [, mode])",
			"io\toutput\t([file])",
			"io\tpopen\t(prog [, mode])",
			"io\tread\t(...)",
			"io\ttempfile\t()",
			"io\ttype\t(obj)",
			"io\twrite\t(...)",
			// math libary
			"_G\tmath\t{}",
			"math\tabs\t(x)",
			"math\tacos\t(x)",
			"math\tatan\t(x)",
			"math\tatan2\t(x, y)",
			"math\tceil\t(x)",
			"math\tcosh\t(x)",
			"math\tdeg\t(x)",
			"math\texp\t(x)",
			"math\tfloor\t(x)",
			"math\tfmod\t(x, y)",
			"math\tfrexp\t(x)",
			"math\thuge\t#",
			"math\tldexp\t(m, e)",
			"math\tlog\t(x)",
			"math\tlog10\t(x)",
			"math\tmax\t(x, ...)",
			"math\tmin\t(x, ...)",
			"math\tmodf\t(x)",
			"math\tpi\t#",
			"math\tpow\t(x, y)",
			"math\trad\t(x)",
			"math\trandom\t([m [, n]])",
			"math\trandomseed\t(x)",
			"math\tsin\t(x)",
			"math\tsinh\t(x)",
			"math\tsqrt\t(x)",
			"math\ttan\t(x)",
			"math\ttanh\t(x)",
			// os libary
			"_G\tos\t{}",
			"os\tclock\t()",
			"os\tdate\t([format [, time]])",
			"os\tdifftime\t(t2, t1)",
			"os\texecute\t([command])",
			"os\texit\t([code])",
			"os\tgetenv\t(varname)",
			"os\tremove\t(filename)",
			"os\trename\t(oldname, newname)",
			"os\tsetlocale\t(locale [, category])",
			"os\ttime\t([table])",
			"os\ttmpname\t()",
			// package libary
			"_G\tpackage\t{}",
			"_G\tmodule\t(name [, ...])",
			"_G\trequire\t(modname)",
			"package\tcpath\t#",
			"package\tloaded\t#",
			"package\tloaders\t#",
			"package\tloadlib\t(libname, funcname)",
			"package\tpath\t#",
			"package\tpreload\t#",
			"package\tseeall\t(module)",
			// string libary
			"_G\tstring\t{}",
			"string\tbyte\t(string [, from [, to]])",
			"string\tchar\t(...)",
			"string\tdump\t(function)",
			"string\tfind\t(string, pattern [, init [, plain]])",
			"string\tformat\t(string, ...)",
			"string\tgmatch\t(string, pattern)",
			"string\tgsub\t(string, pattern, repl [, n])",
			"string\tlen\t(string)",
			"string\tlower\t(string)",
			"string\tmatch\t(string, pattern [, init])",
			"string\trep\t(string, count)",
			"string\treverse\t(string)",
			"string\tsub\t(string, from [, to])",
			"string\tupper\t(string)",
			// table libary
			"_G\ttable\t{}",
			"table\tconcat\t(table [, sep [, i [, j]]])",
			"table\tinsert\t(table, [pos,] value)",
			"table\tmaxn\t(table)",
			"table\tremove\t(table [, pos])",
			"table\tsort\t(table [, comp])"
		};

		string[] ProjectGlobals = new string[0];
		string[] EngineGlobals = new string[0];
		DateTime? LastModified;
		DateTime? LastModifiedEngineGlobals;

		void UpdateEngineGlobals()
		{
			if (!document.HasProject)
				return;

			var proj = document.Project;
			var conf = (LuaConfiguration)proj.DefaultConfiguration;

			if (conf.LangVersion != LangVersion.GarrysMod ||
			    conf.LangVersion != LangVersion.Moai ||
			    conf.LangVersion != LangVersion.Love)
				return;

			string path = "";
			switch (conf.LangVersion) {
				case LangVersion.GarrysMod:
					path = proj.GetAbsoluteChildPath("garrysmod_definitions.txt");
					break;
				case LangVersion.Moai:
					path = proj.GetAbsoluteChildPath("Moai_definitions.txt");
					break;
				case LangVersion.Love:
					path = proj.GetAbsoluteChildPath("Love_definitions.txt");
					break;
			}

			if (!File.Exists(path)) {
				if (conf.LangVersion == LangVersion.GarrysMod || conf.LangVersion == LangVersion.Moai) {
					var assembly = Assembly.GetExecutingAssembly();
					var resourceName = "";

					if (conf.LangVersion == LangVersion.GarrysMod) {
						resourceName = "LuaBinding.garrysmod_definitions.txt";
					}
					if (conf.LangVersion == LangVersion.Moai) {
						resourceName = "LuaBinding.Moai_definitions.txt";
					}
						
					//Copy!
					using (Stream instream = assembly.GetManifestResourceStream(resourceName))
					using (var outstream = File.Create(path)) {
						instream.CopyTo(outstream);
					}
				}
			}

			if (LastModifiedEngineGlobals == null || File.GetLastWriteTimeUtc(path) > LastModifiedEngineGlobals) {
				LastModifiedEngineGlobals = File.GetLastWriteTimeUtc(path);
				EngineGlobals = File.ReadAllLines(path);
			}
		}

		void UpdateProjectGlobals()
		{
			if (!document.HasProject)
				return;

			var proj = document.Project;
			string path = proj.GetAbsoluteChildPath(".project_globals");

			if (!File.Exists(path))
				return;



			if (LastModified == null || File.GetLastWriteTimeUtc(path) > LastModified) {
				LastModified = File.GetLastWriteTimeUtc(path);
				ProjectGlobals = File.ReadAllLines(path);
			}
		}

		readonly Regex rx_is_local = new Regex(@"^\s*(local|for)\s+((([A-z_][A-z0-9_]*))(\s*,\s*([A-z_][A-z0-9_]*))*)?\s*$", RegexOptions.Compiled);
		readonly Regex rx_is_function = new Regex(@"^.*(local\s+)?function(\s+([A-z0-9_\.]*))?\s*(\([A-z0-9_, ]*)?$", RegexOptions.Compiled);
		readonly Regex rx_is_comment = new Regex(@"^.*--.*$", RegexOptions.Compiled);
		readonly Regex rx_in_string = new Regex(@"(?<!\\)""", RegexOptions.Compiled);
		readonly Regex rx_is_number = new Regex(@"(?<![A-z_][0-9\.]*)[0-9\.]+$", RegexOptions.Compiled);

		public override bool CanRunCompletionCommand()
		{
			string line = Editor.GetLineText(Editor.Caret.Line);
			string to_left = line.Substring(0, Math.Min(Editor.Caret.Column - 1, line.Length));

			{ // Are we in a definition?
				if (rx_is_local.IsMatch(to_left) || rx_is_function.IsMatch(to_left))
					return false; // nope, we're defining a local variable
			}

			{ // Are we in a comment?
				if (rx_is_comment.IsMatch(to_left))
					return false;
			}

			{ // TODO: Are we in a string?
				MatchCollection col = rx_in_string.Matches(to_left);
				if (col.Count % 2 == 1) // inside string
					return false;
			}

			{ // Are we writing a number?
				if (rx_is_number.IsMatch(to_left))
					return false;
			}

			/*
			{ // keyword?
				int pos = Editor.Caret.Offset;
				string word = "";

				while( pos > 1 )
				{
					char x = Editor.GetCharAt( pos );
					if( !ValidVarnameChar( x ) )
						break;
					word = x + word;
					pos--;
				}
				// TODO: this
			}
			*/

			// in strings and stuff
			return true; // base.CanRunCompletionCommand();
		}

		public override ICompletionDataList HandleCodeCompletion(CodeCompletionContext completionContext, char completionChar, ref int triggerWordLength)
		{
			if (!CanRunCompletionCommand())
				return null;

			if (completionChar == '(' || completionChar == ')' ||
			    completionChar == '[' || completionChar == ']' ||
			    completionChar == '{' || completionChar == '}' ||
			    completionChar == '"' || completionChar == '\'' ||
			    completionChar == ';' || completionChar == '=' ||
			    completionChar == ' ' || completionChar == '\t' ||
			    completionChar == ',') {
				if (completionChar == '(') {
					// TODO: Add function args
				}
				return null; // don't show it yet
			}

			CompletionDataList ret = new CompletionDataList();
			string fullcontext = "";
			bool has_namespace = false;

			if (completionContext.TriggerOffset > 1) {
				completionContext.TriggerOffset = document.Editor.Caret.Offset;
				int pos = completionContext.TriggerOffset - 1;
				
				while (pos > 1) {
					char letter = document.Editor.GetCharAt(pos);

					pos--;
					if (letter == '.' || letter == ':' || letter == ',' ||
					    !ValidVarnameChar(letter)) {
						has_namespace |= letter == '.' || letter == ':';
						break;

					}
				}
				
				triggerWordLength = completionContext.TriggerOffset - pos - 2;
				completionContext.TriggerWordLength = triggerWordLength;

				// now, work out the context
				bool did_space = false;
				bool did_namespace = false;

				while (pos > 1 && has_namespace) {
					char letter = document.Editor.GetCharAt(pos);
					
					if (char.IsWhiteSpace(letter))
						did_space = true;
					else if (did_space && !did_namespace && (letter != '.' && letter != ':')) {
						break;
					} else if (letter == '.' || letter == ':') {
						did_namespace = true;
						fullcontext = letter + fullcontext;
					} else if (ValidVarnameChar(letter)) {
						did_namespace = false;
						did_space = false;
						fullcontext = letter + fullcontext;
					} else
						break;
					pos--;
				}
			}

			if (completionChar == '.' && string.IsNullOrWhiteSpace(fullcontext.Trim(".".ToCharArray())))
				return null;

			if (fullcontext.Trim() == "")
				fullcontext = "_G";
			else {
				fullcontext = fullcontext.TrimEnd(".".ToCharArray());
				if (fullcontext.StartsWith("_G.", StringComparison.Ordinal))
					fullcontext = fullcontext.Substring("_G.".Length);
			}

			CompletionCategory cat = null;
			Action<string> handle_line = delegate(string line) {
				if (line.Trim().StartsWith("#", StringComparison.Ordinal))
					return;
				if (string.IsNullOrWhiteSpace(line))
					return;

				string[] split = line.Split("\t".ToCharArray());

				string arg0 = split.Length >= 1 ? split[0] : ""; // namespace
				string arg1 = split.Length >= 2 ? split[1] : ""; // name
				string arg2 = split.Length >= 3 ? split[2] : ""; // arguments

				if (arg0 == fullcontext) {
					string icon = MonoDevelop.Ide.Gui.Stock.Method;
					string arg = arg2;

					switch (arg) {
						case "":
							icon = "md-keyword";
							arg = "";
							break;
						case "{}":
							icon = MonoDevelop.Ide.Gui.Stock.NameSpace;
							arg = "";
							break;
						case "#":
							icon = MonoDevelop.Ide.Gui.Stock.Literal;
							arg = "";
							break;
					}

					var elm = ret.Add(arg1 + arg, icon, "", arg1);
					elm.CompletionCategory = cat;
				}
			};

			UpdateProjectGlobals();
			//UpdateEngineGlobals();
			cat = new GlobalCompletionCategory();

			foreach (string glob in Globals)
				handle_line(glob);
			foreach (string glob in ProjectGlobals)
				handle_line(glob);
			foreach (string glob in EngineGlobals)
				handle_line(glob);

			cat = new LocalCompletionCategory();
			cat.DisplayText = "Locals";

			foreach (string glob in GetDocumentLocals())
				handle_line(glob);

			return ret;
			//return base.HandleCodeCompletion(completionContext, completionChar, ref triggerWordLength);
		}


		bool wasnil = true;

		public override void RunCompletionCommand()
		{
			wasnil = false; // allow ctrl+space to open the completion menu thingy
			base.RunCompletionCommand();
		}

		public override ParameterDataProvider HandleParameterCompletion(CodeCompletionContext completionContext, char completionChar)
		{
			// they're just moving their cursor around the document
			if (wasnil && completionChar == '\0')
				return null;

			var ret = _HandleParameterCompletion(completionContext, completionChar);
			wasnil = ret == null;
			return ret;
		}

		ParameterDataProvider _HandleParameterCompletion(CodeCompletionContext completionContext, char completionChar)
		{

			// attempt to get the function
			int row = completionContext.TriggerLine;
			int col = completionContext.TriggerLineOffset;
			string line = Document.Editor.GetLineText(row);
			line = line.Substring(0, Math.Min(col, line.Length));

			// Keep reading backwards until the last unclosed function
			int pos = line.Length - 1;
			int depth = 0;
			bool _break_loop = false;
			while (pos > 0) {
				char x = line[pos];

				switch (x) {
					case ')':
						depth++;
						break;
					case '(':
						depth--;
						_break_loop = depth < 0;
						break;
				}

				if (_break_loop)
					break;

				pos--;
			}

			if (pos <= 0) // negative, captin, not in a function
				return null;
			line = line.Substring(0, pos);

			//Console.WriteLine( "HandleParameterCompletion :{0}:{1}", completionContext.TriggerLine, line );


			bool in_name = true;
			string name = "", context = "";
			for (int i = line.Length; i-- > 0;) {
				char x = line[i];

				if (in_name) {
					if (x == '.' || x == ':')
						in_name = false;
					else if (!ValidVarnameChar(x))
						break;
					else
						name = x + name;
				} else {
					if (x == '.' || x == ':' || ValidVarnameChar(x))
						context = x + context;
					else
						break;
				}
			}

			if (context == "")
				context = "_G";
			string args = "";

			Action<string> handle_line = delegate(string line2) {
				if (line2.Trim().StartsWith("#", StringComparison.Ordinal))
					return;
				if (string.IsNullOrWhiteSpace(line2))
					return;

				string[] split = line2.Split("\t".ToCharArray());

				string arg0 = split.Length >= 1 ? split[0] : ""; // namespace
				string arg1 = split.Length >= 2 ? split[1] : ""; // name
				string arg2 = split.Length >= 3 ? split[2] : ""; // arguments

				if (arg0 == context && arg1 == name)
					args = arg2;
			};

			//UpdateEngineGlobals();
			UpdateProjectGlobals();
			foreach (string glob in Globals)
				handle_line(glob);
			foreach (string glob in ProjectGlobals)
				handle_line(glob);
			foreach (string glob in EngineGlobals)
				handle_line(glob);
			foreach (string glob in GetDocumentLocals())
				handle_line(glob);

			if (args == "")
				return null;
			args = args.Trim("()".ToCharArray());

			return new LuaParameterDataProvider(name, args);
			//return base.HandleParameterCompletion(completionContext, completionChar);
		}

		public override ICompletionDataList CodeCompletionCommand(CodeCompletionContext completionContext)
		{
			int i = 0;
			return HandleCodeCompletion(completionContext, '\0', ref i);

			//return base.CodeCompletionCommand(completionContext);
		}

		//readonly Regex rx_global_funcs = new Regex(@"\s+\d+\s+\[\d+\]\s+SETTABUP\s+[-\d]+\s+[-\d]+\s+[-\d]+\s+;\s+_ENV\s+""(?<name>.+)""", RegexOptions.Compiled);
		readonly Regex rx_locals = new Regex(@"(?<tabs>[ \t]*)local\s+(function\s+(?<func_name>[A-z_][A-z0-9_]*)\s*\((?<func_args>.*)\)|(?<vars>([A-z_][A-z0-9_]*))(\s*,\s*([A-z_][A-z0-9_]*))*)", RegexOptions.Compiled);
		readonly Regex rx_args_and_for = new Regex(@"(?<tabs>[ \t]*)((local\s+)?function\s+([A-z][A-z0-9]+)?\((?<vars>.+)\)|for\s+(?<vars>.+)\s+(in\s+|=))", RegexOptions.Compiled);
		// file, tuple; tuple is start_line, end_line, functiontype
		//Dictionary<string, List<Tuple<int, int, string>>> Cached; // this is so that syntax error we can still get the last successfull
		// cached result
		List<string> GetDocumentLocals()
		{
			var ret = new List<string>();

			// First, get the real globals
			//document.FileName

			// Then get the locals from our char pos, along with the indentation (use tabs, please :C)
			// This is a hacky method that gets the indentation depth 
			string text = Editor.GetTextBetween(0, Editor.Caret.Offset);

			// Get locals
			{
				MatchCollection collection = rx_locals.Matches(text);

				var col = new Match[collection.Count];
				int i = 1;
				foreach (Match match in collection) {
					col[col.Length - i] = match;
					i++;
				}

				int depth = -1;
				foreach (Match match in col) {
					// replace tab with 4 spaces, as this is *usually* right (few people use 2, 6 or 8 size tabs)
					// TODO: Make this get the size from MonoDevelop
					int tabs = match.Groups["tabs"].Value.Replace("\t", "    ").Length;

					// This bit of code just makes it so that "sub locals" arn't shown (it's hacky, i know)
					// TODO: Update the locals to something more, conrete
					if (depth == -1 || tabs < depth)
						depth = tabs;
					else if (tabs > depth) // nope.avi, we dropped lower than this before, try again
					continue;

					if (match.Groups["vars"].Success) {
						string vars = match.Groups["vars"].Value;
						foreach (string name in vars.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
							ret.Add(string.Format("_G\t{0}\t#", name.Trim()));
					} else {
						string func_name = match.Groups["func_name"].Value;
						string func_args = match.Groups["func_args"].Value;

						if (!string.IsNullOrWhiteSpace(func_name))
							ret.Add(string.Format("_G\t{0}\t({1})", func_name.Trim(), func_args.Trim()));
					}
				}
			}

			// Get arguments
			{
				MatchCollection collection = rx_args_and_for.Matches(text);

				var col = new Match[collection.Count];
				int i = 1;
				foreach (Match match in collection) {
					col[col.Length - i] = match;
					i++;
				}

				int depth = -1;
				foreach (Match match in col) {
					// replace tab with 4 spaces, as this is *usually* right (few people use 2, 6 or 8 size tabs)
					// TODO: Make this get the size from MonoDevelop
					int tabs = match.Groups["tabs"].Value.Replace("\t", "    ").Length + 4;

					// This bit of code just makes it so that "sub locals" arn't shown (it's hacky, i know)
					// TODO: Update the locals to something more, conrete
					if (depth == -1 || tabs < depth)
						depth = tabs;
					else if (tabs > depth) // nope.avi, we dropped lower than this before, try again
						continue;

					string[] args = match.Groups["vars"].Value.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

					if (args.Length > 0)
						foreach (string arg in args)
							ret.Add(string.Format("_G\t{0}\t#", arg.Trim()));
				}
			}

			// global functions...
			{
				// TODO:
			}

			return ret.Distinct().ToList();
		}

		Regex rx_is_keyword = new Regex(@"(and|break|do|else|elseif|end|for|function|if|local|nil|not|or|repeat|return|then|until|while)$", RegexOptions.Compiled);

		public override bool KeyPress(Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			// If we are a keyword
			if (keyChar == ' ' && modifier == Gdk.ModifierType.None && CompletionWidget != null) {
				CompletionWindowManager.PreProcessKeyEvent(Gdk.Key.Tab, '\t', Gdk.ModifierType.None);
				CompletionWindowManager.PostProcessKeyEvent(Gdk.Key.Tab, '\t', Gdk.ModifierType.None);
				CompletionWidget.CurrentCodeCompletionContext.TriggerWordLength = 0;
			}

			bool ret = base.KeyPress(key, keyChar, modifier);

			{ // did we type a keyword?
				string line = Editor.GetLineText(Editor.Caret.Line);
				string to_left = line.Substring(0, Math.Min(line.Length, Editor.Caret.Column));

				if (rx_is_keyword.IsMatch(to_left))
					CompletionWindowManager.HideWindow();
			}

			return ret;
		}

		public override int GetCurrentParameterIndex(int startOffset)
		{
			int pos = startOffset; //Editor.Caret.Offset; // startOffset;
			int commas = 1;
			int depth = 0;

			while (pos > 1) {
				char x = document.Editor.GetCharAt(pos);

				switch (x) {
					case ')':
						depth++;
						break;
					case '(':
						depth--;
						if (depth < 0)
							return commas + 1;
						break;
					case ',':
						if (depth == 0)
							commas++;
						break;
				}

				pos--;
			}

			//return 1;
			return base.GetCurrentParameterIndex(startOffset);
		}
	}
}