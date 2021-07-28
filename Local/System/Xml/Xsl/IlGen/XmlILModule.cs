// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;
using System.Xml.Xsl.Runtime;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Collections.Generic;

namespace System.Xml.Xsl.IlGen
{
    using DebuggingModes = DebuggableAttribute.DebuggingModes;

    internal enum XmlILMethodAttributes
    {
        None = 0,
        NonUser = 1,    // Non-user method which should debugger should step through
        Raw = 2,        // Raw method which should not add an implicit first argument of type XmlQueryRuntime
    }

    internal sealed class XmlILModule
    {
        private static long s_assemblyId;                                     // Unique identifier used to ensure that assembly names are unique within AppDomain
        private static readonly ModuleBuilder s_LREModule = CreateLREModule();         // Module used to emit dynamic lightweight-reflection-emit (LRE) methods

        private TypeBuilder? _typeBldr;
        private Hashtable _methods;
        private readonly bool _useLRE, _emitSymbols;
      List<TypeBuilder> _nestedTypes = new List<TypeBuilder>();

        private const string RuntimeName = "{" + XmlReservedNs.NsXslDebug + "}" + "runtime";

        private static ModuleBuilder CreateLREModule()
        {
            // 1. LRE assembly only needs to execute
            // 2. No temp files need be created
            // 3. Never allow assembly to Assert permissions
            AssemblyName asmName = CreateAssemblyName();
            AssemblyBuilder asmBldr = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);

            // Add custom attribute to assembly marking it as security transparent so that Assert will not be allowed
            // and link demands will be converted to full demands.
            asmBldr.SetCustomAttribute(new CustomAttributeBuilder(XmlILConstructors.Transparent, Array.Empty<object>()));

            // Store LREModule once.  If multiple threads are doing this, then some threads might get different
            // modules.  This is OK, since it's not mandatory to share, just preferable.
            return asmBldr.DefineDynamicModule("System.Xml.Xsl.CompiledQuery");
        }

        public XmlILModule(TypeBuilder typeBldr)
        {
            _typeBldr = typeBldr;

            _emitSymbols = false;
            _useLRE = false;
            // Index all methods added to this module by unique name
            _methods = new Hashtable();
        }

        public bool EmitSymbols
        {
            get
            {
                return _emitSymbols;
            }
        }

        // SxS note: AssemblyBuilder.DefineDynamicModule() below may be using name which is not SxS safe.
        // This file is written only for internal tracing/debugging purposes. In retail builds persistAsm
        // will be always false and the file should never be written. As a result it's fine just to suppress
        // the SxS warning.
        public XmlILModule(bool useLRE, bool emitSymbols)
        {
            AssemblyName asmName;
            AssemblyBuilder asmBldr;
            ModuleBuilder modBldr;
            Debug.Assert(!(useLRE && emitSymbols));

            _useLRE = useLRE;
            _emitSymbols = emitSymbols;

            // Index all methods added to this module by unique name
            _methods = new Hashtable();

            if (!useLRE)
            {
                // 1. If assembly needs to support debugging, then it must be saved and re-loaded (rule of CLR)
                // 2. Get path of temp directory, where assembly will be saved
                // 3. Never allow assembly to Assert permissions
                asmName = CreateAssemblyName();

                asmBldr = AssemblyBuilder.DefineDynamicAssembly(
                            asmName, AssemblyBuilderAccess.Run);

                // Add custom attribute to assembly marking it as security transparent so that Assert will not be allowed
                // and link demands will be converted to full demands.
                asmBldr.SetCustomAttribute(new CustomAttributeBuilder(XmlILConstructors.Transparent, Array.Empty<object>()));

                if (emitSymbols)
                {
                    // Add DebuggableAttribute to assembly so that debugging is a better experience
                    DebuggingModes debuggingModes = DebuggingModes.Default | DebuggingModes.IgnoreSymbolStoreSequencePoints | DebuggingModes.DisableOptimizations;
                    asmBldr.SetCustomAttribute(new CustomAttributeBuilder(XmlILConstructors.Debuggable, new object[] { debuggingModes }));
                }

                // Create ModuleBuilder
                modBldr = asmBldr.DefineDynamicModule("System.Xml.Xsl.CompiledQuery");

                _typeBldr = modBldr.DefineType("System.Xml.Xsl.CompiledQuery.Query", TypeAttributes.Public);
            }
        }

