using System;
using System.IO;
using System.Xml;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Ide;
using MonoDevelop.Projects;


namespace LuaBinding
{
	public class LuaProject : Project
	{
		const string ProjectTypeName = "Lua";

		public LuaProject()
		{
		}

		public LuaProject(string languageName, ProjectCreateInformation info, XmlElement projectOptions)
		{
			//TODO projectOptions does nothing!
			if (!String.Equals(languageName, ProjectTypeName))
				throw new ArgumentException("Not a Lua project: " + languageName);

			if (info != null) {
				Name = info.ProjectName;
			}

			CreateDefaultConfigurations();
		}

		public override System.Collections.Generic.IEnumerable<string> GetProjectTypes()
		{
			return new [] {
				ProjectTypeName
			};
		}

		public static LuaProject FromSingleFile(string languageName, string fileName)
		{
			var projectInfo = new ProjectCreateInformation {
				ProjectName = Path.GetFileNameWithoutExtension(fileName),
				SolutionPath = Path.GetDirectoryName(fileName),
				ProjectBasePath = Path.GetDirectoryName(fileName)
			};

			var project = new LuaProject(languageName, projectInfo, null);
			project.AddFile(new ProjectFile(fileName));
			return project;
		}

		public override SolutionItemConfiguration CreateConfiguration(string name)
		{
			return new LuaConfiguration(name);
		}

		public override bool IsCompileable(string fileName)
		{
			return fileName.ToLower().EndsWith(".lua", StringComparison.Ordinal);
		}

		protected override bool OnGetCanExecute(ExecutionContext context, ConfigurationSelector configuration)
		{
			// TODO: Check interpreter paths from here...
			var config = DefaultConfiguration as LuaConfiguration;
			return !string.IsNullOrWhiteSpace(config.MainFile);
		}

		protected override bool CheckNeedsBuild(ConfigurationSelector configuration)
		{
			var config = DefaultConfiguration as LuaConfiguration;
			if (config != null && !string.IsNullOrEmpty(config.Launcher) &&
			    config.LangVersion == LangVersion.Moai &&
			    config.LangVersion == LangVersion.Love) {
				return false;
			}
			return base.CheckNeedsBuild(configuration);
		}


		protected override BuildResult DoBuild(IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			var config = DefaultConfiguration as LuaConfiguration;

			if (config != null && !string.IsNullOrEmpty(config.Launcher) && config.LangVersion == LangVersion.Moai) {
				monitor.ReportWarning("Can't build project with Moai syntax!");
				return new BuildResult("Can't build project with Moai syntax!", 0, 0);
			}

			if (config != null && !string.IsNullOrEmpty(config.Launcher) && config.LangVersion == LangVersion.Love) {
				monitor.ReportWarning("Can't build project with Love syntax!");
				return new BuildResult("Can't build project with Love syntax!", 0, 0);
			}

			if (config != null && config.LangVersion == LangVersion.GarrysMod) {
				monitor.ReportWarning("Can't build project with Garry's Mod Lua syntax!");
				return new BuildResult("Can't build project with Garry's Mod Lua syntax!", 0, 0);
			}

			return LuaCompilerManager.Compile(Items, config, configuration, monitor);
		}

