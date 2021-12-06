using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DiSpaceCore
{
    public abstract class DiSpaceQuestion
    {
        protected readonly DiSpaceClient Client;
        protected DiSpaceQuestion(DiSpaceClient client, IDataRecord record)
        {
            Client = client;

            Id = record.GetSqliteInt32(0);
            ThemeId = record.GetSqliteInt32(1);
            Title = record.GetSqliteText(2);
            Prompt = record.GetSqliteText(3);
            MaxScore = record.GetSqliteFloatOrNull(4);
            ShowSolution = record.GetSqliteBoolean(5);
            TypeOriginal = (DiSpaceQuestionTypeOriginal)record.GetSqliteInt32(6);
            Type = (DiSpaceQuestionType)record.GetSqliteInt32(7);
        }

        public int Id { get; }
        public int ThemeId { get; }
        public string Title { get; }
        public string Prompt { get; }
        public float? MaxScore { get; }
        public bool ShowSolution { get; }
        public DiSpaceQuestionTypeOriginal TypeOriginal { get; }
        public DiSpaceQuestionType Type { get; }

        public static DiSpaceQuestion Resolve(DiSpaceClient client, IDataRecord record)
            => (DiSpaceQuestionType)record.GetSqliteInt32(7) switch
            {
                DiSpaceQuestionType.SimpleChoice => new DiSpaceSimpleQuestion(client, record),
				DiSpaceQuestionType.PairChoice => new DiSpacePairQuestion(client, record),
				DiSpaceQuestionType.AssociativeChoice => new DiSpaceAssociativeQuestion(client, record),
				DiSpaceQuestionType.OrderChoice => new DiSpaceOrderQuestion(client, record),
				DiSpaceQuestionType.CustomInput => new DiSpaceCustomInputQuestion(client, record),
                DiSpaceQuestionType.OpenQuestion => new DiSpaceOpenQuestion(client, record),
                _ => throw new InvalidOperationException("Unknown question type."),
            };

        protected abstract IReadOnlyList<DiSpaceOption>? GetOptions();
        internal IReadOnlyList<DiSpaceOption>? GetOptionsInternal() => GetOptions();
    }
    public abstract class DiSpaceOption
    {
        protected readonly DiSpaceClient Client;
        protected DiSpaceOption(DiSpaceClient client, IDataRecord record)
        {
            Client = client;

            QuestionId = record.GetSqliteInt32(0);
            Hash = record.GetSqliteText(1);
            Id = record.GetSqliteInt32OrNull(2);
            Text = record.GetSqliteText(3);
        }

        public int QuestionId { get; }
        public string Hash { get; }
        public int? Id { get; }
        public string Text { get; }

        public static TOption FindOption<TOption>(TOption[] options, string hashOrId) where TOption : DiSpaceOption
        {
            if (hashOrId.Length > 0 && hashOrId[0] is >= '0' and <= '9' && int.TryParse(hashOrId, out int id))
            {
                return Array.Find(options, o => o.Id == id)
                    ?? throw new ArgumentException("Option with the specified id was not found.");
            }
            else
            {
                if (hashOrId.StartsWith("Choice_")) hashOrId = hashOrId["Choice_".Length..];
                return Array.Find(options, o => o.Hash == hashOrId)
                    ?? throw new ArgumentException("Option with the specified hashOrId was not found.");
            }
        }
        public static TOption FindOption<TOption>(IEnumerable<TOption> options, string hashOrId) where TOption : DiSpaceOption
        {
            if (hashOrId.Length > 0 && hashOrId[0] is >= '0' and <= '9' && int.TryParse(hashOrId, out int id))
            {
                return options.FirstOrDefault(o => o.Id == id)
                    ?? throw new ArgumentException("Option with the specified id was not found.");
            }
            else
            {
                if (hashOrId.StartsWith("Choice_")) hashOrId = hashOrId["Choice_".Length..];
                return options.FirstOrDefault(o => o.Hash == hashOrId)
                    ?? throw new ArgumentException("Option with the specified hashOrId was not found.");
            }
        }

    }
    public abstract class DiSpaceAnswer
    {
        protected readonly DiSpaceClient Client;
        protected DiSpaceAnswer(DiSpaceClient client, IDataRecord record)
        {
            Client = client;

            AttemptId = record.GetSqliteInt32(0);
            QuestionId = record.GetSqliteInt32(1);
            Score = record.GetSqliteFloat(2);
            Type = (DiSpaceQuestionType)record.GetSqliteInt32(3);
            ResponseString = record.GetSqliteText(4);
        }

        public int AttemptId { get; }
        public int QuestionId { get; }
        public float Score { get; }
        public DiSpaceQuestionType Type { get; }
        public string ResponseString { get; }

        public abstract DiSpaceQuestion Question { get; }

        public static DiSpaceAnswer Resolve(DiSpaceClient client, IDataRecord record)
            => (DiSpaceQuestionType)record.GetSqliteInt32(3) switch
            {
                DiSpaceQuestionType.SimpleChoice => new DiSpaceSimpleAnswer(client, record),
				DiSpaceQuestionType.PairChoice => new DiSpacePairAnswer(client, record),
				DiSpaceQuestionType.AssociativeChoice => new DiSpaceAssociativeAnswer(client, record),
				DiSpaceQuestionType.OrderChoice => new DiSpaceOrderAnswer(client, record),
				DiSpaceQuestionType.CustomInput => new DiSpaceCustomInputAnswer(client, record),
				DiSpaceQuestionType.OpenQuestion => new DiSpaceOpenQuestionAnswer(client, record),
				_ => throw new InvalidOperationException("Unknown question type."),
            };
    }
}