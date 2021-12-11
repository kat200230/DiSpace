using System;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.SQLite;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using DiSpaceCore;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Emzi0767;
using static System.Net.Mime.MediaTypeNames;

namespace DiPeek
{
    public static class Program
    {
        private static string FindInParents(string fileName)
        {
            string curDir = Directory.GetCurrentDirectory();
            string path = Path.Combine(curDir, fileName);
            while (!File.Exists(path))
            {
                curDir = Path.GetDirectoryName(curDir)
                    ?? throw new InvalidOperationException($"Could not find {fileName} anywhere in the file tree.");
                path = Path.Combine(curDir, fileName);
            }
            return path;
        }
        public static async Task Main(/* string[] args */)
        {
            string dbPath = FindInParents("dispace.sqlite");
            string configPath = FindInParents("config.xml");

            using (SQLiteConnection connection = new SQLiteConnection($"Data Source={dbPath};Version=3;FailIfMissing=True"))
            {
                DiSpaceClient dispace = new DiSpaceClient(connection);

                BotConfig config;
                XmlSerializer ser = new XmlSerializer(typeof(BotConfig));
                using (XmlReader reader = XmlReader.Create(configPath))
                    config = (BotConfig)ser.Deserialize(reader)!;

                DiscordClient discord = new DiscordClient(new DiscordConfiguration { Token = config.Token });

                DiPeekBot bot = new DiPeekBot(dispace, discord, config);

                await bot.ConnectAsync();

                await Task.Delay(-1);
            }

        }
    }
    public class DiPeekBot
    {
        public DiPeekBot(DiSpaceClient dispace, DiscordClient discord, BotConfig config)
        {
            DiSpace = dispace;
            Discord = discord;
            Config = config;

			discord.MessageCreated += DiscordOnMessageCreated;
        }
        public DiSpaceClient DiSpace { get; }
		public DiscordClient Discord { get; }
		public BotConfig Config { get; }

        private static readonly List<Exception> exceptions = new List<Exception>();

        public async Task ConnectAsync()
        {
            await Task.WhenAll(DiSpace.ConnectAsync(),
                               Discord.ConnectAsync());
        }

