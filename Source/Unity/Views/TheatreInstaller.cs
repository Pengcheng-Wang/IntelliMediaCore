//---------------------------------------------------------------------------------------
// Copyright 2014 North Carolina State University
//
// Center for Educational Informatics
// http://www.cei.ncsu.edu/
//
// Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:
//
//   * Redistributions of source code must retain the above copyright notice, this 
//     list of conditions and the following disclaimer.
//   * Redistributions in binary form must reproduce the above copyright notice, 
//     this list of conditions and the following disclaimer in the documentation 
//     and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" 
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE 
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE 
// GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) 
// HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT 
// LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT
// OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
//---------------------------------------------------------------------------------------
using Zenject;
using IntelliMedia;
using System;
using System.Linq;
using System.Reflection;

namespace IntelliMedia
{
	public class TheatreInstaller : MonoInstaller
	{
		public string[] services;
		public string[] repositories;
		public string[] models;
		public string[] viewModels;

		private Resolver Resolver { get; set; }

		private int TotalBindings { get; set; }

		public override void InstallBindings()
		{	
			Resolver = new Resolver(gameObject.name, Container);

			InstallBindingsByName("Services", services);	
			InstallBindingsByName("Repositories", repositories);	
			InstallBindingsByName("Models", models);	
			InstallBindingsByName("ViewModels", viewModels);

			if (TotalBindings == 0)
			{
				DebugLog.Warning("{0} doesn't specify any classes for binding.", this.gameObject.name);
			}				
		}

		public override void Start()
		{
			base.Start();

			Resolver.Register(Container.Resolve<StageManager>());
		}

		public void OnDestroy()
		{
			if (Resolver != null)
			{
				Resolver.Dispose();
			}
		}

		void InstallBindingsByName(string groupName, string[] classNames)
		{
			if (classNames == null || classNames.Length == 0)
			{
				return;
			}
				
			foreach (string className in classNames)
			{
				Type type = TypeFinder.ClassNameToType (className);
				Container.Bind (type).AsSingle ();
				++TotalBindings;
			}
			DebugLog.Info ("{0}: Installed {1} bindings: {2}", 
				gameObject.name, groupName, string.Join(", ", classNames));
		}
	}
}