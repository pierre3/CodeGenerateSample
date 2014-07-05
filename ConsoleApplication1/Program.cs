using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Collections.Generic;

namespace ConsoleApplication1
{
    public interface ICodeExecutor
    {
        void Execute();
    }

    class Program
    {
        static readonly string indentUsing = "using ";
        static readonly string indent4 = "    ";
        static readonly string indent12 = "            ";
        static readonly IEnumerable<string> foot = new[] { "        }", "    }", "}" };

        static List<string> usingLines;
        static List<string> namespaceLines;
        static List<string> executorLines;

        static void Main(string[] args)
        {
            InitLines();
            List<string> input = executorLines;
            string indent = indent12;

            while (true)
            {
                var line = Console.ReadLine();

                bool replaces = line.StartsWith("@replace");
                bool deletes = line.StartsWith("@delete");
                bool inserts = line.StartsWith("@insert");
                if (replaces || deletes || inserts)
                {
                    var tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length >= 2)
                    {
                        int n;
                        if (int.TryParse(tokens[1], out n))
                        {
                            EditLine(n, deletes || replaces, inserts || replaces);
                        }
                    }
                    continue;
                }

                if (line.StartsWith("@using"))
                {
                    var tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length >= 2) 
                    {
                        usingLines.Add(indentUsing + tokens[1]);
                    }
                    continue;
                }

                switch (line)
                {
                    case "@run":
                        ICodeExecutor exec = CreateExecutor(string.Join(Environment.NewLine, BuildCode()));
                        if (exec != null)
                        {
                            try
                            {
                                exec.Execute();
                            }
                            catch (Exception e)
                            {
                                Console.Error.WriteLine(e);
                            }
                        }
                        break;

                    case "@type":
                        input = namespaceLines;
                        indent = indent4;
                        break;

                    case "@end":
                        input = executorLines;
                        indent = indent12;
                        break;

                    case "@list":
                        Console.WriteLine(string.Join(Environment.NewLine,
                            BuildCode().Select((s, n) => n.ToString("D3") + ": " + s)));
                        break;

                    case "@clearAll":
                        InitLines();
                        input = executorLines;
                        indent = indent12;
                        break;

                    case "@clear":
                        InitExecutor();
                        input = executorLines;
                        indent = indent12;
                        break;

                    case "@exit":
                        return;

                    default:
                        input.Add(indent + line);
                        break;
                }
            }
        }

        static void EditLine(int index, bool deletes, bool inserts)
        {
            if (index >= 0 && index < usingLines.Count)
            {
                if (deletes) { usingLines.RemoveAt(index); }
                if (inserts) { usingLines.Insert(index, Console.ReadLine()); }
            }
            else if (index >= usingLines.Count
               && index < usingLines.Count + namespaceLines.Count)
            {
                if (deletes) { namespaceLines.RemoveAt(index - usingLines.Count); }
                if (inserts)
                {
                    namespaceLines.Insert(index - usingLines.Count, Console.ReadLine());
                }
            }
            else if (index >= usingLines.Count + namespaceLines.Count
               && index < usingLines.Count + namespaceLines.Count + executorLines.Count)
            {
                if (deletes) { executorLines.RemoveAt(index - usingLines.Count - namespaceLines.Count); }
                if (inserts)
                {
                    executorLines.Insert(index - usingLines.Count - namespaceLines.Count, Console.ReadLine());
                }
            }
            else if (index <= usingLines.Count + namespaceLines.Count + executorLines.Count && inserts)
            {
                executorLines.Add(Console.ReadLine());
            }
        }

        static void InitLines()
        {
            InitUsing();
            InitNamespace();
            InitExecutor();
        }

        static void InitUsing()
        {
            usingLines = new List<string>()
            {
                "using System;",
                "using System.Collections;",
                "using System.Collections.Generic;",
                "using System.Linq;"
            };
        }
        static void InitNamespace()
        {
            namespaceLines = new List<string>()
            {
                "namespace ConsoleApplication1",
                "{"
            };
        }
        static void InitExecutor()
        {
            executorLines = new List<string>()
            {
                "    public class GeneratedExecutor: ICodeExecutor",
                "    {",
                "        public void Execute()",
                "        {"
            };
        }

        static IEnumerable<string> BuildCode()
        {
            return usingLines.Concat(namespaceLines).Concat(executorLines).Concat(foot);
        }

        static ICodeExecutor CreateExecutor(string code)
        {
            //ソースコードをParseしてSyntax Treeを生成
            var sourceTree = CSharpSyntaxTree.ParseText(code);

            //コードが参照するアセンブリの一覧
            var references = new[]{
                //microlib.dll
                new MetadataFileReference(typeof(object).Assembly.Location),
                //System.dll
                new MetadataFileReference(typeof(System.Collections.ObjectModel.ObservableCollection<>).Assembly.Location),
                //System.Core.dll
                new MetadataFileReference(typeof(System.Linq.Enumerable).Assembly.Location),
                //ConsoleApplication1.exe
                new MetadataFileReference(typeof(Program).Assembly.Location),
            };

            //コンパイルオブジェクト
            var compilation = CSharpCompilation.Create("GeneratedAssembly",
                new[] { sourceTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            Assembly assembly = null;
            using (var stream = new System.IO.MemoryStream())
            {
                //コンパイル実行。結果をストリームに書き込む
                var result = compilation.Emit(stream);
                if (result.Success)
                {
                    //成功時 アセンブリとしてロードする。
                    assembly = System.Reflection.Assembly.Load(stream.GetBuffer());
                }
                else
                {
                    //失敗時 コンパイルエラーを出力
                    foreach (var mes in result.Diagnostics.Select(d =>
                        string.Format("[{0}]:{1}({2})", d.Id, d.GetMessage(), d.Location.GetLineSpan().StartLinePosition)))
                    {
                        Console.Error.WriteLine(mes);
                    }
                    return null;
                }
            }

            //アセンブリから目的のクラスを取得してインスタンスを生成
            var type = assembly.GetType("ConsoleApplication1.GeneratedExecutor");
            return (ICodeExecutor)Activator.CreateInstance(type);

        }
    }
}
