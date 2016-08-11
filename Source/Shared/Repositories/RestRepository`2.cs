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
using System.Text.RegularExpressions;
using System.Collections.Generic;
using IntelliMedia.Utilities;

namespace IntelliMedia.Repositories
{
    public class RestRepository<T, R> : Repository<T> where T : class, new() where R : class, new()
    {
        private HttpClient httpClient = new HttpClient();
        private const int DefaultRetryCount = 3;
        private ISerializer Serializer { get; set; }
        
        private Uri restServerUri;
        public Uri RestServerUri 
        { 
            get { return restServerUri; } 
            private set { if (restServerUri != value) { restServerUri = value; OnRestServerUriChanged(); } }
        }

        private void OnRestServerUriChanged()
        {
            string path = string.Format("{0}/", typeof(T).Name.ToLower());

            CreateUri = new Uri(RestServerUri, path);
            ReadUri = new Uri(RestServerUri, path);
            UpdateUri = new Uri(new Uri(RestServerUri, path), "?method=put");
            DeleteUri = new Uri(RestServerUri, path);
        }
        
        protected Uri CreateUri { get; set; }
        protected Uri ReadUri { get; set; }
        protected Uri UpdateUri { get; set; }
        protected Uri DeleteUri { get; set; }

        public RestRepository(Uri restServer, ISerializer serializer = null)
        {
            Contract.ArgumentNotNull("restServer", restServer);

            RestServerUri = restServer;

            // Default to XML serializer
            Serializer = (serializer != null ? serializer : SerializerXml.Instance);
        }

        #region IRepository implementation

		public override IAsyncTask Insert(T instance)
        {
			Contract.ArgumentNotNull("instance", instance);

            string serializedObj = Serializer.Serialize<T>(instance);
			MimePart mimePart = new MimePart("SerializedObject", MimePart.ApplicationXml, serializedObj);
            
			return new AsyncTry(httpClient.Post(CreateUri, mimePart))
				.Then<string>((result) =>
				{
					if (string.IsNullOrEmpty(result))
					{
						throw new Exception("Empty response for: " + CreateUri.ToString());
					}

					RestResponse response = Serializer.Deserialize<R>(result) as RestResponse;
					return response.ToItem<T>();
				});
        }

		public override IAsyncTask Update(T instance)
        {
			Contract.ArgumentNotNull("instance", instance);

            string serializedObj = Serializer.Serialize<T>(instance);
            
			MimePart mimePart = new MimePart("SerializedObject", MimePart.ApplicationXml, serializedObj);
            
			return new AsyncTry(httpClient.Post(UpdateUri, mimePart))
				.Then<string>((result) =>
				{
					if (string.IsNullOrEmpty(result))
					{
						throw new Exception("Empty response for: " + UpdateUri.ToString());
					}

					RestResponse response = Serializer.Deserialize<R>(result) as RestResponse;
					return response.ToItem<T>();
				});			
        }

		public override IAsyncTask Delete(T instance)
        {
			Contract.ArgumentNotNull("instance", instance);

            throw new System.NotImplementedException ();
        }

		public override IAsyncTask Get(System.Func<T, bool> predicate)
        {
            throw new System.NotImplementedException ();
        }

		public override IAsyncTask GetByKeys(object[] keys)
		{
            return Get(new Uri(ReadUri, KeysToPath(keys)));
        }

		public override IAsyncTask GetByKey(object key)
        {
            return GetByKey(null, key);
        }

        protected static string KeysToPath(object[] keys)
        {
			Contract.ArgumentNotNull("keys", keys);

            return String.Join(",", Array.ConvertAll(keys, k => k.ToString()));
        }

		public IAsyncTask GetByKey(string path, object key)
        {
			Contract.ArgumentNotNull("key", key);

            Uri readPath = ReadUri;
            if (path != null)
            {
                readPath = new Uri(readPath, path);
            }

            Uri objReadUri = new Uri(readPath, key.ToString());

            return Get(objReadUri, true);
        }

		protected IAsyncTask Get(Uri uri, bool firstOrDefault = false)
        {
			Contract.ArgumentNotNull("uri", uri);

			return new AsyncTry(httpClient.Get(uri))
				.Then<string>((result) =>
				{
					if (string.IsNullOrEmpty(result))
					{
						throw new Exception("Empty response for: " + uri.ToString());
					}

					RestResponse response = Serializer.Deserialize<R>(result) as RestResponse;
					if (firstOrDefault)
					{
						return response.ToItem<T>();
					}
					else
					{
						return response.ToList<T>();
					}
				});				
        }

        #endregion
    }
}
