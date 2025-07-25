﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Vanara.Generators;

namespace Vanara.PInvoke.Tests;

[TestFixture]
public class CodeGenTests
{
	[Test]
	public void AutoHandleTest()
	{
		const string src = """
		namespace Vanara.PInvoke
		{
			public static partial class Test32
			{
				/// <summary>Handle to a test.</summary>
				[Vanara.PInvoke.AutoHandleAttribute(typeof(Vanara.PInvoke.IGdiObjectHandle), typeof(HGDIOBJ))]
				public partial struct HPEN { }

				/// <summary>Handle to a sample.</summary>
				[Vanara.PInvoke.AutoHandleAttribute]
				public partial struct HSAMPLE
				{
					/// <summary>Get the integer value.</summary>
					public int Value => handle.ToInt32();
				}
			}
		}
		""";
		var compilation = GetCompilation(src);
		CreateGeneratorDriverAndRun(compilation, new AutoHandleGenerator(), null, out var output, out var diag);

		Assert.That(output.SyntaxTrees.Count(), Is.EqualTo(3));
		Assert.That(diag.Where(d => d.Severity == DiagnosticSeverity.Error), Is.Empty);

		WriteTrees(TestContext.Out, output.SyntaxTrees, false);
	}

	[Test]
	public void AutoSafeHandleTest()
	{
		const string src = """
		namespace Vanara.PInvoke
		{
			public static partial class Test32
			{
				/// <summary>Handle to a test.</summary>
				[Vanara.PInvoke.AutoSafeHandleAttribute("CloseTest(handle)", typeof(HTEST), typeof(Vanara.PInvoke.SafeHandleV), typeof(HANDLE))]
				public partial class SafeHTEST { }

				/// <summary>Handle to a sample.</summary>
				[Vanara.PInvoke.AutoSafeHandleAttribute(null, typeof(HSAMPLE))]
				public partial class SafeHSAMPLE
				{
					/// <summary>Get the integer value.</summary>
					public int Value => handle.ToInt32();
				}
			}
		}
		""";
		var compilation = GetCompilation(src);
		CreateGeneratorDriverAndRun(compilation, new AutoSafeHandleGenerator(), null, out var output, out var diag);

		Assert.That(output.SyntaxTrees.Count(), Is.EqualTo(3));
		Assert.That(diag.Where(d => d.Severity == DiagnosticSeverity.Error), Is.Empty);

		WriteTrees(TestContext.Out, output.SyntaxTrees, false);
	}

	[Test]
	public void HandlesFromFileTest()
	{
		var compilation = GetCompilation(@"// comment");
		CreateGeneratorDriverAndRun(compilation, new HandlesFromFileGenerator(), "handles.csv", out var output, out var diag);

		Assert.That(diag.Where(d => d.Severity == DiagnosticSeverity.Error), Is.Empty);

		WriteTrees(TestContext.Out, output.SyntaxTrees);
	}

	[TestCase("handlesbad1.csv", "VANGEN001")]
	[TestCase("handlesbad2.csv", "VANGEN002")]
	[TestCase("handlesbad3.csv", "VANGEN003")]
	[TestCase("handlesbad4.csv", "VANGEN003")]
	public void HandlesFromBadFileTest(string fn, string errId)
	{
		var compilation = GetCompilation(@"// comment");
		CreateGeneratorDriverAndRun(compilation, new HandlesFromFileGenerator(), fn, out var output, out var diag);
		Assert.That(diag.Where(d => d.Severity == DiagnosticSeverity.Error), Is.Not.Empty);
		Assert.That(diag[0].Id, Is.EqualTo(errId));
	}

