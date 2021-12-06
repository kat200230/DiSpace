using System.Collections.Generic;
using System.Data;

namespace DiSpaceCore
{
    public class DiSpaceTest
    {
        private readonly DiSpaceClient Client;
        public DiSpaceTest(DiSpaceClient client, IDataRecord record)
        {
            Client = client;

            Id = record.GetSqliteInt32(0);
        }
        public int Id { get; }

        private DiSpaceUnit[]? units;
        public IReadOnlyList<DiSpaceUnit> Units => units ??= Client.GetUnitsInternal(Id);
    }
}