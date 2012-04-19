using System;

namespace NETGen.pyspg
{
	public enum ParameterType { Input = 0, Output = 1, OutputFile = 2 };
	
	[AttributeUsage(AttributeTargets.Field, Inherited = false)]
	public class ParameterAttribute : Attribute
	{				
		public string Comment { get; private set;}
		public ParameterType Type { get; private set;}
		public object DefaultValue { get; private set; }
		
		public ParameterAttribute(ParameterType type, string comment, object defaultValue)
		{
			Comment = comment;
			Type = type;
			DefaultValue = defaultValue;
		}
	}
}

