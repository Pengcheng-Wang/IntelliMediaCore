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
using UnityEngine;
using IntelliMedia.Utilities;

namespace IntelliMedia.Repositories
{
    public class UnityResourcesRepository<T> : Repository<T> where T : class, new()
    {
        public string DataDirectory { get; private set; }

        private ISerializer Serializer { get; set; }
        
        public UnityResourcesRepository(string repositoryPath, ISerializer serializer = null) 
        {
            Contract.ArgumentNotNull("repositoryPath", repositoryPath);

            DataDirectory = PathCombine(repositoryPath, typeof(T).Name);

            // Default to XML serializer
            Serializer = (serializer != null ? serializer : SerializerXml.Instance);
        }

        #region implemented abstract members of Repository

		public override IAsyncTask Insert(T instance)
        {
            throw new NotImplementedException("UnityResourcesRepository.Insert() is not supported since Unity Resources read only.");
        }
        
		public override IAsyncTask Update(T instance)
        {
            throw new NotImplementedException("UnityResourcesRepository.Update() is not supported since Unity Resources read only.");
        }
        
		public override IAsyncTask Delete(T instance)
        {
            throw new NotImplementedException("UnityResourcesRepository.Delete() is not supported since Unity Resources read only.");
        }

		public override IAsyncTask Get(System.Func<T, bool> predicate)
        {
			return new AsyncTask((onCompleted, onError) =>
			{
				List<T> items = new List<T>();
				TextAsset[] textAssets = Resources.LoadAll<TextAsset>(DataDirectory);
				foreach (TextAsset textAsset in textAssets)
				{
					T item = Serializer.Deserialize<T>(textAsset.text);
					string id = GetKey(item).ToString();
					if (textAsset.name.CompareTo(id) != 0)
					{
						DebugLog.Error("Object ID ({0}) does not match filename in {1}: {2}", id, DataDirectory, textAsset.name);
					}

					if (predicate == null || predicate(item))
					{
						items.Add(item);
					}
				}
				onCompleted(items);
			});
		}
        
		public override IAsyncTask GetByKeys(object[] keys)
        {
			Contract.ArgumentNotNull("keys", keys);

			return new AsyncTask((onCompleted, onError) =>
			{			
            	List<T> instances = new List<T>();
                foreach (object key in keys)
                {
                    TextAsset textAsset = Resources.Load<TextAsset>(GetResourceNameFromKey(key));
                    if (textAsset != null)
                    {                       
                        instances.Add(Serializer.Deserialize<T>(textAsset.text));
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
			Contract.ArgumentNotNull("key", key);

			return new AsyncTask((onCompleted, onError) =>
			{		
				T instance = default(T);	
				TextAsset textAsset = Resources.Load<TextAsset>(GetResourceNameFromKey(key));
				if (textAsset != null)
				{                       
					instance = Serializer.Deserialize<T>(textAsset.text);
				}
				onCompleted(instance);
			});
        }

        #endregion

        private string GetResourceNameFromKey(object key)
        {
            if (key == null)
            {
                throw new Exception(string.Format("Key property ({0}.{1}) is null",
                                                  typeof(T).Name,
                                                  KeyPropertyInfo.Name));
            }
            
            return PathCombine(DataDirectory, key.ToString());
        }        

        // Path.Combine() isn't available on the Unity WebPlayer
        private string PathCombine(string path1, string path2)
        {   
            if (String.IsNullOrEmpty(path2))
            {
                return path1;
            }

            if (String.IsNullOrEmpty(path1))
            {
                return path2;
            }

            return String.Format("{0}/{1}", path1, path2);
        }
    }
}