        /// <summary>
        /// Define a method in this module with the specified name and parameters.
        /// </summary>
        public (MethodInfo ToBeGenerated, MethodInfo ToBeCalled, AsyncInfo? AsyncInfo) DefineMethod(string name, Type returnType, Type[] paramTypes, string?[] paramNames, XmlILMethodAttributes xmlAttrs)
        {
            (MethodInfo ToBeGenerated, MethodInfo ToBeCalled, AsyncInfo? AsyncInfo) methResult;
            int uniqueId = 1;
            string nameOrig = name;
            Type[] paramTypesNew;
            bool isRaw = (xmlAttrs & XmlILMethodAttributes.Raw) != 0;

            // Ensure that name is unique
            while (_methods[name] != null)
            {
                // Add unique id to end of name in order to make it unique within this module
                uniqueId++;
                name = nameOrig + " (" + uniqueId + ")";
            }

            if (!isRaw)
            {
                // XmlQueryRuntime is always 0th parameter
                paramTypesNew = new Type[paramTypes.Length + 2];
                paramTypesNew[0] = typeof(XmlQueryRuntime);
                paramTypesNew[1] = typeof(CancellationToken);
                Array.Copy(paramTypes, 0, paramTypesNew, 2, paramTypes.Length);
                paramTypes = paramTypesNew;

         if (returnType==typeof(void))
            returnType=typeof(ValueTask);
         else
            returnType=typeof(ValueTask<>).MakeGenericType(returnType);
            }

            if (!_useLRE)
            {
                MethodBuilder methBldr = _typeBldr!.DefineMethod(
                            name,
                            MethodAttributes.Private | MethodAttributes.Static,
                            returnType,
                            paramTypes);

                if (_emitSymbols && (xmlAttrs & XmlILMethodAttributes.NonUser) != 0)
                {
                    // Add DebuggerStepThroughAttribute and DebuggerNonUserCodeAttribute to non-user methods so that debugging is a better experience
                    methBldr.SetCustomAttribute(new CustomAttributeBuilder(XmlILConstructors.StepThrough, Array.Empty<object>()));
                    methBldr.SetCustomAttribute(new CustomAttributeBuilder(XmlILConstructors.NonUserCode, Array.Empty<object>()));
                }

                if (!isRaw)
                { 
                    methBldr.DefineParameter(1, ParameterAttributes.None, RuntimeName);
                    methBldr.DefineParameter(2, ParameterAttributes.None, "cancellationToken");
                }

                for (int i = 0; i < paramNames.Length; i++)
                {
                    if (paramNames[i] != null && paramNames[i]!.Length != 0)
                        methBldr.DefineParameter(i + (isRaw ? 1 : 3), ParameterAttributes.None, paramNames[i]);
                }

            if (isRaw)
               methResult=(methBldr, methBldr,null);
            else
            {
               (FieldBuilder[] ParmFieldBuilders, MethodBuilder BusinessMb, TypeBuilder TypeBuilder, MethodBuilder StartMb, AsyncInfo MoveNextInfo) asyncHelperInfo = CreateAsyncHelper(name,returnType,paramTypes,paramNames);
               ILGenerator gen = methBldr.GetILGenerator()!;
               Label codeBegin = gen.DefineLabel();
               Label methodEndLabel = gen.DefineLabel();
               int cancellationTokenParmIndex = 1;
               gen.Emit(OpCodes.Ldarga_S,cancellationTokenParmIndex);
               gen.Emit(OpCodes.Call,typeof(CancellationToken).GetProperty(nameof(CancellationToken.IsCancellationRequested))!.GetMethod!);
               gen.Emit(OpCodes.Brfalse,codeBegin);
               gen.Emit(OpCodes.Ldarg,cancellationTokenParmIndex);
               MethodInfo fromCancelledMi = methBldr.ReturnType==typeof(ValueTask)
                  ? typeof(ValueTask).GetMethod(nameof(ValueTask.FromCanceled),0,new[] { typeof(CancellationToken) })!
                  : typeof(ValueTask).GetMethod(nameof(ValueTask.FromCanceled),1,new[] { typeof(CancellationToken) })!.MakeGenericMethod(methBldr.ReturnType.GenericTypeArguments[0]);
               gen.Emit(OpCodes.Call,fromCancelledMi);
               gen.Emit(OpCodes.Br,methodEndLabel);
               gen.MarkLabel(codeBegin);
               //save params to helper's fields
               gen.DeclareLocal(asyncHelperInfo.TypeBuilder);
               int a = 0;
               foreach (Type paramType in paramTypes)
               {
                  gen.Emit(OpCodes.Ldloca_S,0);
                  gen.Emit(OpCodes.Ldarg,a);
                  gen.Emit(OpCodes.Stfld,asyncHelperInfo.ParmFieldBuilders[a]);
                  a++;
               }
               ////helper._builder=AsyncValueTaskMethodBuilder.Create(); - no need to initialize default struct
               //gen.Emit(OpCodes.Ldloca_S,0);
               //gen.Emit(OpCodes.Call,builderFb.FieldType.GetMethod(nameof(AsyncValueTaskMethodBuilder.Create))!);
               //gen.Emit(OpCodes.Stfld,builderFb);
               //helper._state=-1
               gen.Emit(OpCodes.Ldloca_S,0);
               gen.Emit(OpCodes.Ldc_I4_M1);
               gen.Emit(OpCodes.Stfld,asyncHelperInfo.MoveNextInfo.StateFb);
               //helper.Start();
               gen.Emit(OpCodes.Ldloca_S,0);
               gen.Emit(OpCodes.Call,asyncHelperInfo.StartMb);
               gen.MarkLabel(methodEndLabel);
               gen.Emit(OpCodes.Ret);

               methResult=(asyncHelperInfo.BusinessMb, methBldr, asyncHelperInfo.MoveNextInfo);
            }
            }
            else
            {
                DynamicMethod methDyn = new DynamicMethod(name, returnType, paramTypes, s_LREModule);
                methDyn.InitLocals = true;

                methResult = (methDyn, methDyn, null);
            }

            // Index method by name
            _methods[name] = methResult;
            return methResult;
        }