		protected override void DoExecute(IProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			if (!CheckCanExecute(configuration))
				return;

			var config = (LuaConfiguration)GetConfiguration(configuration);
			IConsole console = config.ExternalConsole ?
				context.ExternalConsoleFactory.CreateConsole(!config.PauseConsoleOutput) :
				context.ConsoleFactory.CreateConsole(!config.PauseConsoleOutput);

			var aggregatedMonitor = new AggregatedOperationMonitor(monitor);

			try {
				string param;

				if (config.LangVersion != LangVersion.Love)
					param = string.Format("\"{0}\" {1}", config.MainFile, config.CommandLineParameters);
				else
					param = string.Format("\"{0}\" {1}", ItemDirectory, config.CommandLineParameters);
					
				IProcessAsyncOperation op = 
					Runtime.ProcessService.StartConsoleProcess(GetLuaPath(config),
						param, (config.LangVersion != LangVersion.Love) ? BaseDirectory : ItemDirectory,
						config.EnvironmentVariables, console, null);

				monitor.CancelRequested += delegate {
					op.Cancel();
				};

				aggregatedMonitor.AddOperation(op);
				op.WaitForCompleted();
				monitor.Log.WriteLine("The application exited with code: " + op.ExitCode);
				/*
				var executionCommand = //CreateExecutionCommand( configuration, config );
					new NativeExecutionCommand( GetLuaPath( config.LangVersion ), 
					                           config.CommandLineParameters, 
					                           BaseDirectory );


				if( !context.ExecutionHandler.CanExecute( executionCommand ) )
				{
					monitor.ReportError( GettextCatalog.GetString( "Cannot execute application. The selected execution mode " +
					"is not supported for Lua projects" ), null );
					return;
				}

				IProcessAsyncOperation asyncOp = context.ExecutionHandler.Execute( executionCommand, console );
				aggregatedMonitor.AddOperation( asyncOp );
				asyncOp.WaitForCompleted();

				monitor.Log.WriteLine( "The application exited with code: " + asyncOp.ExitCode );
				*/
			} catch (Exception exc) {
				monitor.ReportError(GettextCatalog.GetString("Cannot execute \"{0}\"", config.MainFile), exc);
			} finally {
				console.Dispose();
				aggregatedMonitor.Dispose();
			}
		}

		bool CheckCanExecute(ConfigurationSelector configuration)
		{
			/*
			if( !IronManager.IsInterpreterPathValid() )
			{
				MessageService.ShowError( "Interpreter not set", "A valid interpreter has not been set." );
				return false;
			}
			*/

			var config = (LuaConfiguration)GetConfiguration(configuration);

			{ // check that the interpreter is set
				FilePath LuaPath = GetLuaPath(config);

				if (string.IsNullOrWhiteSpace(LuaPath))
					return false;
			}

			if (String.IsNullOrEmpty(config.MainFile)) {
				MessageService.ShowError("Main file not set", "Main file has not been set.");
				return false;
			}

			if (!File.Exists(BaseDirectory + "/" + config.MainFile)) {
				MessageService.ShowError("Main file is missing", string.Format("The file `{0}` does not exist!", config.MainFile));
				return false;
			}

			return true;
		}

		static FilePath GetLuaPath(LuaConfiguration conf)
		{
			switch (conf.LangVersion) {
				case LangVersion.Lua:
					return (FilePath)PropertyService.Get<string>("Lua.DefaultInterpreterPath");
				case LangVersion.Lua51:
					return (FilePath)PropertyService.Get<string>("Lua.51InterpreterPath");
				case LangVersion.Lua52:
					return (FilePath)PropertyService.Get<string>("Lua.52InterpreterPath");
				case LangVersion.LuaJIT:
					return (FilePath)PropertyService.Get<string>("Lua.JITInterpreterPath");
				case LangVersion.Moai:
				case LangVersion.Love:
					return new FilePath(conf.Launcher);
			}

			return null;
		}

		protected virtual LuaExecutionCommand CreateExecutionCommand(ConfigurationSelector configSel, LuaConfiguration configuration)
		{
			FilePath LuaPath = GetLuaPath(configuration);

			string arguments = "\"" + configuration.MainFile + "\" " + configuration.CommandLineParameters;
			
			var command = new LuaExecutionCommand(LuaPath) {
				Arguments = arguments,
				WorkingDirectory = BaseDirectory,
				EnvironmentVariables = configuration.GetParsedEnvironmentVariables(),
				Configuration = configuration
			};

			return command;
		}

		void CreateDefaultConfigurations()
		{
			var releaseconfig = CreateConfiguration("Release") as LuaConfiguration;
			Configurations.Add(releaseconfig);
		}
	}
}
