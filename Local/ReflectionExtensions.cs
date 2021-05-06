using System.Reflection;

namespace System.Xml
{
	static class ReflectionExtensions
	{
		internal static MethodInfo? GetMethod(this Type type,string name,BindingFlags flags, Type[] parameterTypes)
			=> type.GetMethod(name,flags,null,parameterTypes,null);

		internal static ConstructorInfo? GetConstructor(this Type type,BindingFlags flags,Type[] parameterTypes)
			=> type.GetConstructor(flags,null,parameterTypes,null);
	}
}
