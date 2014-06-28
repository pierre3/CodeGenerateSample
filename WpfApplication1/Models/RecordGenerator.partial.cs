using System.Collections.Generic;

namespace WpfApplication1.Models
{
    public partial class RecordGenerator
    {
        public IList<ColumnDefinition> ColumnDefinitions { get; set; }
        
        public RecordGenerator(IEnumerable<ColumnDefinition> defs)
        {
            ColumnDefinitions = new List<ColumnDefinition>(defs);
        }

        public class ColumnDefinition
        {
            public string ColumnName { get; set; }
            public string PropertyName { get; set; }
            public string TypeName { get; set; }
        }
    }
}
