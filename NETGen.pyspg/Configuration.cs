using System;
using System.Collections.Generic;

namespace NETGen.pyspg
{
	public enum ParamType { Const = 0, Variable = 1 }
	
	public class ConfigurationMgr
	{
		private static ConfigurationMgr _instance = null;
		private static Dictionary<string, Type> _paramTypes = null;		
		
		private ConfigurationMgr ()
		{
			_paramTypes = new Dictionary<string, Type>();
		}
		
		public static ConfigurationMgr Configuration
		{
			get {
				if(_instance == null)
					_instance = new ConfigurationMgr();
				return _instance;
			}
		}
		
		/// <summary>
		/// Registers the type of a parameter
		/// </summary>
		/// <param name='name'>
		/// Name.
		/// </param>
		/// <param name='t'>
		/// T.
		/// </param>
		public static void RegisterParam(string name, Type t, ParamType paramType = ParamType.Variable, object value = null)
		{
			_paramTypes[name] = t;
		}
		
		public object this[string param_name]
		{
			get {
				throw new NotImplementedException();
				return null;
			}
			set {
				
			}
		}
	}
}

