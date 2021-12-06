using System;
using System.Collections.Generic;
using System.Data;

namespace DiSpaceCore
{
	public class DiSpacePairQuestion : DiSpaceQuestion
	{
        public DiSpacePairQuestion(DiSpaceClient client, IDataRecord record) : base(client, record)
        {
            IsShuffled = record.GetSqliteBoolean(8);
            CorrectString = record.GetSqliteText(11);
        }

		public bool IsShuffled { get; }
		public string CorrectString { get; }
        private Pair<DiSpacePairOption>[]? correct;
        public IReadOnlyList<Pair<DiSpacePairOption>> Correct => correct ??= DecodeCorrect();
        private Pair<DiSpacePairOption>[] DecodeCorrect()
        {
            string[] correctPairs = CorrectString.Split('|');
            IReadOnlyList<DiSpacePairOption> optionsList = Options;
            return Array.ConvertAll(correctPairs, pairString =>
            {
                string[] pairValues = pairString.Split(';');
                return new Pair<DiSpacePairOption>(DiSpaceOption.FindOption(optionsList, pairValues[0]),
                                                   DiSpaceOption.FindOption(optionsList, pairValues[1]));
            });
        }

        private DiSpacePairOption[]? options;
        public IReadOnlyList<DiSpacePairOption> Options
            => options ??= Client.GetOptionsInternal(Id, static (c, r) => new DiSpacePairOption(c, r));

        protected override IReadOnlyList<DiSpaceOption> GetOptions() => Options;
    }
    public class DiSpacePairOption : DiSpaceOption
    {
        public DiSpacePairOption(DiSpaceClient client, IDataRecord record) : base(client, record)
        {
            MaxMatches = record.GetSqliteInt32(6);
        }

		public int MaxMatches { get; }
    }
    public class DiSpacePairAnswer : DiSpaceAnswer
    {
        public DiSpacePairAnswer(DiSpaceClient client, IDataRecord record) : base(client, record) { }

        private DiSpacePairQuestion? question;
        public override DiSpacePairQuestion Question => question ??= (DiSpacePairQuestion)Client.GetQuestion(QuestionId);

        private Pair<DiSpacePairOption>[]? response;
        public IReadOnlyList<Pair<DiSpacePairOption>> Response => response ??= DecodeResponse();
        private Pair<DiSpacePairOption>[] DecodeResponse()
        {
            string[] correctPairs = ResponseString.Split('|');
            IReadOnlyList<DiSpacePairOption> optionsList = Question.Options;
            return Array.ConvertAll(correctPairs, pairString =>
            {
                string[] pairValues = pairString.Split(';');
                return new Pair<DiSpacePairOption>(DiSpaceOption.FindOption(optionsList, pairValues[0]),
                                                   DiSpaceOption.FindOption(optionsList, pairValues[1]));
            });
        }
    }
    public readonly struct Pair<T> : IEquatable<Pair<T>>
    {
        public Pair(T a, T b)
        {
            A = a;
            B = b;
        }
		public T A { get; }
		public T B { get; }

        public bool Equals(Pair<T> other)
            => Equals(A, other.A) ? Equals(B, other.B)
            : Equals(A, other.B) && Equals(B, other.A);
        public override bool Equals(object? obj) => obj is Pair<T> pair && Equals(pair);
        public override int GetHashCode() => (A?.GetHashCode() ?? 0) ^ (B?.GetHashCode() ?? 0);

        public static bool operator ==(Pair<T> left, Pair<T> right) => left.Equals(right);
        public static bool operator !=(Pair<T> left, Pair<T> right) => !left.Equals(right);

    }
}
