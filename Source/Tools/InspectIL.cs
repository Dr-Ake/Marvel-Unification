using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
class InspectIL
{
    static readonly Dictionary<short,OpCode> Codes=new Dictionary<short,OpCode>();
    static void Main(string[] args)
    {
        string managed=Path.GetFullPath(args[0]);
        AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve+=(s,e)=>{
            var name=new AssemblyName(e.Name).Name+".dll";var path=Path.Combine(managed,name);
            return File.Exists(path)?Assembly.ReflectionOnlyLoadFrom(path):Assembly.ReflectionOnlyLoad(e.Name);
        };
        foreach(var f in typeof(OpCodes).GetFields())if(f.FieldType==typeof(OpCode)){var code=(OpCode)f.GetValue(null);Codes[code.Value]=code;}
        var assembly=Assembly.ReflectionOnlyLoadFrom(Path.Combine(managed,"Assembly-CSharp.dll"));
        for(int a=1;a<args.Length;a++)
        {
            var parts=args[a].Split(':');var type=assembly.GetType(parts[0]);
            foreach(var method in type.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static))
            {
                if(parts.Length>1&&method.Name!=parts[1])continue;Console.WriteLine("METHOD "+method);
                var body=method.GetMethodBody();if(body==null)continue;var bytes=body.GetILAsByteArray();int i=0;
                while(i<bytes.Length)
                {
                    int offset=i;short key=bytes[i++];if(key==254)key=(short)(0xfe00|bytes[i++]);var code=Codes[key];string operand="";int size=0;
                    switch(code.OperandType)
                    {
                        case OperandType.InlineNone:break;
                        case OperandType.ShortInlineI:case OperandType.ShortInlineBrTarget:case OperandType.ShortInlineVar:size=1;operand=bytes[i].ToString();break;
                        case OperandType.InlineVar:size=2;operand=BitConverter.ToUInt16(bytes,i).ToString();break;
                        case OperandType.InlineI8:case OperandType.InlineR:size=8;break;
                        case OperandType.InlineSwitch:size=4+4*BitConverter.ToInt32(bytes,i);break;
                        default:size=4;int token=BitConverter.ToInt32(bytes,i);operand=token.ToString();
                            try{if(code.OperandType==OperandType.InlineString)operand=method.Module.ResolveString(token);else if(code.OperandType==OperandType.InlineField||code.OperandType==OperandType.InlineMethod||code.OperandType==OperandType.InlineType||code.OperandType==OperandType.InlineTok){var member=method.Module.ResolveMember(token);operand=member.DeclaringType+"."+member;}}catch{}break;
                    }
                    i+=size;Console.WriteLine(offset.ToString("X4")+" "+code.Name+" "+operand);
                }
            }
        }
    }
}
