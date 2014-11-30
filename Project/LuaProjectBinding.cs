
using System.IO;
using System.Xml;

using MonoDevelop.Projects;

namespace LuaBinding
{
	public class LuaProjectBinding : IProjectBinding
	{
		public string Name {
			get { return "Lua"; }
		}

		public Project CreateProject (ProjectCreateInformation info, XmlElement projectOptions)
		{
			return new LuaProject(Name, info, projectOptions);
		}

		public Project CreateSingleFileProject (string sourceFile)
		{
			return LuaProject.FromSingleFile(Name, sourceFile);
		}

		public bool CanCreateSingleFileProject (string sourceFile)
		{
			return Path.GetExtension(sourceFile) == "lua";
		}
	}
}