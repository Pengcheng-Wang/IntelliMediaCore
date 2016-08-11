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
using System.Collections.Generic;
using IntelliMedia.Models;
using IntelliMedia.Utilities;

namespace IntelliMedia.Repositories
{
    public class ActivityStateResponse : RestResponse
    {
        public List<ActivityState> Items { get; set; }
        
        #region implemented abstract members of RestResponse
        
        public override List<T> ToList<T> ()
        {
            return Items as List<T>;
        }
        
        public override T ToItem<T>()
        {
            if (Items.Count == 1)
            {
                return Items[0] as T;
            }
            
            return default(T);
        }
        
        #endregion
        
        public ActivityStateResponse() {}
        
        public ActivityStateResponse(List<ActivityState> items) 
        {
            Items = items;
        }
    }

    public class ActivityStateRepository : RestRepository<ActivityState, ActivityStateResponse>
    {
        public ActivityStateRepository(Uri restServer) : base(restServer)
        {
        }

		public IAsyncTask GetActivityStates(String studentId, IEnumerable<string> activityIds)
        {              
			Contract.ArgumentNotNull("studentId", studentId);
			Contract.ArgumentNotNull("activityIds", activityIds);

            string path = String.Format("studentid/{0}/activityids/{1}", 
			                            studentId, KeysToPath(activityIds.ToArray()));
            
            
            return Get(new Uri(ReadUri, path));
        }
    }
}