	[Test]
	public void IUnkMethodGenTest()
	{
		const string src = @"
				namespace Vanara.PInvoke
				{
					public interface IUnkHolderIgnore
					{
						[PreserveSig]
						HRESULT Ignore(object? p1, in System.Guid p2, [System.Runtime.InteropServices.MarshalAs(UnmanagedType.IUnknown, IidParameterIndex = 1)] out object? p3);
					}
					public static partial class Test32
					{
						[System.Runtime.InteropServices.MarshalAs(UnmanagedType.Bool)]
						public static readonly bool field1;

						public interface IUnkHolder
						{
							/// <summary>Gets the object.</summary>
							/// <param name=""p1"">The p1.</param>
							/// <param name=""p2"">The p2.</param>
							/// <param name=""p3"">The p3.</param>
							/// <param name=""p4"">The p4.</param>
							/// <param name=""p5"">The p5.</param>
							/// <returns>The ret.</returns>
							[PreserveSig]
							HRESULT GetObj(object? p1, in System.Guid p2, [System.Runtime.InteropServices.MarshalAs(UnmanagedType.IUnknown, IidParameterIndex = 1)] out object? p3, ref NativeOverlapped p4, out long p5);
							/// <summary>Gets the obj2.</summary>
							/// <param name=""p1"">The p1.</param>
							/// <param name=""p2"">The p2.</param>
							/// <param name=""p3"">The p3.</param>
							void GetObj2(float p1, in System.Guid p2, [System.Runtime.InteropServices.MarshalAs(UnmanagedType.IUnknown, IidParameterIndex = 1)] out object? p3);
							HRESULT Ignore1(object? p1, in System.Guid p2, [System.Runtime.InteropServices.MarshalAs(UnmanagedType.IUnknown)] object? p3);
							HRESULT Ignore2(object? p1, in System.Guid p2, [System.Runtime.InteropServices.MarshalAs(UnmanagedType.IUnknown)] out object? p3);
							HRESULT Ignore3([System.Runtime.InteropServices.MarshalAs(UnmanagedType.LPArray)] int[] p3);
							[SuppressAutoGen]
							void Ignore4(float p1, in System.Guid p2, [System.Runtime.InteropServices.MarshalAs(UnmanagedType.IUnknown, IidParameterIndex = 1)] out object? p3);
						}

						/// <summary>Gets the object.</summary>
						/// <param name=""p1"">The p1.</param>
						/// <param name=""p2"">The p2.</param>
						/// <param name=""p3"">The p3.</param>
						/// <returns>The ret.</returns>
						[System.Runtime.InteropServices.DllImport(""test32.dll"")]
						public static extern HRESULT GetObj(object? p1, in System.Guid p2, [System.Runtime.InteropServices.MarshalAs(UnmanagedType.IUnknown, IidParameterIndex = 1)] out object? p3);
					}
				}
			";
		var compilation = GetCompilation(src);
		CreateGeneratorDriverAndRun(compilation, new IUnkMethodGenerator(), null, out var output, out var diag);
		WriteTrees(TestContext.Out, output.SyntaxTrees);
		Assert.That(diag.Where(d => d.Severity == DiagnosticSeverity.Error), Is.Empty);
		Assert.That(output.SyntaxTrees.Count(), Is.EqualTo(4));
	}

	private static void CreateGeneratorDriverAndRun(CSharpCompilation compilation, IIncrementalGenerator sourceGenerator, string? additionalFile, out Compilation output, out System.Collections.Immutable.ImmutableArray<Diagnostic> diag) =>
		CSharpGeneratorDriver.Create(
			generators: [sourceGenerator.AsSourceGenerator()],
			additionalTexts: additionalFile is not null ? [new InMemoryAdditionalText("handles.csv", File.ReadAllText(additionalFile))] : [],
			driverOptions: new GeneratorDriverOptions(default, trackIncrementalGeneratorSteps: true)).
		RunGeneratorsAndUpdateCompilation(compilation, out output, out diag);

	private static CSharpCompilation GetCompilation(string sourceCode) => CSharpCompilation.Create(nameof(CodeGenTests),
		[CSharpSyntaxTree.ParseText(sourceCode)],
		AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).Select(a => MetadataReference.CreateFromFile(a.Location)).Cast<MetadataReference>(),
		new(OutputKind.DynamicallyLinkedLibrary));

	private static void WriteTrees(TextWriter tw, IEnumerable<SyntaxTree> trees, bool skipFirst = true)
	{
		foreach (var tree in trees.Skip(skipFirst ? 1 : 0))
		{
			var fn = Path.GetFileName(tree.FilePath);
			tw.WriteLine($"== {fn} {new string('=', 78-fn.Length)}");
			tw.WriteLine(tree);
		}
	}
}

internal class InMemoryAdditionalText(string path, string content) : AdditionalText
{
	private readonly SourceText _content = SourceText.From(content, Encoding.UTF8);
	public override string Path { get; } = path;
	public override SourceText GetText(CancellationToken cancellationToken = default) => _content;
}