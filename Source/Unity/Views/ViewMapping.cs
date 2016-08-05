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
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace IntelliMedia
{
	public class ViewMapping : MonoBehaviour, IViewResolver
	{
		private readonly Dictionary<Type, Info> viewMap = new Dictionary<Type, Info>();

		[Serializable]
		public class Info
		{
			public string viewModelTypeName;
			public string viewTypeName;
			public Type viewType;
			public string[] viewCapabilities;
		}
		public Info[] views;

		private StageManager stageManager;

		[Inject]
		public void Initialize(StageManager stageManager)
		{
			this.stageManager = stageManager;

			foreach (Info info in views)
			{
				try
				{
					Type viewModelType = TypeFinder.ClassNameToType(info.viewModelTypeName);
					viewMap[viewModelType] = info;
					viewMap[viewModelType].viewType = TypeFinder.ClassNameToType(info.viewTypeName);
				}
				catch(Exception e)
				{
					DebugLog.Error("{0}: {1}", gameObject.name, e.Message);
				}
			}

			if (viewMap.Keys.Count == 0)
			{
				DebugLog.Warning("{0}: Zero views defined. Remove this component if not used.", gameObject.name);
			}
		}

		public void Start()
		{	
			stageManager.Register(this);
		}			

		public void OnDestroy()
		{
			stageManager.Unregister(this);
		}

		#region IViewResolver implementation

		public string Name { get { return gameObject.name; }}

		public Type Resolve(Type viewModelType, string[] capabilities = null)
		{
			if (!viewMap.ContainsKey(viewModelType))
			{
//				if (throwOnError)
//				{
//					throw new Exception(string.Format("{0}: Unable to find view for '{1}'.",
//						Name, viewModelType.Name));
//				}
//				else
				{
					return null;
				}
			}

			return viewMap[viewModelType].viewType;
		}

		#endregion
	}
}

