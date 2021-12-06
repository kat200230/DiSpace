using System;
using System.Collections.Generic;
using System.Data;

namespace DiSpaceCore
{
	public class DiSpaceOrderQuestion : DiSpaceQuestion
	{
        public DiSpaceOrderQuestion(DiSpaceClient client, IDataRecord record) : base(client, record)
        {
            IsShuffled = record.GetSqliteBoolean(8);
            ModalFeedback = record.GetSqliteTextOrNull(9);
            CorrectString = record.GetSqliteText(11);
        }

        public bool IsShuffled { get; }
        public string? ModalFeedback { get; }
		public string CorrectString { get; }

        private DiSpaceOrderOption[]? options;
        public IReadOnlyList<DiSpaceOrderOption> Options => options ??= DecodeOptions();
        private DiSpaceOrderOption[] DecodeOptions()
        {
            DiSpaceOrderOption[] orderOptions = Client.GetOptionsInternal(Id, static (c, r) => new DiSpaceOrderOption(c, r));
            string[] correctSplit = CorrectString.Split('|');
            int i = 0;
            return Array.ConvertAll(correctSplit, opt =>
            {
                DiSpaceOrderOption option = DiSpaceOption.FindOption(orderOptions, opt);
                option.CorrectIndex = i++;
                return option;
            });
        }

        protected override IReadOnlyList<DiSpaceOption> GetOptions() => Options;
    }
    public class DiSpaceOrderOption : DiSpaceOption
    {
		public DiSpaceOrderOption(DiSpaceClient client, IDataRecord record) : base(client, record) { }
		public int CorrectIndex { get; internal set; }
    }
    public class DiSpaceOrderAnswer : DiSpaceAnswer
    {
        public DiSpaceOrderAnswer(DiSpaceClient client, IDataRecord record) : base(client, record) { }

        private DiSpaceOrderQuestion? question;
        public override DiSpaceOrderQuestion Question => question ??= (DiSpaceOrderQuestion)Client.GetQuestion(QuestionId);

        private DiSpaceOrderOption[]? response;
        public IReadOnlyList<DiSpaceOrderOption> Response => response ??= DecodeResponse();
        private DiSpaceOrderOption[] DecodeResponse()
        {
            string[] split = ResponseString.Split('|');
            IReadOnlyList<DiSpaceOrderOption> options = Question.Options;
            return Array.ConvertAll(split, opt => DiSpaceOption.FindOption(options, opt));
        }

    }
}
