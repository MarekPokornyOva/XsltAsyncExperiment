#region using
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
#endregion using

namespace System.Xml.Xsl.IlGen
{
	class AsyncILGenerator
	{
		List<Action<ILGenerator>> _instructions = new List<Action<ILGenerator>>();

		readonly ILGenerator _inner;
		internal AsyncILGenerator(ILGenerator inner)
		{
			_inner = inner;
		}

		public LocalBuilder DeclareLocal(Type localType)
			=> _inner.DeclareLocal(localType);
		public LocalBuilder DeclareLocal(Type localType, bool pinned)
			=> _inner.DeclareLocal(localType,pinned);
		public Label DefineLabel()
			=> _inner.DefineLabel();
		public void BeginScope()
			=> _instructions.Add(ilgen=>ilgen.BeginScope());
		public void EndScope()
			=> _instructions.Add(ilgen=>ilgen.EndScope());
		public void MarkLabel(Label loc)
			=> _instructions.Add(ilgen=>ilgen.MarkLabel(loc));
		public void Emit(OpCode opcode, Type cls)
			=> _instructions.Add(ilgen=>ilgen.Emit(opcode,cls));
		public void Emit(OpCode opcode, string str)
			=> _instructions.Add(ilgen=>ilgen.Emit(opcode,str));
		public void Emit(OpCode opcode, float arg)
			=> _instructions.Add(ilgen=>ilgen.Emit(opcode,arg));
		public void Emit(OpCode opcode, sbyte arg)
			=> _instructions.Add(ilgen=>ilgen.Emit(opcode,arg));
		public void Emit(OpCode opcode, MethodInfo meth)
			=> _instructions.Add(ilgen=>ilgen.Emit(opcode,meth));
		public void Emit(OpCode opcode, FieldInfo field)
			=> _instructions.Add(ilgen=>ilgen.Emit(opcode,field));
		public void Emit(OpCode opcode, Label[] labels)
			=> _instructions.Add(ilgen=>ilgen.Emit(opcode,labels));
		public void Emit(OpCode opcode, SignatureHelper signature)
			=> _instructions.Add(ilgen=>ilgen.Emit(opcode,signature));
		public void Emit(OpCode opcode, LocalBuilder local)
			=> _instructions.Add(ilgen=>ilgen.Emit(opcode,local));
		public void Emit(OpCode opcode, ConstructorInfo con)
			=> _instructions.Add(ilgen=>ilgen.Emit(opcode,con));
		public void Emit(OpCode opcode, long arg)
			=> _instructions.Add(ilgen=>ilgen.Emit(opcode,arg));
		public void Emit(OpCode opcode, int arg)
			=> _instructions.Add(ilgen=>ilgen.Emit(opcode,arg));
		public void Emit(OpCode opcode, short arg)
			=> _instructions.Add(ilgen=>ilgen.Emit(opcode,arg));
		public void Emit(OpCode opcode, double arg)
			=> _instructions.Add(ilgen=>ilgen.Emit(opcode,arg));
		public void Emit(OpCode opcode, byte arg)
			=> _instructions.Add(ilgen=>ilgen.Emit(opcode,arg));
		public void Emit(OpCode opcode)
			=> _instructions.Add(ilgen=>ilgen.Emit(opcode));
		public void Emit(OpCode opcode, Label label)
			=> _instructions.Add(ilgen=>ilgen.Emit(opcode,label));

		public void Insert(Action<ILGenerator> codeGen)
			=> _instructions.Insert(0,codeGen);

		public void Bake()
		{
			foreach (Action<ILGenerator> instr in _instructions)
				instr(_inner);
		}
	}
}
