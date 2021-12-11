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
            Name = record.GetSqliteTextOrNull(1);
        }
        public int Id { get; }
		public string? Name { get; }

        private DiSpaceUnit[]? units;
        public IReadOnlyList<DiSpaceUnit> Units => units ??= Client.GetUnitsInternal(Id);

        private DiSpaceAttempt[]? attempts;
        public IReadOnlyList<DiSpaceAttempt> Attempts => attempts ??= Client.GetAttemptsByTestId(Id);
    }
}