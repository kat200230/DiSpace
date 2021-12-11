using System;
using System.Collections.Generic;
using System.Data;

namespace DiSpaceCore
{
    public class DiSpaceAttempt
    {
        private readonly DiSpaceClient Client;
        public DiSpaceAttempt(DiSpaceClient client, IDataRecord record)
        {
            Client = client;

            Id = record.GetSqliteInt32(0);
            TestId = record.GetSqliteInt32(1);
            UserId = record.GetSqliteInt32(2);
            StartedAt = record.GetSqliteDateTime(3);
            FinishedAt = record.GetSqliteDateTimeOrNull(4);
            Score = record.GetSqliteFloat(5);
            MaxScore = record.GetSqliteFloat(6);
            ShowResultsMode = record.GetSqliteText(7);
            OpenQuestionCount = record.GetSqliteInt32(8);
            IsTrial = record.GetSqliteBoolean(9);
            SetAsRead = record.GetSqliteBoolean(10);
            RevisionNumber = record.GetSqliteInt32(11);
        }

        public int Id { get; }
        public int TestId { get; }
        public int UserId { get; }
        public DateTimeOffset StartedAt { get; }
        public DateTimeOffset? FinishedAt { get; }
        public float Score { get; }
        public float MaxScore { get; }
        public string ShowResultsMode { get; }
        public int OpenQuestionCount { get; }
        public bool IsTrial { get; }
        public bool SetAsRead { get; }
        public int RevisionNumber { get; }

        private DiSpaceUnitResult[]? unitResults;
        public IReadOnlyList<DiSpaceUnitResult> UnitResults => unitResults ??= Client.GetUnitResultsInternal(Id);
    }
    public class DiSpaceUnitResult
    {
        private readonly DiSpaceClient Client;
        public DiSpaceUnitResult(DiSpaceClient client, IDataRecord record)
        {
            Client = client;

            AttemptId = record.GetSqliteInt32(0);
            UnitId = record.GetSqliteInt32(1);
            Score = record.GetSqliteFloat(2);
            MaxScore = record.GetSqliteFloat(3);
        }

		public int AttemptId { get; }
		public int UnitId { get; }
		public float Score { get; }
		public float MaxScore { get; }

        private DiSpaceThemeResult[]? themeResults;
        public IReadOnlyList<DiSpaceThemeResult> ThemeResults => themeResults ??= Client.GetThemeResultsInternal(AttemptId, UnitId);
    }
    public class DiSpaceThemeResult
    {
        private readonly DiSpaceClient Client;
        public DiSpaceThemeResult(DiSpaceClient client, IDataRecord record)
        {
            Client = client;

            AttemptId = record.GetSqliteInt32(0);
            ThemeId = record.GetSqliteInt32(1);
            Score = record.GetSqliteFloat(2);
            MaxScore = record.GetSqliteFloat(3);
        }

        public int AttemptId { get; }
        public int ThemeId { get; }
        public float Score { get; }
        public float MaxScore { get; }

        private DiSpaceAnswer[]? answers;
        public IReadOnlyList<DiSpaceAnswer> Answers => answers ??= Client.GetAnswersInternal(AttemptId, ThemeId);
    }
}