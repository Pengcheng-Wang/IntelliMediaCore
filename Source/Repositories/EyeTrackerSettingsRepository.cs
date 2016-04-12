#region Copyright 2015 North Carolina State University
//---------------------------------------------------------------------------------------
// Copyright 2015 North Carolina State University
//
// Computer Science Department
// Center for Educational Informatics
// http://www.cei.ncsu.edu/
//
// All rights reserved
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
#endregion

using System;
using UnityEngine;
using System.Collections.Generic;

namespace IntelliMedia
{ 
    public class EyeTrackerSettingsRepository : PlayerPrefsRepository<EyeTrackerSettings>
    {
        public readonly static string LocalSettingsId = "Local";

        public delegate void SettingsChangedHandler();

        public EyeTrackerSettingsRepository() : base()
        {
        }

        /// <summary>
        /// Create new settings object if it doesn't exist, or return existing settings.
        /// </summary>
        /// <param name="callback">Callback.</param>
        public void Get(ResponseHandler callback)
        {
            GetByKey(LocalSettingsId, (Response response) =>
            {
                if (response.Success)
                {
                    callback(response);
                }
                else
                {
                    EyeTrackerSettings settings = new EyeTrackerSettings() { Id = LocalSettingsId };
                    Insert(settings, callback);
                }
            });
        }                
    }
}
