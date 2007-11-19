﻿// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <author name="Daniel Grunwald"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.IO;
using System.Xml;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.Widgets.SideBar;
using ICSharpCode.SharpDevelop.Internal.Templates;

namespace ICSharpCode.SharpDevelop.Gui
{
	/// <summary>
	/// The tools box shown when using the text editor.
	/// </summary>
	public sealed class TextEditorSideBar : SharpDevelopSideBar
	{
		static TextEditorSideBar instance;
		
		public static TextEditorSideBar Instance {
			get {
				WorkbenchSingleton.AssertMainThread();
				if (instance == null) {
					instance = new TextEditorSideBar();
					instance.Initialize();
				}
				return instance;
			}
		}
		
		SideTab clipboardRing;
		
		private TextEditorSideBar()
		{
		}
		
		private void Initialize()
		{
			foreach (TextTemplate template in TextTemplate.TextTemplates) {
				SideTab tab = new SideTab(this, template.Name);
				tab.DisplayName = StringParser.Parse(tab.Name);
				tab.CanSaved  = false;
				foreach (TextTemplate.Entry entry in template.Entries)  {
					tab.Items.Add(SideTabItemFactory.CreateSideTabItem(entry.Display, entry.Value));
				}
				tab.CanBeDeleted = tab.CanDragDrop = false;
				Tabs.Add(tab);
			}
			
			try {
				XmlDocument doc = new XmlDocument();
				
				doc.Load(Path.Combine(PropertyService.ConfigDirectory, "SideBarConfig.xml"));
				if (doc.DocumentElement.GetAttribute("version") != "1.0") {
					GenerateStandardSideBar();
				} else {
					LoadSideBarConfig(doc.DocumentElement["SideBar"]);
				}
			} catch (FileNotFoundException) {
				// do not show a warning when the side bar file does not exist
				GenerateStandardSideBar();
			} catch (Exception ex) {
				MessageService.ShowWarning(ex.ToString());
				GenerateStandardSideBar();
			}
			
			WorkbenchSingleton.WorkbenchUnloaded += delegate { SaveSideBarViewConfig(); };
		}
		
		void GenerateStandardSideBar()
		{
			clipboardRing = new SideTab(this, "${res:SharpDevelop.SideBar.ClipboardRing}");
			clipboardRing.DisplayName = StringParser.Parse(clipboardRing.Name);
			clipboardRing.CanBeDeleted = false;
			clipboardRing.CanDragDrop  = false;
			this.Tabs.Add(clipboardRing);
			this.ActiveTab = clipboardRing;
		}
		
		public void PutInClipboardRing(string text)
		{
			if (clipboardRing != null) {
				clipboardRing.Items.Add("Text:" + text.Trim(), text);
				if (clipboardRing.Items.Count > 20) {
					clipboardRing.Items.RemoveAt(0);
				}
			}
			Refresh();
		}
		
		public void SaveSideBarViewConfig()
		{
			XmlDocument doc = new XmlDocument();
			doc.LoadXml("<SideBarConfig version=\"1.0\"/>");
			doc.DocumentElement.AppendChild(WriteConfig(doc));
			
			FileUtility.ObservedSave(new NamedFileOperationDelegate(doc.Save),
			                         Path.Combine(PropertyService.ConfigDirectory, "SideBarConfig.xml"),
			                         FileErrorPolicy.ProvideAlternative);
		}
		
		void LoadSideBarConfig(XmlElement el)
		{
			foreach (XmlElement sideTabEl in el.ChildNodes) {
				SideTab tab = new SideTab(this, sideTabEl.GetAttribute("text"));
				tab.DisplayName = StringParser.Parse(tab.Name);
				if (tab.Name == el.GetAttribute("activetab")) {
					ActiveTab = tab;
				} else {
					if (ActiveTab == null) {
						ActiveTab = tab;
					}
				}
				
				foreach (XmlElement sideTabItemEl in sideTabEl.ChildNodes) {
					tab.Items.Add(SideTabItemFactory.CreateSideTabItem(sideTabItemEl.GetAttribute("text"),
					                                                   sideTabItemEl.GetAttribute("value")));
				}
				
				if (sideTabEl.GetAttribute("clipboardring") == "true") {
					tab.CanBeDeleted = false;
					tab.CanDragDrop  = false;
					tab.Name         = "${res:SharpDevelop.SideBar.ClipboardRing}";
					tab.DisplayName  = StringParser.Parse(tab.Name);
					clipboardRing = tab;
				}
				Tabs.Add(tab);
			}
		}
		
		XmlElement WriteConfig(XmlDocument doc)
		{
			if (doc == null) {
				throw new ArgumentNullException("doc");
			}
			XmlElement el = doc.CreateElement("SideBar");
			el.SetAttribute("activetab", ActiveTab.Name);
			
			foreach (SideTab tab in Tabs) {
				if (tab.CanSaved) {
					XmlElement child = doc.CreateElement("SideTab");
					
					if (tab == clipboardRing) {
						child.SetAttribute("clipboardring", "true");
					}
					
					child.SetAttribute("text", tab.Name);
					
					foreach (SideTabItem item in tab.Items) {
						XmlElement itemChild = doc.CreateElement("SideTabItem");
						
						itemChild.SetAttribute("text",  item.Name);
						itemChild.SetAttribute("value", item.Tag.ToString());
						
						child.AppendChild(itemChild);
					}
					el.AppendChild(child);
				}
			}
			
			return el;
		}
	}
}
