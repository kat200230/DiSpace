using System;
using System.Collections.Generic;
using System.Data;

namespace DiSpaceCore
{
    public class DiSpaceSimpleQuestion : DiSpaceQuestion
    {
        public DiSpaceSimpleQuestion(DiSpaceClient client, IDataRecord record) : base(client, record)
        {
            IsShuffled = record.GetSqliteBoolean(8);
            ModalFeedback = record.GetSqliteTextOrNull(9);
            MaxChoices = record.GetSqliteInt32(10);
        }

        public bool IsShuffled { get; }
        public string? ModalFeedback { get; }
        public int MaxChoices { get; }

        private DiSpaceSimpleOption[]? options;
        public IReadOnlyList<DiSpaceSimpleOption> Options
            => options ??= Client.GetOptionsInternal(Id, static (c, r) => new DiSpaceSimpleOption(c, r));
        private DiSpaceSimpleOption[]? correct;
        public IReadOnlyList<DiSpaceSimpleOption> Correct
            => correct ??= Array.FindAll((DiSpaceSimpleOption[])Options, static o => o.IsCorrect);

        protected override IReadOnlyList<DiSpaceSimpleOption> GetOptions() => Options;
    }
    public class DiSpaceSimpleOption : DiSpaceOption
    {
        public DiSpaceSimpleOption(DiSpaceClient client, IDataRecord record) : base(client, record)
        {
            Score = record.GetSqliteFloat(4);
            IsCorrect = record.GetSqliteBoolean(5);
        }

        public float Score { get; }
        public bool IsCorrect { get; }
    }
    public class DiSpaceSimpleAnswer : DiSpaceAnswer
    {
        public DiSpaceSimpleAnswer(DiSpaceClient client, IDataRecord record) : base(client, record) { }

        private DiSpaceSimpleQuestion? question;
        public override DiSpaceSimpleQuestion Question => question ??= (DiSpaceSimpleQuestion)Client.GetQuestion(QuestionId);

        private DiSpaceSimpleOption[]? response;
        public IReadOnlyList<DiSpaceSimpleOption> Response => response ??= DecodeResponse();
        private DiSpaceSimpleOption[] DecodeResponse()
        {
            string[] responseOptions = ResponseString.Split('|');
            IReadOnlyList<DiSpaceSimpleOption> options = Question.Options;
            return Array.ConvertAll(responseOptions, opt => DiSpaceOption.FindOption(options, opt));
        }
    }
}