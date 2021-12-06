using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace DiSpaceCore
{
	public class DiSpaceAssociativeQuestion : DiSpaceQuestion
	{
        public DiSpaceAssociativeQuestion(DiSpaceClient client, IDataRecord record) : base(client, record)
        {
            IsShuffled = record.GetSqliteBoolean(8);
            ModalFeedback = record.GetSqliteTextOrNull(9);
            CorrectString = record.GetSqliteTextOrNull(11);
        }

		public bool IsShuffled { get; }
		public string? ModalFeedback { get; }
		public string? CorrectString { get; }

        private DiSpaceOption[]? options;
        private DiSpaceAssociativeRow[]? rows;
        public IReadOnlyList<DiSpaceAssociativeRow> Rows => rows ??= GetRows();
        private DiSpaceAssociativeColumn[]? columns;
        public IReadOnlyList<DiSpaceAssociativeColumn> Columns => columns ??= GetColumns();

        private DiSpaceAssociativeRow[] GetRows()
        {
            SetOptions();
            return rows;
        }
        private DiSpaceAssociativeColumn[] GetColumns()
        {
            SetOptions();
            return columns;
        }
        [MemberNotNull(nameof(options), nameof(rows), nameof(columns))]
        private void SetOptions()
        {
            options = Client.GetOptionsInternal(Id, static (c, r) =>
            {
                string? mappingString = r.GetSqliteTextOrNull(7);
                return mappingString is not null ? (DiSpaceOption)new DiSpaceAssociativeRow(c, r) : new DiSpaceAssociativeColumn(c, r);
            });
            rows = options.OfType<DiSpaceAssociativeRow>().ToArray();
            Array.ForEach(rows, r => r.question = this);
            columns = options.OfType<DiSpaceAssociativeColumn>().ToArray();
        }

        private DiSpaceAssociativeChoice[]? correct;
        public IReadOnlyList<DiSpaceAssociativeChoice> Correct => correct ??= DecodeCorrect();
        private DiSpaceAssociativeChoice[] DecodeCorrect()
        {
            if (CorrectString is not null)
            {
                string[] correctSplit = CorrectString.Split('|');
                if (options is null) SetOptions();
                return Array.ConvertAll(correctSplit, correctPair =>
                {
                    string[] pairSplit = correctPair.Split(';');
                    return new DiSpaceAssociativeChoice(DiSpaceOption.FindOption(rows!, pairSplit[0]),
                                                        DiSpaceOption.FindOption(columns!, pairSplit[1]));
                });
            }
            else
            {
                if (options is null) SetOptions();
                List<DiSpaceAssociativeChoice> choices = new List<DiSpaceAssociativeChoice>();
                foreach (DiSpaceAssociativeRow row in rows!)
                {
                    choices.Add(new DiSpaceAssociativeChoice(row, row.Mapping.First(static m => m.IsCorrect).Column));
                }
                return choices.ToArray();
            }
        }

        protected override IReadOnlyList<DiSpaceOption> GetOptions()
        {
            if (options is null) SetOptions();
            return options;
        }
    }
    public class DiSpaceAssociativeRow : DiSpaceOption
    {
        public DiSpaceAssociativeRow(DiSpaceClient client, IDataRecord record) : base(client, record)
        {
            MappingString = record.GetSqliteText(7);
        }

		public string MappingString { get; }
        internal DiSpaceAssociativeQuestion question;
        private DiSpaceAssociativeMapping[]? mapping;
        public IReadOnlyList<DiSpaceAssociativeMapping> Mapping => mapping ??= DecodeMapping();
        private DiSpaceAssociativeMapping[] DecodeMapping()
        {
            string[] mappingSplit = MappingString.Split('|');
            return Array.ConvertAll(mappingSplit, s =>
            {
                string[] split = s.Split(';');
                return new DiSpaceAssociativeMapping(DiSpaceOption.FindOption(question.Columns, split[0]),
                                                     float.Parse(split[1]),
                                                     int.Parse(split[2]) == 1);
            });
        }
    }
    public struct DiSpaceAssociativeMapping
    {
        public DiSpaceAssociativeMapping(DiSpaceAssociativeColumn column, float score, bool isCorrect)
        {
            Column = column;
            Score = score;
            IsCorrect = isCorrect;
        }
		public DiSpaceAssociativeColumn Column { get; }
        public float Score { get; }
		public bool IsCorrect { get; }
    }
    public class DiSpaceAssociativeColumn : DiSpaceOption
    {
		public DiSpaceAssociativeColumn(DiSpaceClient client, IDataRecord record) : base(client, record) { }
    }
    public class DiSpaceAssociativeAnswer : DiSpaceAnswer
    {
		public DiSpaceAssociativeAnswer(DiSpaceClient client, IDataRecord record) : base(client, record) { }

        private DiSpaceAssociativeQuestion? question;
        public override DiSpaceAssociativeQuestion Question => question ??= (DiSpaceAssociativeQuestion)Client.GetQuestion(QuestionId);

        private DiSpaceAssociativeChoice[]? response;
        public IReadOnlyList<DiSpaceAssociativeChoice> Response => response ??= DecodeResponse();
        private DiSpaceAssociativeChoice[] DecodeResponse()
        {
            string[] pairs = ResponseString.Split('|');
            IReadOnlyList<DiSpaceAssociativeRow> rows = Question.Rows;
            IReadOnlyList<DiSpaceAssociativeColumn> columns = Question.Columns;
            return Array.ConvertAll(pairs, pairString =>
            {
                string[] pairSplit = pairString.Split(';');
                return new DiSpaceAssociativeChoice(DiSpaceOption.FindOption(rows, pairSplit[0]),
                                                    DiSpaceOption.FindOption(columns, pairSplit[1]));
            });
        }

    }
    public readonly struct DiSpaceAssociativeChoice
    {
        public DiSpaceAssociativeChoice(DiSpaceAssociativeRow row, DiSpaceAssociativeColumn column)
        {
            Row = row;
            Column = column;
        }
		public DiSpaceAssociativeRow Row { get; }
		public DiSpaceAssociativeColumn Column { get; }
    }
}