        /// <summary>
        /// Get an XmlILGenerator that can be used to generate the body of the specified method.
        /// </summary>
        public static ILGenerator DefineMethodBody(MethodBase methInfo)
        {
            DynamicMethod? methDyn = methInfo as DynamicMethod;
            if (methDyn != null)
                return methDyn.GetILGenerator();

            MethodBuilder? methBldr = methInfo as MethodBuilder;
            if (methBldr != null)
                return methBldr.GetILGenerator();

            return ((ConstructorBuilder)methInfo).GetILGenerator();
        }

        /// <summary>
        /// Find a MethodInfo of the specified name and return it.  Return null if no such method exists.
        /// </summary>
        public MethodInfo? FindMethod(string name)
        {
            return (MethodInfo?)_methods[name];
        }

        /// <summary>
        /// Define ginitialized data field with the specified name and value.
        /// </summary>
        public FieldInfo DefineInitializedData(string name, byte[] data)
        {
            Debug.Assert(!_useLRE, "Cannot create initialized data for an LRE module");
            return _typeBldr!.DefineInitializedData(name, data, FieldAttributes.Private | FieldAttributes.Static);
        }

        /// <summary>
        /// Define private static field with the specified name and value.
        /// </summary>
        public FieldInfo DefineField(string fieldName, Type type)
        {
            Debug.Assert(!_useLRE, "Cannot create field for an LRE module");
            return _typeBldr!.DefineField(fieldName, type, FieldAttributes.Private | FieldAttributes.Static);
        }

        /// <summary>
        /// Define static constructor for this type.
        /// </summary>
        public ConstructorInfo DefineTypeInitializer()
        {
            Debug.Assert(!_useLRE, "Cannot create type initializer for an LRE module");
            return _typeBldr!.DefineTypeInitializer();
        }

