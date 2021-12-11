using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace DiSpaceCore
{
    public class DiSpaceClient
    {
        public DiSpaceClient(SQLiteConnection connection) => Database = connection;
        public SQLiteConnection Database { get; }

        private Dictionary<int, DiSpaceTest> tests = new Dictionary<int, DiSpaceTest>();
        private Dictionary<int, DiSpaceUnit> units = new Dictionary<int, DiSpaceUnit>();
        private Dictionary<int, DiSpaceTheme> themes = new Dictionary<int, DiSpaceTheme>();
        private Dictionary<int, DiSpaceQuestion> questions = new Dictionary<int, DiSpaceQuestion>();
        private Dictionary<int, DiSpaceAttempt> attempts = new Dictionary<int, DiSpaceAttempt>();

        public async Task ConnectAsync()
        {
            await Database.OpenAsync();
            Database.BindFunction(new SQLiteFunctionAttribute("CIC", 2, FunctionType.Scalar), new CaseInsensitiveSearch());
        }

        public void ClearCache()
        {
            tests = new Dictionary<int, DiSpaceTest>();
            units = new Dictionary<int, DiSpaceUnit>();
            themes = new Dictionary<int, DiSpaceTheme>();
            questions = new Dictionary<int, DiSpaceQuestion>();
            attempts = new Dictionary<int, DiSpaceAttempt>();
        }

        public DiSpaceTest GetTest(int id) => TryGetTest(id, out DiSpaceTest? test) ? test
            : throw new ArgumentException("Test with the specified id was not found.");
        public bool TryGetTest(int id, [NotNullWhen(true)] out DiSpaceTest? test)
        {
            if (tests.TryGetValue(id, out test)) return true;
            SQLiteCommand getTest = new SQLiteCommand("SELECT * FROM tests WHERE id = @id LIMIT 1;", Database);
            getTest.Parameters.Add("@id", DbType.Int32).Value = id;
            SQLiteDataReader reader = getTest.ExecuteReader(CommandBehavior.SingleRow);
            if (!reader.Read()) return false;
            tests.Add(id, test = new DiSpaceTest(this, reader));
            return true;
        }
        public DiSpaceAttempt GetAttempt(int id) => TryGetAttempt(id, out DiSpaceAttempt? attempt) ? attempt
            : throw new ArgumentException("Attempt with the specified id was not found.");
        public bool TryGetAttempt(int id, [NotNullWhen(true)] out DiSpaceAttempt? attempt)
        {
            if (attempts.TryGetValue(id, out attempt)) return true;
            SQLiteCommand getAttempt = new SQLiteCommand("SELECT * FROM attempts WHERE id = @id LIMIT 1;", Database);
            getAttempt.Parameters.Add("@id", DbType.Int32).Value = id;
            SQLiteDataReader reader = getAttempt.ExecuteReader(CommandBehavior.SingleRow);
            if (!reader.Read()) return false;
            attempts.Add(id, attempt = new DiSpaceAttempt(this, reader));
            return true;
        }
        public DiSpaceUnit GetUnit(int id) => TryGetUnit(id, out DiSpaceUnit? unit) ? unit
		    : throw new ArgumentException("Unit with the specified id was not found.");
        public bool TryGetUnit(int id, [NotNullWhen(true)] out DiSpaceUnit? unit)
        {
            if (units.TryGetValue(id, out unit)) return true;
            SQLiteCommand getUnit = new SQLiteCommand("SELECT * FROM units WHERE id = @id LIMIT 1;", Database);
            getUnit.Parameters.Add("@id", DbType.Int32).Value = id;
            SQLiteDataReader reader = getUnit.ExecuteReader(CommandBehavior.SingleRow);
            if (!reader.Read()) return false;
            units.Add(id, unit = new DiSpaceUnit(this, reader));
            return true;
        }
        public DiSpaceTheme GetTheme(int id) => TryGetTheme(id, out DiSpaceTheme? theme) ? theme
            : throw new ArgumentException("Theme with the specified id was not found.");
        public bool TryGetTheme(int id, [NotNullWhen(true)] out DiSpaceTheme? theme)
        {
            if (themes.TryGetValue(id, out theme)) return true;
            SQLiteCommand getTheme = new SQLiteCommand("SELECT * FROM themes WHERE id = @id LIMIT 1;", Database);
            getTheme.Parameters.Add("@id", DbType.Int32).Value = id;
            SQLiteDataReader reader = getTheme.ExecuteReader(CommandBehavior.SingleRow);
            if (!reader.Read()) return false;
            themes.Add(id, theme = new DiSpaceTheme(this, reader));
            return true;
        }
        public DiSpaceQuestion GetQuestion(int id) => TryGetQuestion(id, out DiSpaceQuestion? question) ? question
            : throw new ArgumentException("Question with the specified id was not found.");
        public bool TryGetQuestion(int id, [NotNullWhen(true)] out DiSpaceQuestion? question)
        {
            if (questions.TryGetValue(id, out question)) return true;
            SQLiteCommand getQuestion = new SQLiteCommand("SELECT * FROM questions WHERE id = @id LIMIT 1;", Database);
            getQuestion.Parameters.Add("@id", DbType.Int32).Value = id;
            SQLiteDataReader reader = getQuestion.ExecuteReader(CommandBehavior.SingleRow);
            if (!reader.Read()) return false;
            questions.Add(id, question = DiSpaceQuestion.Resolve(this, reader));
            return true;
        }

        private DiSpaceAttempt? lastAttempt;
        public DiSpaceAttempt GetLastAttempt()
        {
            if (lastAttempt is not null) return lastAttempt;
            SQLiteCommand getLastAttempt = new SQLiteCommand("SELECT * FROM attempts ORDER BY started_at DESC LIMIT 1;", Database);
            SQLiteDataReader reader = getLastAttempt.ExecuteReader(CommandBehavior.SingleRow);
            if (!reader.Read()) throw new InvalidOperationException("The attempts table is empty.");
            return lastAttempt = new DiSpaceAttempt(this, reader);
			// TODO: prevent cache duplicity
        }

        private DiSpaceAttempt? firstAttempt;
        public DiSpaceAttempt GetFirstAttempt()
        {
            if (firstAttempt is not null) return firstAttempt;
            SQLiteCommand getFirstAttempt = new SQLiteCommand("SELECT * FROM attempts ORDER BY started_at ASC LIMIT 1;", Database);
            SQLiteDataReader reader = getFirstAttempt.ExecuteReader(CommandBehavior.SingleRow);
            if (!reader.Read()) throw new InvalidOperationException("The attempts table is empty.");
            return firstAttempt = new DiSpaceAttempt(this, reader);
            // TODO: prevent cache duplicity
        }

        internal DiSpaceUnit[] GetUnitsInternal(int testId)
        {
            SQLiteCommand getUnits = new SQLiteCommand("SELECT * FROM units WHERE test_id = @test_id;", Database);
            getUnits.Parameters.Add("@test_id", DbType.Int32).Value = testId;
            SQLiteDataReader reader = getUnits.ExecuteReader();
            List<DiSpaceUnit> list = new List<DiSpaceUnit>();
            while (reader.Read()) list.Add(new DiSpaceUnit(this, reader));
            return list.ToArray();
        }
        internal DiSpaceTheme[] GetThemesInternal(int unitId)
        {
            SQLiteCommand getThemes = new SQLiteCommand("SELECT * FROM themes WHERE unit_id = @unit_id;", Database);
            getThemes.Parameters.Add("@unit_id", DbType.Int32).Value = unitId;
            SQLiteDataReader reader = getThemes.ExecuteReader();
            List<DiSpaceTheme> list = new List<DiSpaceTheme>();
            while (reader.Read()) list.Add(new DiSpaceTheme(this, reader));
            return list.ToArray();
        }
        internal DiSpaceQuestion[] GetQuestionsInternal(int themeId)
        {
            SQLiteCommand getQuestions = new SQLiteCommand("SELECT * FROM questions WHERE theme_id = @theme_id;", Database);
            getQuestions.Parameters.Add("@theme_id", DbType.Int32).Value = themeId;
            SQLiteDataReader reader = getQuestions.ExecuteReader();
            List<DiSpaceQuestion> list = new List<DiSpaceQuestion>();
			while (reader.Read()) list.Add(DiSpaceQuestion.Resolve(this, reader));
            return list.ToArray();
        }

        public IReadOnlyList<DiSpaceOption> GetOptions(int questionId)
            => GetQuestion(questionId).GetOptionsInternal() ?? throw new ArgumentException("Question doesn't have options.");
        internal TOption[] GetOptionsInternal<TOption>(int questionId, Func<DiSpaceClient, IDataRecord, TOption> resolver) where TOption : DiSpaceOption
        {
            SQLiteCommand getOptions = new SQLiteCommand("SELECT * FROM options WHERE question_id = @question_id;", Database);
            getOptions.Parameters.Add("@question_id", DbType.Int32).Value = questionId;
            SQLiteDataReader reader = getOptions.ExecuteReader();
            List<TOption> list = new List<TOption>();
            while (reader.Read()) list.Add(resolver(this, reader));
            return list.ToArray();
        }

        internal DiSpaceUnitResult[] GetUnitResultsInternal(int attemptId)
        {
            SQLiteCommand getUnitResults = new SQLiteCommand("SELECT * FROM unit_results WHERE attempt_id = @attempt_id;", Database);
            getUnitResults.Parameters.Add("@attempt_id", DbType.Int32).Value = attemptId;
            SQLiteDataReader reader = getUnitResults.ExecuteReader();
            List<DiSpaceUnitResult> list = new List<DiSpaceUnitResult>();
            while (reader.Read()) list.Add(new DiSpaceUnitResult(this, reader));
            return list.ToArray();
        }
        internal DiSpaceThemeResult[] GetThemeResultsInternal(int attemptId, int unitId)
        {
            SQLiteCommand getThemeResults
                = new SQLiteCommand("SELECT * FROM theme_results WHERE attempt_id = @attempt_id AND unit_id = @unit_id;", Database);
            getThemeResults.Parameters.Add("@attempt_id", DbType.Int32).Value = attemptId;
            getThemeResults.Parameters.Add("@unit_id", DbType.Int32).Value = unitId;
            SQLiteDataReader reader = getThemeResults.ExecuteReader();
            List<DiSpaceThemeResult> list = new List<DiSpaceThemeResult>();
            while (reader.Read()) list.Add(new DiSpaceThemeResult(this, reader));
            return list.ToArray();
        }
        internal DiSpaceAnswer[] GetAnswersInternal(int attemptId, int themeId)
        {
            SQLiteCommand getAnswers
                = new SQLiteCommand("SELECT * FROM answers WHERE attempt_id = @attempt_id AND theme_id = @theme_id;", Database);
            getAnswers.Parameters.Add("@attempt_id", DbType.Int32).Value = attemptId;
            getAnswers.Parameters.Add("@theme_id", DbType.Int32).Value = themeId;
            SQLiteDataReader reader = getAnswers.ExecuteReader();
            List<DiSpaceAnswer> list = new List<DiSpaceAnswer>();
            while (reader.Read()) list.Add(DiSpaceAnswer.Resolve(this, reader));
            return list.ToArray();
        }

        public DiSpaceAnswer[] GetAnswersByQuestion(int questionId)
        {
            SQLiteCommand getAnswers
                = new SQLiteCommand("SELECT * FROM answers WHERE question_id = @question_id;", Database);
            getAnswers.Parameters.Add("@question_id", DbType.Int32).Value = questionId;
            SQLiteDataReader reader = getAnswers.ExecuteReader();
            List<DiSpaceAnswer> list = new List<DiSpaceAnswer>();
            while (reader.Read()) list.Add(DiSpaceAnswer.Resolve(this, reader));
            return list.ToArray();
        }
        public DiSpaceTheme[] SearchThemes(string likePattern)
        {
            SQLiteCommand searchThemes = new SQLiteCommand($"SELECT * FROM themes WHERE name LIKE @like;", Database);
            searchThemes.Parameters.Add("@like", DbType.String).Value = likePattern;
            SQLiteDataReader reader = searchThemes.ExecuteReader();
            List<DiSpaceTheme> list = new List<DiSpaceTheme>();
            while (reader.Read()) list.Add(new DiSpaceTheme(this, reader));
            return list.ToArray();
        }
        public DiSpaceQuestion[] SearchQuestionsByThemeName(string themeName)
        {
            SQLiteCommand searchQuestions = new SQLiteCommand($"SELECT * FROM questions WHERE theme_id IN (SELECT id FROM themes WHERE name LIKE @like) GROUP BY prompt;", Database);
            searchQuestions.Parameters.Add("@like", DbType.String).Value = themeName;
            SQLiteDataReader reader = searchQuestions.ExecuteReader();
            List<DiSpaceQuestion> list = new List<DiSpaceQuestion>();
            while (reader.Read()) list.Add(DiSpaceQuestion.Resolve(this, reader));
            return list.ToArray();
        }

        public DiSpaceTest[] SearchTests(string substring)
        {
            SQLiteCommand searchThemes = new SQLiteCommand("SELECT * FROM tests WHERE CIC(name, @substring);", Database);
            searchThemes.Parameters.Add("@substring", DbType.String).Value = substring;
            SQLiteDataReader reader = searchThemes.ExecuteReader();
            List<DiSpaceTest> list = new List<DiSpaceTest>();
            while (reader.Read()) list.Add(new DiSpaceTest(this, reader));
            return list.ToArray();
        }

        public DiSpaceAttempt[] GetAttemptsByTestId(int testId)
        {
            SQLiteCommand getAttempts = new SQLiteCommand("SELECT * FROM attempts WHERE test_id = @test_id;", Database);
            getAttempts.Parameters.Add("@test_id", DbType.Int32).Value = testId;
            SQLiteDataReader reader = getAttempts.ExecuteReader();
            List<DiSpaceAttempt> list = new List<DiSpaceAttempt>();
            while (reader.Read())
            {
                int id = reader.GetSqliteInt32(0);
                if (!attempts.TryGetValue(id, out DiSpaceAttempt? attempt))
                    attempt = new DiSpaceAttempt(this, reader);
                list.Add(attempt);
            }
            return list.ToArray();
        }


    }
    public class CaseInsensitiveSearch : SQLiteFunction
    {
        public override object Invoke(object[] args)
            => (args[0] as string)?.Contains(args[1] as string ?? string.Empty, StringComparison.InvariantCultureIgnoreCase) ?? false;
    }
    public static class Extensions
    {
        public static int GetSqliteInt32(this IDataRecord record, int field) => (int)record.GetInt64(field);
        public static int? GetSqliteInt32OrNull(this IDataRecord record, int field) => record.IsDBNull(field) ? null : (int)record.GetInt64(field);
        public static DateTimeOffset GetSqliteDateTime(this IDataRecord record, int field)
            => DateTimeOffset.FromUnixTimeSeconds(record.GetInt64(field));
        public static DateTimeOffset? GetSqliteDateTimeOrNull(this IDataRecord record, int field)
            => record.IsDBNull(field) ? null : DateTimeOffset.FromUnixTimeSeconds(record.GetInt64(field));
        public static string GetSqliteText(this IDataRecord record, int field) => record.GetString(field);
        public static string? GetSqliteTextOrNull(this IDataRecord record, int field) => record.IsDBNull(field) ? null : record.GetString(field);
        public static bool GetSqliteBoolean(this IDataRecord record, int field) => record.GetInt64(field) is 1;
        public static float GetSqliteFloat(this IDataRecord record, int field) => record.GetFloat(field);
        public static float? GetSqliteFloatOrNull(this IDataRecord record, int field) => record.IsDBNull(field) ? null : record.GetFloat(field);
    }
}
