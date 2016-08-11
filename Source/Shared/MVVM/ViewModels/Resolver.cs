//---------------------------------------------------------------------------------------
// Copyright 2015 North Carolina State University
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
using System.Collections.Generic;
using Zenject;
using System;
using IntelliMedia.Views;
using IntelliMedia.Utilities;

namespace IntelliMedia.ViewModels
{
	public class Resolver : ITheatreResolver
	{
		public string Name { get; private set; }

		private StageManager StageManager;
		private readonly DiContainer Container;

		public Resolver(string name, DiContainer container)
		{
			this.Name = name;
			this.Container = container;	
		}			

		public IAsyncTask TryResolve<T>() where T : class
		{
			return Resolve(typeof(T), false);
		}

		public IAsyncTask Resolve<T>() where T : class
		{
			return Resolve(typeof(T), true);
		}

		public IAsyncTask TryResolve(Type type)
		{
			return Resolve(type, false);
		}

		public IAsyncTask Resolve(Type type)
		{
			return Resolve(type, true);
		}

		public IAsyncTask TryResolveViewFor(ViewModel vm, string[] capabilities = null)
		{
			return ResolveViewFor( vm.GetType(), capabilities, false);
		}

		public IAsyncTask ResolveViewFor(ViewModel vm, string[] capabilities = null)
		{
			return ResolveViewFor( vm.GetType(), capabilities, true);
		}

		private IAsyncTask ResolveViewFor(Type vmType, string[] capabilities, bool throwOnError)
		{
			return new AsyncTask((onCompleted, onError) =>
			{			
				Type viewType = null;
				ViewDescriptorAttribute attribute = null;
				foreach(Type iviewType in Container.ResolveTypeAll(typeof(IView)))
				{
					DebugLog.Info("IView: {0}", iviewType.Name);
					attribute = ViewDescriptorAttribute.FindOn(iviewType);
					if (attribute != null && attribute.ViewModelType == vmType)
					{
						viewType = iviewType;
						break;
					}
				}

				if (viewType == null && throwOnError)
				{
					throw new Exception(string.Format("Unable to find IView with ViewDescriptorAttribute.ViewModelType = '{0}'",
						vmType.Name));
				}

				onCompleted(viewType);
			});
		}

		private IAsyncTask Resolve(Type type, bool throwOnError)
		{
			return new AsyncTask((onCompleted, onError) =>
			{
				object obj = null;
				if (throwOnError)
				{					
					obj = Container.Resolve(type);
				}
				else
				{
					obj = Container.TryResolve(type);
				}

				if (obj != null)
				{
					DebugLog.Info("{0}: Resolved '{1}'", Name, type.Name);					
				}
					
				onCompleted(obj);
			});
		}
	}
}
 