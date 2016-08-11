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
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using IntelliMedia.Utilities;

namespace IntelliMedia.Repositories
{
    public class MemoryRepository<T> : Repository<T> where T : class, new()
    {
        private Dictionary<object, T> repository = new Dictionary<object, T>();

        public MemoryRepository()
        {
        }

        #region IRepository implementation

		public override IAsyncTask Insert(T instance)
        {
			return new AsyncTask((onCompleted, onError) =>
			{
                if (IsIdNull(instance))
                {
                    AssignUniqueId(instance);
                }

                object id = GetKey(instance);
                if (repository.ContainsKey(id))
                {
                    throw new Exception("Attempting to insert an object which already exists. id =" + id.ToString());
                }
                repository[id] = instance;
				onCompleted(instance);
			});
		}

		public override IAsyncTask Update(T instance)
        {
			return new AsyncTask((onCompleted, onError) =>
			{
                object id = GetKey(instance);
                if (repository.ContainsKey(id))
                {
                    repository[id] = instance;
                }
                else
                {
                    throw new Exception("Unable to updated object that has not been inserted into the repository. id = " + id.ToString());
                }
				onCompleted(instance);
			});
		}

		public override IAsyncTask Delete(T instance)
        {
			return new AsyncTask((onCompleted, onError) =>
			{
                object id = GetKey(instance);
                repository.Remove(id);
				onCompleted(instance);
			});
		}		

		public override IAsyncTask Get(System.Func<T, bool> predicate)
        {
			return new AsyncTask((onCompleted, onError) =>
			{
				onCompleted(repository.Values.Where(predicate).ToList());
			});
		}

		public override IAsyncTask GetByKeys(object[] keys)
        {
			return new AsyncTask((onCompleted, onError) =>
			{
            	List<T> instances = new List<T>();
                foreach (object key in keys)
                {
                    if (repository.ContainsKey(key))
                    {
                        instances.Add(repository[key]);
                    }
                    else
                    {
                        throw new Exception(String.Format("Unable to find {0} with id = {1}", typeof(T).Name, key.ToString()));
                    }
                }
				onCompleted(instances);
			});
		}

		public override IAsyncTask GetByKey(object key)
        {
			return new AsyncTask((onCompleted, onError) =>
			{		
				T instance = default(T);	
				repository.TryGetValue(key, out instance);
				onCompleted(instance);
			});
		}

        #endregion 
    }
}
