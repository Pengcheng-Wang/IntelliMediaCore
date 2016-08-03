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

namespace IntelliMedia
{
	public class ViewModelFactory : IDisposable
	{
		public string Name { get; private set; }

		private readonly StageManager StageManager;
		private readonly DiContainer Container;

		public ViewModelFactory(string name, DiContainer container, StageManager stageManager)
		{
			this.Name = name;
			this.Container = container;
			this.StageManager = stageManager;

			StageManager.Register(this);	
		}

		#region IDisposable implementation

		public void Dispose()
		{
			StageManager.Unregister(this);	
		}

		#endregion

		public IAsyncTask TryResolve<T>() where T:class
		{
			return new AsyncTask((onCompleted, onError) =>
			{
				onCompleted(Container.TryResolve<T>());
			});
		}

		public IAsyncTask Resolve<T>() where T:class
		{
			return new AsyncTask((onCompleted, onError) =>
			{
				onCompleted(Container.Resolve<T>());
			});
		}

		public IAsyncTask TryResolve(Type type)
		{
			Contract.ArgumentNotNull("type", type);

			return new AsyncTask((onCompleted, onError) =>
			{
				onCompleted(Container.TryResolve(type));
			});
		}

		public IAsyncTask Resolve(Type type)
		{
			Contract.ArgumentNotNull("type", type);

			return new AsyncTask((onCompleted, onError) =>
			{
				onCompleted(Container.Resolve(type));
			});
		}
	}
}
 