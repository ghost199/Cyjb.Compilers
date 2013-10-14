﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cyjb.Compiler.Lexer;
using Cyjb.Compiler.RegularExpressions;
using Cyjb.IO;
using Cyjb.Text;

namespace Cyjb.Compiler
{
	/// <summary>
	/// 表示词法分析或语法分析中使用的语法规则。
	/// </summary>
	/// <remarks>关于词法分析的相关信息，请参考我的系列博文
	/// <see href="http://www.cnblogs.com/cyjb/archive/p/LexerIntroduce.html">
	/// 《C# 词法分析器（一）词法分析介绍》</see>。</remarks>
	/// <example>下面简单的构造一个数学算式的词法分析器：
	/// <code>
	/// Grammar grammar = new Grammar();
	/// // 终结符的定义。
	/// grammar.DefineSymbol("digits", "[0-9]+", c => c.Accept(int.Parse(c.Text)));
	/// grammar.DefineSymbol("+", "\\+");
	/// grammar.DefineSymbol("-", "\\-");
	/// grammar.DefineSymbol("*", "\\*");
	/// grammar.DefineSymbol("/", "\\/");
	/// grammar.DefineSymbol("^", "\\^");
	/// grammar.DefineSymbol("(", "\\(");
	/// grammar.DefineSymbol(")", "\\)");
	/// // 吃掉所有空白。
	/// grammar.DefineSymbol("\\s", c => { });
	/// // 要分析的源文件。
	/// string source = "1 + 20 * 3 / 4*(5+6)";
	/// // 构造词法分析器。
	/// TokenReader reader = grammar.GetReader(source);
	/// while (true) {
	/// 	Token token = reader.ReadToken();
	/// 	Console.WriteLine(token);
	/// 	if (token.IsEndOfFile) {
	/// 		break;
	/// 	}
	/// }
	/// </code>
	/// </example>
	/// <seealso cref="LexerRule"/>
	/// <seealso href="http://www.cnblogs.com/cyjb/archive/p/LexerIntroduce.html">
	/// 《C# 词法分析器（一）词法分析介绍》</seealso>
	public sealed class Grammar
	{

		#region 词法分析器定义

		/// <summary>
		/// 初始的词法分析器上下文。
		/// </summary>
		public const string InitialContext = "Initial";
		/// <summary>
		/// 默认的接受动作。
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private static readonly Action<ReaderController> DefaultAccept = c => c.Accept();
		/// <summary>
		/// 正则表达式列表。
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly Dictionary<string, Regex> regexs = new Dictionary<string, Regex>();
		/// <summary>
		/// 终结符号列表。
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly SymbolCollection<TerminalSymbol> terminalSymbols = new SymbolCollection<TerminalSymbol>();
		/// <summary>
		/// 词法分析器上下文列表。
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly LexerContextCollection lexerContexts = new LexerContextCollection();
		/// <summary>
		/// 词法分析器的规则。
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private LexerRule lexerRule;
		/// <summary>
		/// 词法分析器的规则是否发生了改变。
		/// </summary>
		private bool lexerChanged = true;

		#endregion // 词法分析器定义

		/// <summary>
		/// 初始化 <see cref="Grammar"/> 类的新实例。
		/// </summary>
		public Grammar()
		{
			this.lexerContexts.DefineInclusiveContext(InitialContext);
		}

		#region 获取词法分析器

