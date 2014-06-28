using System.Collections;
using System.Collections.Generic;

namespace WpfApplication1.Models
{
    /// <summary>
    /// CSVレコードのコレクションを表すインターフェース
    /// </summary>
    public interface IRecordCollection
    {
        /// <summary>レコードコレクション(バインド用)</summary>
        IEnumerable Items { get; }
        
        /// <summary>
        /// レコードの追加
        /// </summary>
        /// <param name="header">カラムヘッダ</param>
        /// <param name="row">レコードデータ</param>
        void AddRecord(IEnumerable<string> header, IEnumerable<string> row);
    }
}
