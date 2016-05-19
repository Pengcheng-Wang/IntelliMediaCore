//---------------------------------------------------------------------------------------
// Copyright 2016 North Carolina State University
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
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Reflection;

namespace IntelliMedia
{
	public class PropertyBinder<TObject>
	{
		public delegate void BindHandler(TObject viewModel);
		private List<BindHandler> binders = new List<BindHandler>();
		private List<BindHandler> unbinders = new List<BindHandler>();

		// When built for WebGL, the expression.Compile() call fails with this error:
		//   Unsupported internal call for IL2CPP:DynamicMethod::create_dynamic_method - System.Reflection.Emit is not supported.
		// Don't allow this overload for WebGL builds, use the string name for the property/field instead
		#if !UNITY_WEBGL
		public void Add<TProperty>(Expression<Func<TObject, BindableProperty<TProperty>>> expression, BindableProperty<TProperty>.ValueChangedHandler changedHandler)
		{
			Func<TObject, BindableProperty<TProperty>> getProperty = expression.Compile();

			binders.Add((TObject viewModel) => 
			{
				getProperty(viewModel).ValueChanged += changedHandler;
			});

			unbinders.Add((TObject viewModel) => 
			{
				getProperty(viewModel).ValueChanged -= changedHandler;
			});				
		}
		#endif

		public void Add<TProperty>(string name, BindableProperty<TProperty>.ValueChangedHandler changedHandler)
		{
			PropertyInfo propertyInfo =  null;

			FieldInfo fieldInfo = typeof(TObject).GetField(name, BindingFlags.Instance | BindingFlags.Public);
			if (fieldInfo == null)
			{
				propertyInfo = typeof(TObject).GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
				if (propertyInfo == null)
				{
					throw new Exception(String.Format("Unable to find bindable property '{0}.{1}'",
						typeof(TObject).Name,
						name));
				}
			}

			binders.Add((TObject viewModel) => 
			{
				GetPropertyValue<TProperty>(name, viewModel, fieldInfo, propertyInfo).ValueChanged += changedHandler;
			});

			unbinders.Add((TObject viewModel) => 
			{
				GetPropertyValue<TProperty>(name, viewModel, fieldInfo, propertyInfo).ValueChanged -= changedHandler;
			});				
		}

		private static BindableProperty<TProperty> GetPropertyValue<TProperty>(string name, TObject viewModel, FieldInfo fieldInfo, PropertyInfo propertyInfo)
		{
			object value = null;
			if (fieldInfo != null)
			{
				value = fieldInfo.GetValue (viewModel);
			}
			else
			{
				value = propertyInfo.GetValue (viewModel, null);
			}
			BindableProperty<TProperty> bindableProperty = value as BindableProperty<TProperty>;
			if (bindableProperty == null)
			{
				throw new Exception(String.Format("BindableProperty '{0}.{1}' value is null", typeof(TObject).Name, name));
			}

			return bindableProperty;
		}

		public void Add(BindHandler bind, BindHandler unbind)
		{
			binders.Add(bind);
			unbinders.Add(unbind);				
		}

		public void Bind(TObject vm)
		{
			DebugLog.Info("PropertyBinder.Bind: {0}", (vm != null ? vm.GetType().Name : "null"));
			if (vm != null)
			{
				foreach(BindHandler handler in binders)
				{
					handler(vm);
				}
			}
		}

		public void Unbind(TObject vm)
		{
			DebugLog.Info("PropertyBinder.Unbind: {0}", (vm != null ? vm.GetType().Name : "null"));
			if (vm != null)
			{
				foreach(BindHandler handler in unbinders)
				{
					handler(vm);
				}
			}
		}
	}
}