		/// <summary>
		/// 获取词法分析器上下文列表。
		/// </summary>
		/// <value>词法分析器的上下文列表。</value>
		internal LexerContextCollection Contexts
		{
			get { return lexerContexts; }
		}
		/// <summary>
		/// 获取定义的终结符号的集合。
		/// </summary>
		/// <value>所有定义的终结符号的集合。</value>
		internal SymbolCollection<TerminalSymbol> TerminalSymbols
		{
			get { return terminalSymbols; }
		}
		/// <summary>
		/// 获取识别当前语法规则的词法分析器规则。
		/// </summary>
		/// <value>识别当前语法规则的词法分析器规则。</value>
		public LexerRule LexerRule
		{
			get
			{
				if (lexerChanged)
				{
					this.lexerRule = new LexerRule(this);
				}
				return this.lexerRule;
			}
		}
		/// <summary>
		/// 返回指定源文件的词法单元读取器。
		/// </summary>
		/// <param name="source">要读取的源文件。</param>
		/// <returns>指定源文件的词法单元读取器。</returns>
		/// <overloads>
		/// <summary>
		/// 返回指定源文件的词法单元读取器。
		/// </summary>
		/// </overloads>
		public TokenReader GetReader(string source)
		{
			return LexerRule.GetReader(source);
		}
		/// <summary>
		/// 返回指定源文件的词法单元读取器。
		/// </summary>
		/// <param name="source">要读取的源文件。</param>
		/// <returns>指定源文件的词法单元读取器。</returns>
		public TokenReader GetReader(SourceReader source)
		{
			return LexerRule.GetReader(source);
		}
		/// <summary>
		/// 返回指定源文件的允许拒绝的词法单元读取器。
		/// </summary>
		/// <param name="source">要读取的源文件。</param>
		/// <returns>指定源文件的词法单元读取器。</returns>
		/// <overloads>
		/// <summary>
		/// 返回指定源文件的允许拒绝的词法单元读取器。
		/// </summary>
		/// </overloads>
		public TokenReader GetRejectableReader(string source)
		{
			return LexerRule.GetRejectableReader(source);
		}
		/// <summary>
		/// 返回指定源文件的允许拒绝的词法单元读取器。
		/// </summary>
		/// <param name="source">要读取的源文件。</param>
		/// <returns>指定源文件的词法单元读取器。</returns>
		public TokenReader GetRejectableReader(SourceReader source)
		{
			return LexerRule.GetRejectableReader(source);
		}

		#endregion // 获取词法分析器

		#region TerminalSymbol

		#region Regex 字符串

		/// <summary>
		/// 定义使用默认接受动作的终结符。
		/// </summary>
		/// <param name="id">终结符的标识符，不能包含空白或者与 <see cref="Token"/> 
		/// 结构中定义的特殊标识符相同。</param>
		/// <param name="regex">终结符对应的正则表达式。</param>
		/// <returns>定义的终结符。</returns>
		/// <exception cref="System.ArgumentException">标识符包含空白，或者与 <see cref="Token"/> 
		/// 结构中定义的特殊标识符相同。</exception>
		/// <overloads>
		/// <summary>
		/// 定义终结符。
		/// </summary>
		/// </overloads>
		public void DefineSymbol(string id, string regex)
		{
			CheckId(id);
			ExceptionHelper.CheckArgumentNull(regex, "regex");
			InternalDefineSymbol(id, regex, DefaultAccept);
		}
		/// <summary>
		/// 定义具有指定正则表达式和动作的终结符。
		/// </summary>
		/// <param name="regex">终结符对应的正则表达式。</param>
		/// <param name="action">终结符的动作。</param>
		public void DefineSymbol(string regex, Action<ReaderController> action)
		{
			ExceptionHelper.CheckArgumentNull(regex, "regex");
			InternalDefineSymbol(null, regex, action);
		}
		/// <summary>
		/// 定义具有指定正则表达式和动作的终结符。
		/// </summary>
		/// <param name="id">终结符的标识符，不能包含空白或者与 <see cref="Token"/> 
		/// 结构中定义的特殊标识符相同。</param>
		/// <param name="regex">终结符对应的正则表达式。</param>
		/// <param name="action">终结符的动作。</param>
		/// <exception cref="System.ArgumentException">标识符包含空白，或者与 <see cref="Token"/> 
		/// 结构中定义的特殊标识符相同。</exception>
		public void DefineSymbol(string id, string regex, Action<ReaderController> action)
		{
			CheckId(id);
			ExceptionHelper.CheckArgumentNull(regex, "regex");
			InternalDefineSymbol(id, regex, action);
		}

		#endregion // Regex 字符串

		#region Regex 对象