        /// <summary>
        /// Once all methods have been defined, CreateModule must be called in order to "bake" the methods within
        /// this module.
        /// </summary>
        public void BakeMethods()
        {
            Type typBaked;
            Hashtable methodsBaked;

            if (!_useLRE)
            {
            foreach (TypeBuilder tb in _nestedTypes)
               tb.CreateTypeInfo()!.AsType();
            _nestedTypes.Clear();

                typBaked = _typeBldr!.CreateTypeInfo()!.AsType();

                // Replace all MethodInfos in this.methods
                methodsBaked = new Hashtable(_methods.Count);
                foreach (string methName in _methods.Keys)
                {
                    methodsBaked[methName] = typBaked.GetMethod(methName, BindingFlags.NonPublic | BindingFlags.Static);
                }
                _methods = methodsBaked;

                // Release TypeBuilder and symbol writer resources
                _typeBldr = null;
            }
        }

        /// <summary>
        /// Wrap a delegate around a MethodInfo of the specified name and type and return it.
        /// </summary>
        public Delegate CreateDelegate(string name, Type typDelegate)
        {
            if (!_useLRE)
                return ((MethodInfo)_methods[name]!).CreateDelegate(typDelegate);

            return ((DynamicMethod)_methods[name]!).CreateDelegate(typDelegate);
        }

        /// <summary>
        /// Define unique assembly name (within AppDomain).
        /// </summary>
        private static AssemblyName CreateAssemblyName()
        {
            AssemblyName name;

            System.Threading.Interlocked.Increment(ref s_assemblyId);
            name = new AssemblyName();
            name.Name = "System.Xml.Xsl.CompiledQuery." + s_assemblyId;

            return name;
        }


