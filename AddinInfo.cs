
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("LuaBinding", 
        Namespace = "MonoDevelop",
        Version = "4.1.5",
        Category = "Language bindings")]

[assembly:AddinName ("Lua Language Binding")]
[assembly:AddinDescription ("Lua language binding")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]

