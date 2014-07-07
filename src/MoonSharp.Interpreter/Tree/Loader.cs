﻿#define DEBUG_COMPILER

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Antlr4.Runtime;
using MoonSharp.Interpreter.Diagnostics;
using MoonSharp.Interpreter.Execution;
using MoonSharp.Interpreter.Execution.VM;
using MoonSharp.Interpreter.Grammar;
using MoonSharp.Interpreter.Tree.Expressions;
using MoonSharp.Interpreter.Tree.Statements;

namespace MoonSharp.Interpreter.Tree
{
	internal static class Loader
	{
		internal static int LoadChunkFromICharStream(ICharStream charStream, ByteCode bytecode, string sourceName, int sourceIdx)
		{
			LuaParser parser = CreateParser(charStream, sourceIdx);

			ScriptLoadingContext lcontext = CreateLoadingContext(sourceName, sourceIdx);
			ChunkStatement stat = new ChunkStatement(parser.chunk(), lcontext);

			int beginIp = -1;

			using (var _ = new CodeChrono("ChunkStatement.LoadFromICharStream/Compile"))
			{
				bytecode.Emit_Nop(string.Format("Begin chunk {0}", sourceName));
				beginIp = bytecode.GetJumpPointForLastInstruction();
				stat.Compile(bytecode);
				bytecode.Emit_Nop(string.Format("End chunk {0}", sourceName));

				Debug_DumpByteCode(bytecode, sourceIdx);
			}

			return beginIp;
		}

		internal static int LoadFunctionFromICharStream(ICharStream charStream, ByteCode bytecode, string sourceName, int sourceIdx)
		{
			LuaParser parser = CreateParser(charStream, sourceIdx);

			ScriptLoadingContext lcontext = CreateLoadingContext(sourceName, sourceIdx);
			FunctionDefinitionExpression fndef = new FunctionDefinitionExpression(parser.anonfunctiondef(), lcontext);

			int beginIp = -1;

			using (var _ = new CodeChrono("ChunkStatement.LoadFromICharStream/Compile"))
			{
				bytecode.Emit_Nop(string.Format("Begin function {0}", sourceName));
				beginIp = fndef.CompileBody(bytecode, sourceName);
				bytecode.Emit_Nop(string.Format("End function {0}", sourceName));

				Debug_DumpByteCode(bytecode, sourceIdx);
			}

			return beginIp;
		}


		[Conditional("DEBUG_COMPILER")]
		private static void Debug_DumpByteCode(ByteCode bytecode, int sourceIdx)
		{
			bytecode.Dump(string.Format(@"c:\temp\codedump_{0}.txt", sourceIdx));
		}

		[Conditional("DEBUG_COMPILER")]
		private static void Debug_DumpAst(LuaParser parser, int sourceIdx)
		{
			AstDump astDump = new AstDump();
			astDump.DumpTree(parser.chunk(), string.Format(@"c:\temp\treedump_{0:000}.txt", sourceIdx));
			parser.Reset();
		}


		private static ScriptLoadingContext CreateLoadingContext(string sourceName, int sourceIdx)
		{
			return new ScriptLoadingContext()
			{
				Scope = new BuildTimeScope(),
				SourceIdx = sourceIdx,
				SourceName = sourceName,
			};
		}

		private static LuaParser CreateParser(ICharStream charStream, int sourceIdx)
		{
			LuaLexer lexer;
			LuaParser parser;

			using (var _ = new CodeChrono("ChunkStatement.LoadFromICharStream/Parsing"))
			{
				lexer = new LuaLexer(charStream);
				parser = new LuaParser(new CommonTokenStream(lexer));
			}

			Debug_DumpAst(parser, sourceIdx);

			return parser;
		}

	}
}