      (FieldBuilder[] ParmFieldBuilders, MethodBuilder BusinessMb, TypeBuilder Type, MethodBuilder StartMb, AsyncInfo MoveNextInfo) CreateAsyncHelper(string name,Type returnType,Type[] paramTypes,string?[] paramNames)
		{
         TypeBuilder helperTb = _typeBldr!.DefineNestedType($"~{name}::helper",TypeAttributes.NestedPrivate|TypeAttributes.AutoClass|TypeAttributes.AnsiClass|TypeAttributes.Sealed|TypeAttributes.BeforeFieldInit,typeof(ValueType),new[] { typeof(IAsyncStateMachine) });
         this._nestedTypes.Add(helperTb);

         void Log(MethodBuilder meth,ILGenerator gen,string text)
         {
            gen.Emit(OpCodes.Ldstr,$"{name}-{meth.Name}:{text}");
            gen.Emit(OpCodes.Call,typeof(Console).GetMethod(nameof(Console.WriteLine),BindingFlags.Public|BindingFlags.Static,new[] { typeof(string) })!);
         }

         Type? returnTypeRaw = returnType==typeof(ValueTask) ? default : returnType.GenericTypeArguments[0];
         FieldBuilder builderFb = helperTb.DefineField("_builder",returnTypeRaw==default ? typeof(AsyncValueTaskMethodBuilder) : typeof(AsyncValueTaskMethodBuilder<>).MakeGenericType(returnTypeRaw),FieldAttributes.Private);
         FieldBuilder stateFb = helperTb.DefineField("_state",typeof(int),FieldAttributes.Assembly);
         int a = -1;
         int b = -3;
         (Type Type, string Name)[] parms = paramTypes.Select(pType =>
         {
            a++;
            b++;
            return (pType, a switch
            {
               0 => RuntimeName,
               1 => "cancellationToken",
               _ => ((paramNames!=default)&&(paramNames.Length>b) ? paramNames![b] : null)??"parm"+a
            });
         }).ToArray();
         FieldBuilder[] fieldBuilders = parms.Select(x => helperTb.DefineField(x.Name,x.Type,FieldAttributes.Public)).ToArray();

         //void MoveNextExecutive() - this is the executive method
         MethodBuilder moveNextExecMb = helperTb.DefineMethod(name+"-MoveNextExecutive",MethodAttributes.Private|MethodAttributes.HideBySig,CallingConventions.HasThis);
         moveNextExecMb.SetReturnType(typeof(void));
         moveNextExecMb.SetParameters(Type.EmptyTypes);

         //void MoveNext()
         MethodBuilder moveNextMb = helperTb.DefineMethod("MoveNext", MethodAttributes.Private|MethodAttributes.Final|MethodAttributes.HideBySig|MethodAttributes.NewSlot|MethodAttributes.Virtual,CallingConventions.HasThis);
         moveNextMb.SetReturnType(typeof(void));
         moveNextMb.SetParameters(Type.EmptyTypes);
         ILGenerator gen = moveNextMb.GetILGenerator();
         //try
         gen.BeginExceptionBlock();
         Log(moveNextMb,gen,"Begin");
         gen.Emit(OpCodes.Ldarg_0);
         gen.Emit(OpCodes.Call,moveNextExecMb);
         Log(moveNextMb,gen,"End");
         //catch
         gen.BeginCatchBlock(typeof(Exception));
         LocalBuilder exLb = gen.DeclareLocal(typeof(Exception),false);
         gen.Emit(OpCodes.Stloc,exLb);
         gen.Emit(OpCodes.Ldarg_0);
         gen.Emit(OpCodes.Ldflda,builderFb);
         gen.Emit(OpCodes.Ldloc_S,exLb);
         gen.Emit(OpCodes.Call,builderFb.FieldType.GetMethod(nameof(AsyncValueTaskMethodBuilder.SetException))!);
         //Emit(OpCodes.Leave,_methEnd); - no needed; it's added automatically
         gen.EndExceptionBlock();
         gen.Emit(OpCodes.Ret);
         helperTb.DefineMethodOverride(moveNextMb, typeof(IAsyncStateMachine).GetMethod(nameof(IAsyncStateMachine.MoveNext))!);

         //Start
         MethodBuilder startMb = helperTb.DefineMethod("Start",MethodAttributes.Public|MethodAttributes.HideBySig,CallingConventions.HasThis);
         startMb.SetReturnType(returnType);
         startMb.SetParameters(Type.EmptyTypes);
         gen=startMb.GetILGenerator();
         Label skipMoveNextCall = gen.DefineLabel();
         //if (_state == -1)
         gen.Emit(OpCodes.Ldarg_0);
         gen.Emit(OpCodes.Ldfld,stateFb);
         gen.Emit(OpCodes.Ldc_I4_M1);
         gen.Emit(OpCodes.Bne_Un_S,skipMoveNextCall);
         //MoveNext();
         gen.Emit(OpCodes.Ldarg_0);
         gen.Emit(OpCodes.Call,moveNextMb);
         gen.MarkLabel(skipMoveNextCall);
         //return _builder.Task;
         gen.Emit(OpCodes.Ldarg_0);
         gen.Emit(OpCodes.Ldflda,builderFb);
         gen.Emit(OpCodes.Call,builderFb.FieldType.GetProperty(nameof(AsyncValueTaskMethodBuilder.Task))!.GetMethod!);
         gen.Emit(OpCodes.Ret);

         //void SetStateMachine
         MethodBuilder setStateMachineMb = helperTb.DefineMethod("SetStateMachine",MethodAttributes.Private|MethodAttributes.Final|MethodAttributes.HideBySig|MethodAttributes.NewSlot|MethodAttributes.Virtual,CallingConventions.HasThis);
         setStateMachineMb.SetReturnType(typeof(void));
         setStateMachineMb.SetParameters(new[] { typeof(IAsyncStateMachine) });
         gen = setStateMachineMb.GetILGenerator();
         //_builder.SetStateMachine(stateMachine);
         gen.Emit(OpCodes.Ldarg_0);
         gen.Emit(OpCodes.Ldflda, builderFb);
         gen.Emit(OpCodes.Ldarg_1);
         gen.Emit(OpCodes.Call, typeof(AsyncValueTaskMethodBuilder).GetMethod(nameof(AsyncValueTaskMethodBuilder.SetStateMachine), BindingFlags.Public | BindingFlags.Instance, new[] { typeof(IAsyncStateMachine) })!);
         gen.Emit(OpCodes.Ret);
         helperTb.DefineMethodOverride(setStateMachineMb, typeof(IAsyncStateMachine).GetMethod(nameof(IAsyncStateMachine.SetStateMachine))!);

         return (fieldBuilders, moveNextExecMb, helperTb, startMb, new AsyncInfo(stateFb,moveNextMb,helperTb,fieldBuilders,builderFb) { MoveNextPart=-1 });
      }
    }
}
