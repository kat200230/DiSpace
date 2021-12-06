using System;
using System.Collections.Generic;
using System.Data;

namespace DiSpaceCore
{
	public class DiSpaceCustomInputQuestion : DiSpaceQuestion
	{
        public DiSpaceCustomInputQuestion(DiSpaceClient client, IDataRecord record) : base(client, record)
        {
            CorrectString = record.GetSqliteText(11);
        }

		public string CorrectString { get; }

        private DiSpaceCustomInputPattern[]? correct;
        public IReadOnlyList<DiSpaceCustomInputPattern> Correct => correct ??= DecodeCorrect();
        private DiSpaceCustomInputPattern[] DecodeCorrect()
        {
            string[] correctSplit = CorrectString.Split('|');
            return Array.ConvertAll(correctSplit, static str =>
            {
                string[] strSplit = str.Split('/');
                return new DiSpaceCustomInputPattern(strSplit[0].Replace("&s;", "/").Replace("&p;", "|"),
                                                     float.Parse(strSplit[1]));
            });
        }

        protected override IReadOnlyList<DiSpaceOption>? GetOptions() => null;

    }
    public struct DiSpaceCustomInputPattern
    {
        public DiSpaceCustomInputPattern(string pattern, float score)
        {
            Pattern = pattern;
            Score = score;
        }
		public string Pattern { get; }
		public float Score { get; }
    }
    public class DiSpaceCustomInputAnswer : DiSpaceAnswer
    {
		public DiSpaceCustomInputAnswer(DiSpaceClient client, IDataRecord record) : base(client, record) { }

        public string Response => ResponseString;

        private DiSpaceCustomInputQuestion? question;
        public override DiSpaceCustomInputQuestion Question => question ??= (DiSpaceCustomInputQuestion)Client.GetQuestion(QuestionId);

    }
}