		/// <summary>
		/// 定义一个使用默认接受动作的终结符。
		/// </summary>
		/// <param name="id">终结符的标识符，不能包含空白或者与 <see cref="Token"/> 
		/// 结构中定义的特殊标识符相同。</param>
		/// <param name="regex">终结符对应的正则表达式。</param>
		/// <exception cref="System.ArgumentException">标识符包含空白，或者与 <see cref="Token"/> 
		/// 结构中定义的特殊标识符相同。</exception>
		public void DefineSymbol(string id, Regex regex)
		{
			InternalDefineSymbol(id, regex, DefaultAccept, null);
		}
		/// <summary>
		/// 在指定上下文中定义使用默认接受动作的终结符。
		/// </summary>
		/// <param name="id">终结符的标识符，不能包含空白或者与 <see cref="Token"/> 
		/// 结构中定义的特殊标识符相同。</param>
		/// <param name="regex">终结符对应的正则表达式。</param>
		/// <param name="contexts">定义终结符的上下文。</param>
		/// <exception cref="System.ArgumentException">标识符包含空白，或者与 <see cref="Token"/> 
		/// 结构中定义的特殊标识符相同。</exception>
		public void DefineSymbol(string id, Regex regex, IEnumerable<string> contexts)
		{
			InternalDefineSymbol(id, regex, DefaultAccept, contexts);
		}

		#endregion // Regex 对象

		#region Regex 对象 + Action

		/// <summary>
		/// 定义具有指定正则表达式和动作的终结符。
		/// </summary>
		/// <param name="regex">终结符对应的正则表达式。</param>
		/// <param name="action">终结符的动作。</param>
		/// <returns>定义的终结符。</returns>
		public void DefineSymbol(Regex regex, Action<ReaderController> action)
		{
			InternalDefineSymbol(null, regex, action, null);
		}
		/// <summary>
		/// 定义具有指定正则表达式和动作的终结符。
		/// </summary>
		/// <param name="id">终结符的标识符，不能包含空白或者与 <see cref="Token"/> 
		/// 结构中定义的特殊标识符相同。</param>
		/// <param name="regex">终结符对应的正则表达式。</param>
		/// <param name="action">终结符的动作。</param>
		/// <returns>定义的终结符。</returns>
		/// <exception cref="System.ArgumentException">标识符包含空白，或者与 <see cref="Token"/> 
		/// 结构中定义的特殊标识符相同。</exception>
		public void DefineSymbol(string id, Regex regex, Action<ReaderController> action)
		{
			CheckId(id);
			InternalDefineSymbol(id, regex, action, null);
		}
		/// <summary>
		/// 在指定上下文中定义具有指定正则表达式和动作的终结符。
		/// </summary>
		/// <param name="regex">终结符对应的正则表达式。</param>
		/// <param name="action">终结符的动作。</param>
		/// <param name="contexts">定义终结符的上下文。</param>
		public void DefineSymbol(Regex regex, Action<ReaderController> action,
			IEnumerable<string> contexts)
		{
			InternalDefineSymbol(null, regex, action, contexts);
		}
		/// <summary>
		/// 在指定上下文中定义具有指定正则表达式和动作的终结符。
		/// </summary>
		/// <param name="id">终结符的标识符，不能包含空白或者与 <see cref="Token"/> 
		/// 结构中定义的特殊标识符相同。</param>
		/// <param name="regex">终结符对应的正则表达式。</param>
		/// <param name="action">终结符的动作。</param>
		/// <param name="contexts">定义终结符的上下文。</param>
		/// <exception cref="System.ArgumentException">标识符包含空白，或者与 <see cref="Token"/> 
		/// 结构中定义的特殊标识符相同。</exception>
		public void DefineSymbol(string id, Regex regex, Action<ReaderController> action,
			IEnumerable<string> contexts)
		{
			CheckId(id);
			InternalDefineSymbol(id, regex, action, contexts);
		}

		#endregion // Regex 对象 + Action

