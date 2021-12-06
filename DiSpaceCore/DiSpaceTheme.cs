using System.Collections.Generic;
using System.Data;

namespace DiSpaceCore
{
    public class DiSpaceTheme
    {
        private readonly DiSpaceClient Client;
        public DiSpaceTheme(DiSpaceClient client, IDataRecord record)
        {
            Client = client;

            Id = record.GetSqliteInt32(0);
            UnitId = record.GetSqliteInt32(1);
            Hash = record.GetSqliteText(2);
            Name = record.GetSqliteTextOrNull(3);
            Description = record.GetSqliteTextOrNull(4);
            Selection = record.GetSqliteInt32(5);
            IsVisible = record.GetSqliteBoolean(6);
            IsShuffled = record.GetSqliteBoolean(7);
        }

        public int Id { get; }
        public int UnitId { get; }
        public string Hash { get; }
        public string? Name { get; }
        public string? Description { get; }
        public int Selection { get; }
        public bool IsVisible { get; }
        public bool IsShuffled { get; }

        private DiSpaceQuestion[]? questions;
        public IReadOnlyList<DiSpaceQuestion> Questions => questions ??= Client.GetQuestionsInternal(Id);

        private DiSpaceUnit? unit;
        public DiSpaceUnit Unit => unit ??= Client.GetUnit(UnitId);

    }
}