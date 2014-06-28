using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using WpfApplication1.Models;

namespace WpfApplication1.ViewModels
{
    /// <summary>
    /// VM
    /// </summary>
    public class MainViewModel
    {
        /// <summary>
        /// CSVレコードのコレクション
        /// </summary>
        public IRecordCollection Records { private set; get; }

        /// <summary>
        /// CSVデータの読み取り
        /// </summary>
        /// <param name="path">ファイルパス</param>
        public void Read(string path)
        {
            using (var reader = new System.IO.StreamReader(path))
            {
                //カラムヘッダ行を読み取り
                var line = reader.ReadLine();
                var header = line.Split(',');

                //カラム定義部を読み取り
                line = reader.ReadLine();
                var defs = line.Split(',').Select(x => x.Split(':'))
                    .Select(x => new { name = x[0].Trim(), type = x[1].Trim() });
                
                //カラム定義から、レコード読み取り用オブジェクトを自動生成
                var fieldDefs = header.Zip(defs, (h, d) => new RecordGenerator.ColumnDefinition()
                {
                    ColumnName = h,
                    PropertyName = d.name,
                    TypeName = d.type
                });
                Records = CreateRecordCollection(fieldDefs);

                //レコード本体を読み取り
                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine();
                    var row = line.Split(',');

                    Records.AddRecord(header, row);
                }
            }
        }

        /// <summary>
        /// レコードデータ保持用オブジェクトを生成する
        /// </summary>
        /// <param name="columnDefs">カラム定義</param>
        /// <returns>レコードデータ保持オブジェクト</returns>
        public static IRecordCollection CreateRecordCollection(IEnumerable<RecordGenerator.ColumnDefinition> columnDefs)
        {
            //ランタイムテンプレートでソースコードを生成
            var generator = new RecordGenerator(columnDefs);
            var sourceCode = generator.TransformText();

            //ソースコードをParseしてSyntax Treeを生成
            var sourceTree = CSharpSyntaxTree.ParseText(sourceCode);

            //コードが参照するアセンブリの一覧
            var references = new[]{
                //microlib.dll
                new MetadataFileReference(typeof(object).Assembly.Location),
                //System.dll
                new MetadataFileReference(typeof(System.Collections.ObjectModel.ObservableCollection<>).Assembly.Location),
                //System.Core.dll
                new MetadataFileReference(typeof(System.Linq.Enumerable).Assembly.Location),
                //HandyUtil.Extensions.dll
                new MetadataFileReference(typeof(HandyUtil.Extensions.System.StringExt).Assembly.Location),
                //WpfApplication1.exe
                new MetadataFileReference(typeof(MainViewModel).Assembly.Location)
            };

            //コンパイルオブジェクト
            var compilation = CSharpCompilation.Create("GeneratedModels",
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
                        System.Diagnostics.Debug.WriteLine(mes);
                    }
                    return null;
                }
            }
            //アセンブリから目的のクラスを取得してインスタンスを生成
            var type = assembly.GetType("WpfApplication1.Models.GeneratedRecordCollection");
            return (IRecordCollection)Activator.CreateInstance(type);
        }


    }

}