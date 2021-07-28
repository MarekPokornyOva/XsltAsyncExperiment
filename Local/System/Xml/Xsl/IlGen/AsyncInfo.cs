#region using
using System.Collections.Generic;
using System.Reflection.Emit;
#endregion using

namespace System.Xml.Xsl.IlGen
{
	class AsyncInfo
	{
		internal AsyncInfo(FieldBuilder stateFb,MethodBuilder moveNextMb,TypeBuilder helperTb,FieldBuilder[] parmFieldBuilders,FieldBuilder builderFb)
		{
			StateFb=stateFb;
			MoveNextMb=moveNextMb;
			HelperTb=helperTb;
			ParmFieldBuilders=parmFieldBuilders;
			BuilderFb=builderFb;
		}

		internal List<LocalBuilder> AwaiterLbS { get; } = new List<LocalBuilder>();
		internal List<FieldBuilder> AwaiterFbS { get; } = new List<FieldBuilder>();
		internal LocalBuilder? LastAwaiterLb { get; set; }
		internal FieldBuilder? LastAwaiterFb { get; set; }
		internal FieldBuilder StateFb { get; }
		internal MethodBuilder MoveNextMb { get; }
		internal TypeBuilder HelperTb { get; }
		internal FieldBuilder[] ParmFieldBuilders { get; }
		internal FieldBuilder BuilderFb { get; }
		internal int MoveNextPart { get; set; }
		internal List<Label> MoveNextParts { get; } = new List<Label>();
	}
}
