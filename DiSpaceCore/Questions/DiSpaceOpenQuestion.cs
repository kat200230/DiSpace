using System.Collections.Generic;
using System.Data;

namespace DiSpaceCore
{
	public class DiSpaceOpenQuestion : DiSpaceQuestion
	{
        public DiSpaceOpenQuestion(DiSpaceClient client, IDataRecord record) : base(client, record) { }

        protected override IReadOnlyList<DiSpaceOption>? GetOptions() => null;

    }
    public class DiSpaceOpenQuestionAnswer : DiSpaceAnswer
    {
		public DiSpaceOpenQuestionAnswer(DiSpaceClient client, IDataRecord record) : base(client, record) { }

        private DiSpaceOpenQuestion? question;
        public override DiSpaceOpenQuestion Question => question ??= (DiSpaceOpenQuestion)Client.GetQuestion(QuestionId);

        public string Response => ResponseString;
    }
}
