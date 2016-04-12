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

namespace IntelliMediaSample
{
	public class TheatreInstaller : MonoInstaller
	{
		public string[] services;
		public string[] repositories;
		public string[] models;

		[Serializable]
		public class ViewViewModel
		{
			public string viewModel;
			public bool singleton;
			public UnityGuiView view;
			public bool viewIsPrefab;
			public string activityUrn;
			public bool revealOnStart;
		}
		public ViewViewModel[] viewsAndViewModels;

		private static Type ClassNameToType(string className)
		{
			Contract.ArgumentNotNull("className", className);

			Type viewModelType = Assembly.GetExecutingAssembly().GetTypes().FirstOrDefault(t => String.Compare(t.Name, className) == 0);
			if (viewModelType == null)
			{
				throw new Exception("Unable to find class with name: " + className);
			}

			return viewModelType;
		}

		public override void InstallBindings()
		{	
			InstallBindingsByName("Services", services);	
			InstallBindingsByName("Repositories", repositories);	
			InstallBindingsByName("Models", models);	

			InstallMvvmBindings();	
		}

		public override void Start()
		{
			base.Start();

			if (viewsAndViewModels != null && viewsAndViewModels.Any(v => v.revealOnStart))
			{
				foreach(ViewViewModel vvm in viewsAndViewModels.Where(v => v.revealOnStart))
				{
					Container.Resolve<StageManager>().Reveal(vvm.viewModel).Start();
				}
			}
			else
			{
				DebugLog.Warning("TheatreInstaller: No ViewModel's RevealOnStart set.");
			}
		}

		void InstallBindingsByName(string groupName, string[] classNames)
		{
			if (services.Length == 0)
			{
				DebugLog.Info("No {0} to install", groupName);
				return;
			}

			DebugLog.Info ("Install {0} bindings", groupName);
			foreach (string className in classNames)
			{
				Type type = ClassNameToType (className);
				DebugLog.Info ("   Bind {0} to single", type.Name);
				Container.Bind (type).ToSingle ();
			}
		}

		void InstallMvvmBindings()
		{
			if (viewsAndViewModels.Length == 0)
			{
				DebugLog.Info("No View or ViewModels to install");
				return;
			}

			DebugLog.Info ("Install MVVM bindings");
			if (Container.Resolve<ViewFactory> () == null)
			{
				throw new Exception ("ViewFactory has not been installed in Dependency Injection container");
			}
			foreach (ViewViewModel vvm in viewsAndViewModels)
			{
				Type vmType = ClassNameToType(vvm.viewModel);
				if (vvm.singleton)
				{
					DebugLog.Info ("   Bind {0} to single", vmType.Name);
					Container.Bind(vmType).ToSingle();
				}
				else
				{
					DebugLog.Info ("   Bind {0} to transient", vmType.Name);
					Container.Bind(vmType).ToTransient();
				}

				Type viewType = vvm.view.GetType ();
				if (vvm.viewIsPrefab)
				{
					DebugLog.Info ("Bind {0} to transient prefab", viewType.Name);
					Container.Bind (viewType).ToTransientPrefab(vvm.view.gameObject);
				}
				else
				{
					DebugLog.Info ("Bind {0} to instance", viewType.Name);
					Container.Bind (viewType).ToInstance(vvm.view.gameObject);
				}
				Container.Resolve<ViewFactory> ().Register (vmType, viewType);
				if (!String.IsNullOrEmpty (vvm.activityUrn))
				{
					Container.Resolve<ActivityLauncher> ().Register (vvm.activityUrn, vmType);
				}
			}
		}
	}
}