// 
// CompilerParametersPanelWidget.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of thi0s software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using MonoDevelop.Projects;
using Gtk;

namespace LuaBinding
{
	[System.ComponentModel.ToolboxItem(true)]
	partial class CompilerParametersPanelWidget : Bin
	{

		ListStore _VersionsStore;

		public CompilerParametersPanelWidget()
		{
			Build();

			_VersionsStore = new ListStore(typeof(string), typeof(LangVersion));
			_VersionsStore.AppendValues("Default", LangVersion.Lua);
			_VersionsStore.AppendValues("Lua 5.1", LangVersion.Lua51);
			_VersionsStore.AppendValues("Lua 5.2", LangVersion.Lua52);
			_VersionsStore.AppendValues("LuaJIT", LangVersion.LuaJIT);
			_VersionsStore.AppendValues("Garry's Mod", LangVersion.GarrysMod);
			_VersionsStore.AppendValues("Moai", LangVersion.Moai);
			_VersionsStore.AppendValues("LÖVE", LangVersion.Love);
			LanguageVersion.Model = _VersionsStore;

			Visible = true;
		}

		public string DefaultFile {
			get { return MainFileEntry.Text ?? String.Empty; }
			set { MainFileEntry.Text = value ?? String.Empty; }
		}

		public LangVersion LangVersion {
			get {
				return (LangVersion)LanguageVersion.Active;
			}
			set {
				LanguageVersion.Active = (int)value;
			}
		}

		public string Launcher {
			get {
				return !string.IsNullOrEmpty(filechooserbutton3.Uri) ? new Uri(filechooserbutton3.Uri).LocalPath : "";
			}set {
				if (!string.IsNullOrEmpty(value))
					filechooserbutton3.SetUri(new Uri(value).AbsoluteUri);
			}

		}

		DotNetProject pproject;
		DotNetProjectConfiguration pconfiguration;

		public void Load(DotNetProject project, DotNetProjectConfiguration configuration)
		{
			pproject = project;
			pconfiguration = configuration;
		}

		public void Store()
		{
			pproject.CompileTarget = CompileTarget.Exe;
			pconfiguration.DebugMode = false;
		}
	}

	sealed class CompilerParametersPanel : MonoDevelop.Ide.Gui.Dialogs.MultiConfigItemOptionsPanel
	{
		CompilerParametersPanelWidget widget;

		public override Widget CreatePanelWidget()
		{
			return widget = new CompilerParametersPanelWidget();
		}

		public override void LoadConfigData()
		{
			var config = CurrentConfiguration as LuaConfiguration;

			widget.DefaultFile = config.MainFile;
			widget.LangVersion = config.LangVersion;
			widget.Launcher = config.Launcher;
		}

		public override void ApplyChanges()
		{
			var config = CurrentConfiguration as LuaConfiguration;

			config.MainFile = widget.DefaultFile;
			config.LangVersion = widget.LangVersion;
			config.Launcher = widget.Launcher;
		}
	}
}