        private Task DiscordOnMessageCreated(DiscordClient sender, MessageCreateEventArgs e)
        {
            Task.Run(async () =>
            {
                if (e.Channel.IsPrivate) return;
                string[] args = e.Message.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (args.Length > 0)
                {
                    string cmd = args[0];
                    if (!cmd.StartsWith(Config.Prefix, StringComparison.InvariantCultureIgnoreCase)) return;
                    cmd = cmd[Config.Prefix.Length..];
                    CommandEventArgs cmdArgs = new CommandEventArgs(e, cmd, args.Skip(1));
                    try
                    {
                        await DiscordOnCommand(cmdArgs);
                    }
                    catch (Exception e)
                    {
                        await cmdArgs.Respond("Произошла ошибка при обработке запроса!", $"`{e.Message}`", "Информация об ошибке была записана в лог.");
                        exceptions.Add(e);
                    }
                }
            });
            return Task.CompletedTask;
        }
        private async Task DiscordOnCommand(CommandEventArgs e)
        {
            if (e.MatchCommand("help", "h", "?"))
            {
                await e.Respond("**`help` - список команд DiPeek:**",
                                "**`test <ID>`** - показывает инфу о тесте с этим ID.",
                                "**`test <ID> txt`** - вытягивает данные о тесте с этим ID в текстовый файл.",
                                "**`test search <term>`** - ищет тесты по названию.",
                                "**`theme search <term>`** - ищет темы с заданным запросом в названии (P.S.: не у всех тем выставлены названия).",
                                "**`question <ID>`** - показывает инфу об открытом вопросе с этим ID.",
								"**`version`** - показывает версию и дату последнего обновления базы данных.",
                                "Введите команду без аргументов для более подробной информации.");
            }
			else if (e.MatchCommand("version", "v"))
            {
                await e.Respond($"Текущая версия DiPeek:\n{GetVersion()}");
            }
            else if (e.MatchCommand("test", "t"))
            {
                if (!e.HasNextArgument || e.MatchArgument("help", "h", "?"))
                {
                    await e.Respond("**Команда `test`:**",
                                    "**`test <ID>`** - показывает инфу о тесте с этим ID.",
									"**`test <ID> text`** - отправляет файл с ответами на тест. Позже сделаю в MD и HTML форматах.",
                                    "**`test search <term>`** - ищет тест по названию. Может занять некоторое время.");
                }
                if (e.MatchArgument("search"))
                {
                    if (!e.HasNextArgument)
                    {
                        await e.Respond("Вы ничего не ввели. Введите запрос для поиска.");
                        return;
                    }
                    string term = e.NextArgument()!;
                    while (e.HasNextArgument) term += $" {e.NextArgument()}";
                    if (term.Length < 2)
                    {
                        await e.Respond("Слишком короткий запрос. Введите как минимум 2 символа.");
                        return;
                    }
                    if (term.Length > 50)
                    {
                        await e.Respond("Слишком длинный запрос. Ввести можно максимум 50 символов.");
                        return;
                    }
                    DiSpaceTest[] tests = DiSpace.SearchTests(term);
                    if (tests.Length == 0)
                    {
                        string resp = "**Не удалось ничего найти.**\nМожете попробовать ввести корень ключевого слова";
                        if (term.Contains(' ')) resp += " или оставить только одно из слов в запросе";
                        await e.Respond(resp + ".");
                        return;
                    }
                    StringBuilder sb = new StringBuilder();
                    AppendNotice(sb);
                    string title = $"|========== ПОИСК ПО ТЕСТАМ С '{term}' ==========|";
                    sb.Append("\n" + new string('=', title.Length));
                    sb.Append("\n" + title);
                    sb.Append("\n" + new string('=', title.Length));
                    sb.Append('\n');

                    foreach (DiSpaceTest test2 in tests)
                    {
                        sb.Append($"\n===== Тест \"{test2.Name}\" (ID: {test2.Id}) =====");
                    }

                    sb.Append('\n', 2);
                    AppendNotice(sb);
                    await e.RespondFile("test_search.txt", sb.ToString());
                    return;

                }
                if (!e.MatchNumberArgument(out int testId))
                {
                    await e.Respond($"Не удалось распознать `{e.PeekArgument().Limit(20)}` как число.",
                                    "Предлагаю следующее решение: введите число. 😉");
                    return;
                }
                if (!DiSpace.TryGetTest(testId, out DiSpaceTest? test))
                {
                    await e.Respond($"Не удалось найти тест с ID {testId} в срезе "
                                  + $"[{DiSpace.GetFirstAttempt().StartedAt:yyyy/MM/dd} - {DiSpace.GetLastAttempt().StartedAt:yyyy/MM/dd}].",
                                    "Что может быть не так:",
									"- Вы ввели что-то не то; *(позже сделаю дополнительную проверку тут)*",
                                    "- Ещё никто не проходил этот тест, либо результаты были подтёрты;",
                                    "- Тесту больше 13 лет, или же он очень свежий;",
                                    "- Тест был переайдирован, в таком случае ищите по названию;",
                                    $"Последняя запись в БД датируется: **{DiSpace.GetLastAttempt().StartedAt:yyyy/MM/dd hh:mm:ss}**.");
                    return;
                }

                if (!e.HasNextArgument)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append($"ПРИМЕЧАНИЕ: Используйте \"/test {test.Id}\" txt для вывода ответов.\n\n");
                    sb.Append($"Зарегистрированных попыток: {test.Attempts.Count}.").Append('\n');
                    if (test.Attempts.Count < 5)
                    {
                        sb.Append("!!! ПРЕДУПРЕЖДЕНИЕ: Попыток мало, и некоторые вопросы могли быть не раскрыты.").Append('\n');
                        sb.Append("!!! Это также применимо и ко всем другим тестам, но тут будьте особенно осторожны.").Append('\n', 2);
                    }
                    AppendNotice(sb);
                    AppendTestStructure(sb, test);
                    AppendNotice(sb);
                    await e.RespondFile($"test_{test.Id}_contents.txt", sb.ToString());
                    return;
                }
                else if (e.MatchArgument("text", "txt"))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append($"Зарегистрированных попыток: {test.Attempts.Count}.").Append('\n');
                    if (test.Attempts.Count < 5)
                    {
                        sb.Append("!!! ПРЕДУПРЕЖДЕНИЕ: Попыток мало, и некоторые вопросы могли быть не раскрыты.").Append('\n');
                        sb.Append("!!! Это также применимо и ко всем другим тестам, но тут будьте особенно осторожны.").Append('\n', 2);
                    }
                    AppendNotice(sb);
                    AppendTest(sb, test);
                    AppendNotice(sb);

                    await e.RespondFile($"test_{test.Id}.txt", sb.ToString());
                }

            }
			else if (e.MatchCommand("question", "qu"))
            {
                if (!e.HasNextArgument)
                {
                    await e.Respond("Используйте эту команду с айди открытых вопросов.");
                    return;
                }
                if (!e.MatchNumberArgument(out int questionId))
                {
                    await e.Respond($"Не удалось распознать `{e.PeekArgument().Limit(20)}` как число.",
                                    "Предлагаю следующее решение: введите число. 😉");
                    return;
                }
                if (!DiSpace.TryGetQuestion(questionId, out DiSpaceQuestion? qu))
                {
                    await e.Respond("Не удалось найти вопрос с введённым айди.");
                    return;
                }

                if (qu.Type is DiSpaceQuestionType.OpenQuestion)
                {
                    DiSpaceOpenQuestionAnswer[] answers = DiSpace.GetAnswersByQuestion(questionId).OfType<DiSpaceOpenQuestionAnswer>().ToArray();
                    if (answers.Length == 0)
                    {
                        await e.Respond("Не удалось ничего найти.");
                        return;
                    }
                    DiSpaceOpenQuestion question = answers[0].Question;

                    StringBuilder sb = new StringBuilder();
                    AppendNotice(sb);
                    string title = $"|========== ВОПРОС (ID: {questionId}) ==========|";
                    sb.Append("\n" + new string('=', title.Length));
                    sb.Append("\n" + title);
                    sb.Append("\n" + new string('=', title.Length));

                    sb.Append($"\n\n{question.Prompt.Clean()}\n");

                    foreach (DiSpaceOpenQuestionAnswer answer in answers.OrderByDescending(static a => a.Score))
                    {
                        string response = answer.Response;
                        if (string.IsNullOrWhiteSpace(response)) continue;
                        string maxStr = question.MaxScore.HasValue ? question.MaxScore.GetValueOrDefault().ToString("N2") : "???";
                        sb.Append($"\n\n===== {answer.Score:N2}/{maxStr} | Ответ из попытки с ID: {answer.AttemptId} ====\n\n");

                        sb.Append(response.Replace("\n\n", "\n"));
                    }

                    sb.Append('\n', 2);
                    AppendNotice(sb);

                    await e.RespondFile($"question_{questionId}.txt", sb.ToString());
                }
                else
                {
					StringBuilder sb = new StringBuilder();
						string maxStr = qu.MaxScore.HasValue ? qu.MaxScore.GetValueOrDefault().ToString("N2") : "???";
                        sb.Append($"**\"{qu.Title}\" (макс. {maxStr} б.)**:\n{qu.Prompt.Clean().Limit(600)}");

                        if (qu is DiSpaceSimpleQuestion simple)
                        {
                            foreach (DiSpaceSimpleOption option in simple.Options)
                            {
                                string opt = $"- {option.Text.Clean()} - ({option.Score:N2} б.)";
                                opt = option.IsCorrect ? $"**{opt} - правильный ответ**" : $"*{opt}*";
                                sb.Append("\n" + opt);
                            }
                        }
                        else if (qu is DiSpacePairQuestion pair)
                        {
                            foreach (DiSpacePairOption pairOption in pair.Options)
                                sb.Append($"\n- {pairOption.Text.Clean()}");
                            sb.Append("\n\n**Правильные соотношения:**");
                            foreach (Pair<DiSpacePairOption> p in pair.Correct)
                                sb.Append($"\n**- {p.A.Text.Clean()} <=> {p.B.Text.Clean()}**;");
                        }
                        else if (qu is DiSpaceAssociativeQuestion associative)
                        {
                            sb.Append("\nСтроки:");
                            foreach (DiSpaceAssociativeRow row in associative.Rows)
                                sb.Append($"\n- {row.Text.Clean()}");
                            sb.Append("\nСтолбцы:");
                            foreach (DiSpaceAssociativeColumn column in associative.Columns)
                                sb.Append($"\n- {column.Text.Clean()}");
                            sb.Append("\n\n**Правильные соотношения:**");
                            foreach (DiSpaceAssociativeChoice p in associative.Correct)
                                sb.Append($"\n**- {p.Row.Text.Clean()} <=> {p.Column.Text.Clean()}**;");
                        }
                        else if (qu is DiSpaceOrderQuestion order)
                        {
                            sb.Append($"**\n\nПравильный порядок:**");
                            foreach (DiSpaceOrderOption option in order.Options)
                                sb.Append($"\n**{option.Text.Clean()}**;");
                        }
                        else if (qu is DiSpaceCustomInputQuestion customInput)
                        {
                            sb.Append("**\n\nШаблоны правильных ответов:**");
                            foreach (DiSpaceCustomInputPattern pattern in customInput.Correct)
                                sb.Append($"\n**- `{pattern.Pattern}` ({pattern.Score:N2} б.)**;");
                        }

                        string text = sb.ToString();
                        if (text.Length < 1500) await e.Respond(text);
                        else await e.RespondFile($"answer_{questionId}.txt", text);
                }


            }
			else if (e.MatchCommand("theme", "th"))
            {
                if (e.MatchArgument("search", "s"))
                {
                    if (!e.HasNextArgument)
                    {
                        await e.Respond("Вы ничего не ввели. Введите запрос для поиска.");
                        return;
                    }
                    string term = e.NextArgument()!;
                    while (e.HasNextArgument) term += $" {e.NextArgument()}";
                    if (term.Length < 3)
                    {
                        await e.Respond("Слишком короткий запрос. Введите как минимум 3 символа.");
                        return;
                    }
                    if (term.Length > 50)
                    {
                        await e.Respond("Слишком длинный запрос. Ввести можно максимум 50 символов.");
                        return;
                    }
                    DiSpaceTheme[] themes = DiSpace.SearchThemes($"%{term}%");
                    if (themes.Length == 0)
                    {
                        await e.Respond("Не удалось ничего найти.");
                        return;
                    }

                    StringBuilder sb = new StringBuilder();
                    sb.Append($"**Темы с \"{term}\" в названии:**");
                    foreach (DiSpaceTheme theme in themes)
                    {
                        sb.Append($"\n- {theme.Name.Replace("\n", string.Empty)} ({theme.Hash}, в тесте {theme.Unit.TestId}) содержит {theme.Questions.Count} вопросов.");
                    }
                    string text = sb.ToString();
                    await e.RespondFile("theme_search.txt", text);
                }
            }
        }

        public string GetVersion()
        {
            DiSpaceAttempt last = DiSpace.GetLastAttempt();
            return $"v{last.Id}, {last.StartedAt:d MMMM yyyy}";
        }

        public void AppendNotice(StringBuilder sb)
        {
            sb.Append("Извлечено с помощью DiPeek: https://discord.gg/tphsh9vsty\n");
        }

        public void AppendTest(StringBuilder sb, DiSpaceTest test, bool cascade = true)
        {
            if (cascade)
            {
                StringBuilder tb = new StringBuilder();
                tb.Append("||||| ТЕСТ ");
                if (test.Name is null) tb.Append("(название неизвестно)");
                else tb.Append('"').Append(test.Name.Clean()).Append('"');
                tb.Append($" (ID: {test.Id}) |||||");

                sb.Append('=', tb.Length);
                sb.Append('\n').Append(tb.ToString());
                sb.Append('\n').Append('=', tb.Length).Append('\n', 2);

                if (test.Units.Count is 0)
                {
                    sb.Append("Тест пустой. Неожиданно.\n");
                    sb.Append("Единственное объяснение, которое приходит в голову - это то, что составитель загрузил пустой файл.\n");
                    sb.Append("Это также означает, что никто и не проходил этот тест. Вообщем, смотрите другие тесты. Этот - пустышка.").Append('\n', 2);
                }
                foreach (DiSpaceUnit unit in test.Units)
                    AppendUnit(sb, unit);
            }
            else
            {
                sb.Append("===== ТЕСТ ");
                if (test.Name is null) sb.Append("(название неизвестно)");
                else sb.Append('"').Append(test.Name.Clean()).Append('"');
                sb.Append($" (ID: {test.Id}) =====").Append('\n', 2);
            }
        }
        public void AppendUnit(StringBuilder sb, DiSpaceUnit unit, bool cascade = true)
        {
            sb.Append("===== РАЗДЕЛ ");
            if (unit.Name is null) sb.Append("(без названия)");
            else sb.Append('"').Append(unit.Name.Clean()).Append('"');
            sb.Append($" (Хэш: {unit.Hash}");
            if (unit.IsShuffled) sb.Append(", ВПЕРЕМЕШКУ");
            sb.Append(") =====");
            if (unit.Description is not null)
                sb.Append('\n').Append(unit.Description.Clean());
            sb.Append('\n', 2);

            if (cascade)
            {
                foreach (DiSpaceTheme theme in unit.Themes)
                    AppendTheme(sb, theme);
            }
        }
        public void AppendTheme(StringBuilder sb, DiSpaceTheme theme, bool cascade = true)
        {
            sb.Append("===== Тема ");
            if (theme.Name is null) sb.Append("(без названия)");
            else sb.Append('"').Append(theme.Name.Clean()).Append('"');
            sb.Append($" (Хэш: {theme.Hash}");
            if (theme.IsShuffled) sb.Append(", ВПЕРЕМЕШКУ");
            sb.Append(") =====");
            if (theme.Description is not null)
                sb.Append('\n').Append(theme.Description.Clean());
            sb.Append('\n', 2);

            if (cascade)
            {
                foreach (DiSpaceQuestion question in theme.Questions)
                    AppendQuestion(sb, question);
            }
        }
        public void AppendQuestion(StringBuilder sb, DiSpaceQuestion question)
        {
            string maxStr = question.MaxScore.HasValue ? question.MaxScore.GetValueOrDefault().ToString("N2") : "???";
            sb.Append($"- Вопрос \"{question.Title.Clean()}\" (макс. {maxStr} б.):\n- {question.Prompt.Clean()}");

            if (question is DiSpaceSimpleQuestion simple)
            {
                int count = simple.Correct.Count;
                sb.Append(count is 1 ? "\nПравильный ответ:" : "\nПравильные ответы:");
                if (count is 1)
                {
                    sb.Append($"\n- {simple.Correct[0].Text.Clean()}.");
                }
				else
                    for (int i = 0; i < count; i++)
                    {
                        sb.Append($"\n- {simple.Correct[i].Text.Clean()}");
                        sb.Append(i + 1 == count ? '.' : ';');
                    }
            }
            else if (question is DiSpacePairQuestion pair)
            {
                int count = pair.Correct.Count;
                sb.Append("\nПравильные пары:");
                for (int i = 0; i < count; i++)
                {
                    Pair<DiSpacePairOption> c = pair.Correct[i];
                    sb.Append($"\n- {c.A.Text.Clean()} <=> {c.B.Text.Clean()};");
                    sb.Append(i + 1 == count ? '.' : ';');
                }
            }
            else if (question is DiSpaceAssociativeQuestion associative)
            {
                int count = associative.Correct.Count;
                sb.Append("\nПравильные ассоциации:");
                for (int i = 0; i < count; i++)
                {
                    DiSpaceAssociativeChoice c = associative.Correct[i];
                    sb.Append($"\n- {c.Row.Text.Clean()} <=> {c.Column.Text.Clean()}");
                    sb.Append(i + 1 == count ? '.' : ';');
                }
            }
            else if (question is DiSpaceOrderQuestion order)
            {
                sb.Append("\nПравильный порядок:");
                List<DiSpaceOrderOption> options = order.Options.ToList();
                options.Sort(static (a, b) => a.CorrectIndex.CompareTo(b.CorrectIndex));
                for (int i = 0; i < options.Count; i++)
                {
                    sb.Append($"\n{i + 1}. {options[i].Text.Clean()}");
                    sb.Append(i + 1 == options.Count ? '.' : ';');
                }

            }
            else if (question is DiSpaceCustomInputQuestion customInput)
            {
                int count = customInput.Correct.Count;
                sb.Append(count is 1 ? "\nШаблон правильного ответа:" : "\nШаблоны правильных ответов:");
                List<DiSpaceCustomInputPattern> patterns = customInput.Correct.ToList();
                patterns.Sort(static (a, b) => -a.Score.CompareTo(b.Score));
                for (int i = 0; i < count; i++)
                {
                    DiSpaceCustomInputPattern pattern = customInput.Correct[i];
                    sb.Append($"\n- `{pattern.Pattern}` ({(pattern.Score > 0 ? "+" : "-")}{Math.Abs(pattern.Score)} б.)");
                    sb.Append(i + 1 == count ? '.' : ';');
                }

            }
            else if (question is DiSpaceOpenQuestion open)
            {
                DiSpaceOpenQuestionAnswer[] answers = DiSpace.GetAnswersByQuestion(question.Id)
                                                             .OfType<DiSpaceOpenQuestionAnswer>().ToArray();
                if (answers.Length is 0)
                {
                    sb.Append($"\nНедоступно ни одного ответа. ({GetVersion()})");
                    return;
                }
                sb.Append($"\nОтвет: используйте /question {open.Id} для просмотра {answers.Length} ответов.");
                sb.Append($"\n{answers.Count(static a => a.Score > 0f)}/{answers.Length} оцененных.");
                sb.Append($"\n{answers.Count(static a => !string.IsNullOrWhiteSpace(a.Response))}/{answers.Length} непустых.");
            }
            sb.Append('\n', 2);
        }

        public void AppendTestStructure(StringBuilder sb, DiSpaceTest test)
        {
            StringBuilder tb = new StringBuilder();
            tb.Append("||||| ТЕСТ ");
            if (test.Name is null) tb.Append("(название неизвестно)");
            else tb.Append('"').Append(test.Name.Clean()).Append('"');
            tb.Append($" (ID: {test.Id}) |||||");

            sb.Append('=', tb.Length);
            sb.Append('\n').Append(tb.ToString());
            sb.Append('\n').Append('=', tb.Length).Append('\n', 3);

            if (test.Units.Count is 0)
            {
                sb.Append("Тест пустой. Неожиданно.\n");
                sb.Append("Единственное объяснение, которое приходит в голову - это то, что составитель загрузил пустой файл.\n");
                sb.Append("Это также означает, что никто и не проходил этот тест. Вообщем, смотрите другие тесты. Этот - пустышка.").Append('\n', 2);
            }
            foreach (DiSpaceUnit unit in test.Units)
            {
                sb.Append("\n===== РАЗДЕЛ ");
                if (unit.Name is null) sb.Append("(без названия)");
                else sb.Append('"').Append(unit.Name.Clean()).Append('"');
                sb.Append($" (Хэш: {unit.Hash}) =====").Append('\n', 2);
                if (unit.Description is not null)
                    sb.Append('\n').Append(unit.Description.Clean());

                foreach (DiSpaceTheme theme in unit.Themes)
                {
                    sb.Append("\n- Тема ");
                    if (theme.Name is null) sb.Append("(без названия)");
                    else sb.Append('"').Append(theme.Name.Clean()).Append('"');
                    sb.Append($" (Хэш: {theme.Hash}, {theme.Questions.Count} вопросов)").Append('\n');
                    if (theme.Description is not null)
                        sb.Append('\n').Append(theme.Description.Clean());
                }
            }
            sb.Append('\n', 2);
        }

    }
    public class BotConfig
    {
        [field: XmlElement("Token")] public string Token { get; set; } = null!;
        [field: XmlElement("Prefix")] public string Prefix { get; set; } = null!;
    }
    public class CommandEventArgs
    {
        public CommandEventArgs(MessageCreateEventArgs e, string command, IEnumerable<string> arguments)
        {
            E = e;
            Command = command;
            Arguments = new Queue<string>(arguments);
        }
        public string Command { get; }
        public bool MatchCommand(string command) => string.Equals(Command, command, StringComparison.InvariantCultureIgnoreCase);
        public bool MatchCommand(string command, params string[] aliases)
        {
            if (string.Equals(Command, command, StringComparison.InvariantCultureIgnoreCase)) return true;
            return aliases.Any(alias => string.Equals(Command, alias, StringComparison.InvariantCultureIgnoreCase));
        }

		public Queue<string> Arguments { get; }
        public bool HasNextArgument => Arguments.Count > 0;
        public string? PeekArgument() => Arguments.TryPeek(out string? arg) ? arg : null;
        public string? NextArgument() => Arguments.TryDequeue(out string? arg) ? arg : null;
        public bool MatchArgument(string argument)
        {
            bool res = Arguments.TryPeek(out string? arg) && string.Equals(arg, argument, StringComparison.InvariantCultureIgnoreCase);
            if (res) Arguments.Dequeue();
            return res;
        }
        public bool MatchArgument(string argument, params string[] aliases)
        {
            if (!Arguments.TryPeek(out string? arg)) return false;
            bool res = string.Equals(arg, argument, StringComparison.InvariantCultureIgnoreCase)
                    || aliases.Any(alias => string.Equals(arg, alias, StringComparison.InvariantCultureIgnoreCase));
            if (res) Arguments.Dequeue();
            return res;
        }
        public bool MatchNumberArgument(out int argument)
        {
            if (Arguments.TryPeek(out string? nextArgument) && int.TryParse(nextArgument, out int parsed))
            {
                Arguments.Dequeue();
                argument = parsed;
                return true;
            }
            argument = default;
            return false;
        }

        public bool CheckArgument([NotNullWhen(true)] out string? argument, Func<string, bool> argumentChecker)
        {
            bool res = Arguments.TryPeek(out string? nextArgument) && argumentChecker(nextArgument);
            argument = res ? Arguments.Dequeue() : null;
            return res;
        }

        private readonly MessageCreateEventArgs E;
        public DiscordChannel Channel => E.Channel;
        public DiscordUser Author => E.Author;

        public Task<DiscordMessage> Respond(params string[] lines) => Channel.SendMessageAsync(string.Join('\n', lines));
        public async Task<DiscordMessage> RespondFile(string fileName, string text)
        {
            DiscordMessageBuilder dmb = new DiscordMessageBuilder();
            MemoryStream stream = new MemoryStream();
            stream.Write(Encoding.UTF8.GetBytes(text));
            stream.Seek(0, SeekOrigin.Begin);
            dmb.WithFile(fileName, stream);
            DiscordMessage msg = await Channel.SendMessageAsync(dmb);
            await stream.DisposeAsync();
            return msg;
        }

    }
    public static class UsefulExtensions
    {
        public static string Limit(this string? str, int maxLength)
        {
            if (str is null) return string.Empty;
			return str.Length <= maxLength ? str : str[..(maxLength - 1)] + "…";
        }
        private static readonly Regex imgRegex = new Regex("<img.*?src=\"(.+?)\".*?/?>");
        private static readonly Regex garbageRegex = new Regex("<(?:span|sup|style).*?/?>");
        public static string Clean(this string? str)
        {
            if (str is null) return string.Empty;
            RemoveFromString(ref str, "<div>", "</div>");
            RemoveFromString(ref str, "<p>", "</p>");
            RemoveFromString(ref str, "<span>", "</span>");
            RemoveFromString(ref str, "<sup>", "</sup>");
            RemoveFromString(ref str, "<style>", "</style>");
            RemoveFromString(ref str, "<em>", "</em>");
            RemoveFromString(ref str, "<strong>", "</strong>");
            RemoveFromString(ref str, "<br>", "<br/>");
            str = str.Replace("&nbsp;", " ");
            str = str.Replace("\n", " ");
            str = imgRegex.Replace(str, " https://dispace.edu.nstu.ru/$1 ");
            str = garbageRegex.Replace(str, string.Empty);
            return str.Trim();
        }
        private static void RemoveFromString(ref string str, params string[] remove)
        {
            foreach (string part in remove)
                str = str.Replace(part, string.Empty);
        }
    }
}