using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.SQLite;

namespace DiSpaceCore
{
    public class DiSpaceUnit
    {
        private readonly DiSpaceClient Client;
        public DiSpaceUnit(DiSpaceClient client, IDataRecord record)
        {
            Client = client;
            Id = record.GetSqliteInt32(0);
            TestId = record.GetSqliteInt32(1);
            Hash = record.GetSqliteText(2);
            Name = record.GetSqliteTextOrNull(3);
            Description = record.GetSqliteTextOrNull(4);
            Selection = record.GetSqliteInt32(5);
            IsVisible = record.GetSqliteBoolean(6);
            IsShuffled = record.GetSqliteBoolean(7);
        }

        public int Id { get; }
        public int TestId { get; }
        public string Hash { get; }
        public string? Name { get; }
        public string? Description { get; }
        public int Selection { get; }
        public bool IsVisible { get; }
        public bool IsShuffled { get; }

        private DiSpaceTheme[]? themes;
        public IReadOnlyList<DiSpaceTheme> Themes => themes ??= Client.GetThemesInternal(Id);
    }
}