		/// <summary>
		/// 定义一个终结符。
		/// </summary>
		/// <param name="id">终结符的标识符。</param>
		/// <param name="regex">终结符对应的正则表达式。</param>
		/// <param name="action">终结符的动作。</param>
		private void InternalDefineSymbol(string id, string regex, Action<ReaderController> action)
		{
			IEnumerable<string> context = null;
			if (regex[0] == '<' && regex.IndexOf("<<EOF>>", StringComparison.OrdinalIgnoreCase) != 0)
			{
				// 处理上下文。
				int idx = regex.IndexOf('>');
				if (idx == -1)
				{
					throw CompilerExceptionHelper.IncompleteLexerContext("regex");
				}
				string contextStr = regex.Substring(1, idx - 1);
				regex = regex.Substring(idx + 1);
				if (contextStr != "*")
				{
					context = contextStr.Split(',');
				}
			}
			InternalDefineSymbol(id, RegexParser.ParseRegex(regex, RegexOptions.None, this.regexs), action, context);
		}
		/// <summary>
		/// 定义一个终结符。
		/// </summary>
		/// <param name="id">终结符的标识符。</param>
		/// <param name="regex">终结符对应的正则表达式。</param>
		/// <param name="action">终结符的动作。</param>
		/// <param name="contexts">定义终结符的上下文。</param>
		private void InternalDefineSymbol(string id, Regex regex, Action<ReaderController> action,
			IEnumerable<string> contexts)
		{
			ExceptionHelper.CheckArgumentNull(regex, "regex");
			TerminalSymbol symbol = new TerminalSymbol(id, terminalSymbols.Count, regex, action);
			if (contexts == null || !contexts.Any())
			{
				// 添加所有包含型上下文。
				symbol.Context.UnionWith(
					this.lexerContexts.Where(c => c.ContextType == LexerContextType.Inclusive));
			}
			else
			{
				symbol.Context.UnionWith(CheckContexts(contexts));
			}
			terminalSymbols.InternalAdd(symbol);
			lexerChanged = true;
		}
		/// <summary>
		/// 检查给定的上下文是否是有效的。
		/// </summary>
		/// <param name="contexts">要检查的上下文。</param>
		/// <returns>要检查的上下文。</returns>
		private IEnumerable<LexerContext> CheckContexts(IEnumerable<string> contexts)
		{
			foreach (string label in contexts)
			{
				if (label != null)
				{
					string tmpLabel = label.Trim();
					LexerContext context;
					if (!this.lexerContexts.TryGetItem(tmpLabel, out context))
					{
						throw CompilerExceptionHelper.InvalidLexerContext("contexts", tmpLabel);
					}
					yield return context;
				}
			}
		}

		#endregion // TerminalSymbol

		#region 正则表达式

		/// <summary>
		/// 定义一个指定名称的正则表达式。
		/// </summary>
		/// <param name="name">正则表达式的名称。</param>
		/// <param name="regex">定义的正则表达式。</param>
		/// <overloads>
		/// <summary>
		/// 定义一个指定名称的正则表达式。
		/// </summary>
		/// </overloads>
		public void DefineRegex(string name, Regex regex)
		{
			regexs[name] = regex;
		}
		/// <summary>
		/// 定义一个指定名称的正则表达式。
		/// </summary>
		/// <param name="name">正则表达式的名称。</param>
		/// <param name="regex">定义的正则表达式。</param>
		public void DefineRegex(string name, string regex)
		{
			regexs[name] = RegexParser.ParseRegex(regex, RegexOptions.None, regexs);
		}

		#endregion // 正则表达式

		#region 上下文

		/// <summary>
		/// 定义一个新的词法分析器的上下文。
		/// </summary>
		/// <param name="label">上下文的标签。</param>
		public void DefineContext(string label)
		{
			lexerContexts.DefineContext(label);
		}
		/// <summary>
		/// 定义一个新的词法分析器的包含型上下文。
		/// </summary>
		/// <param name="label">上下文的标签。</param>
		public void DefineInclusiveContext(string label)
		{
			lexerContexts.DefineInclusiveContext(label);
		}

		#endregion // 上下文

		/// <summary>
		/// 检查指定的符号标识符。
		/// </summary>
		/// <param name="id"></param>
		private static void CheckId(string id)
		{
			ExceptionHelper.CheckArgumentNull(id, "id");
			if (id == Token.EndOfFile)
			{
				CompilerExceptionHelper.InvalidSymbolId("id", id);
			}
			for (int i = 0; i < id.Length; i++)
			{
				if (char.IsWhiteSpace(id, i))
				{
					CompilerExceptionHelper.InvalidSymbolId("id", id);
				}
			}
		}
	}
}
