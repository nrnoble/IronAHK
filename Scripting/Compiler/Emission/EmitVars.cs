using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Reflection.Emit;
using System.Reflection;

namespace IronAHK.Scripting
{
    internal partial class MethodWriter
    {
        void EmitAssignStatement(CodeAssignStatement Assign, bool ForceTypes)
        {
            EmitAssignment(Assign.Left, Assign.Right, ForceTypes);
        }

        void EmitAssignment(CodeExpression Left, CodeExpression Right, bool ForceTypes)
        {
            Depth++;
            Debug("Emitting assignment statement");

            if(Left is CodeVariableReferenceExpression)
            {
                var Reference = Left as CodeVariableReferenceExpression;

                LocalBuilder Var;
                if(Locals.ContainsKey(Reference.VariableName))
                    Var = Locals[Reference.VariableName];
                else
                {
                    Var = Generator.DeclareLocal(typeof(int));
                    Locals.Add(Reference.VariableName, Var);
                }

                EmitExpression(Right, ForceTypes);
                Generator.Emit(OpCodes.Stloc, Var);
            }
            else throw new CompileException(Left, "Left hand is unassignable");

            Depth--;
        }

        void EmitDynamicName(CodeArrayCreateExpression Dynamic)
        {
            Depth++;
            Debug("Emitting dynamic name expression");
            // Bail if it's not a string, we only use it for that
            if(Dynamic.CreateType.BaseType != "System.String")
                throw new CompileException(Dynamic, "CodeArrayCreateExpression is only used for dynamic names. System.String only");

            Generator.Emit(OpCodes.Ldc_I4, Dynamic.Initializers.Count);
            Generator.Emit(OpCodes.Newarr, typeof(string));

            for(int i = 0; i < Dynamic.Initializers.Count; i++)
            {
                Generator.Emit(OpCodes.Dup);
                Generator.Emit(OpCodes.Ldc_I4, i);
                Type Generated = EmitExpression(Dynamic.Initializers[i]);
                ForceTopStack(Generated, typeof(string));

                Generator.Emit(OpCodes.Stelem_Ref);
            }
            Depth--;
        }

        string ResolveName(CodeVariableReferenceExpression Var)
        {
            const string sep = ".";
            if (IsEntryPoint)
                return string.Concat(sep, Var.VariableName);
            else
                return string.Concat(Member.Name, sep, Var.VariableName);
        }

        void ForceTopStack(Type Top, Type Wanted)
        {
            Depth++;
            if(Top != Wanted)
            {
                Debug("Forcing top stack "+Top+" to "+Wanted);
                if(Wanted == typeof(string))
                {
                    if(Top != typeof(object)) Generator.Emit(OpCodes.Box, Top);
                    Generator.Emit(OpCodes.Call, ForceString);
                }
                else if (Wanted == typeof(float))
                {
                    if (Top != typeof(object)) Generator.Emit(OpCodes.Box, Top);
                    Generator.Emit(OpCodes.Call, ForceFloat);
                }
                else if (Wanted == typeof(decimal))
                {
                    if (Top != typeof(object)) Generator.Emit(OpCodes.Box, Top);
                    Generator.Emit(OpCodes.Call, ForceDecimal);
                }
                else if (Wanted == typeof(long))
                {
                    if (Top != typeof(object)) Generator.Emit(OpCodes.Box, Top);
                    Generator.Emit(OpCodes.Call, ForceLong);
                }
                else if (Wanted == typeof(int))
                {
                    if (Top != typeof(object)) Generator.Emit(OpCodes.Box, Top);
                    Generator.Emit(OpCodes.Call, ForceInt);
                }
                else if (Wanted == typeof(bool))
                {
                    if (Top != typeof(object)) Generator.Emit(OpCodes.Box, Top);
                    Generator.Emit(OpCodes.Call, ForceBool);
                }
                else if (Wanted == typeof(object))
                {
                    if (Top != typeof(object)) Generator.Emit(OpCodes.Box, Top);
                }
                else
                {
                    Debug("WARNING: Can not force " + Wanted);
                }
            }
            Depth--;
        }
    }
}