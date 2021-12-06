using System;
using System.Data.SQLite;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using DiSpaceCore;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

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

        public async Task ConnectAsync()
        {
            await Task.WhenAll(DiSpace.Database.OpenAsync(),
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

                    }
                }
            });
            return Task.CompletedTask;
        }
        private async Task DiscordOnCommand(CommandEventArgs e)
        {
            if (e.MatchCommand("help", "h", "?"))
            {
                await e.Respond("**Список команд DiPeek:**",
                                "**`test <ID>`** - показывает инфу о тесте с этим ID.",
                                "***`guide <ID>`** - Work-In-Progress.*",
                                "Введите команду без аргументов для более подробной информации.");
            }
            else if (e.MatchCommand("guide", "g"))
            {
                await e.Respond("**Команда `guide`:**",
								"**`guide <ID>`** - эта фича будет служить вам гайдом для прохождения тестов.",
                                "Вводите айди теста, а потом пишите первые несколько слов попавшегося вам вопроса, и бот ищет вопрос, соответствующий тому, что вы написали, и отправляет ответ на него. Надо не забыть сделать статистическую проверку теста, чтобы примерно узнать, какой процент вопросов покрыт базой данных, чтобы люди сразу знали, что может попасться непокрытый вопрос.",
                                "***Work-In-Progress***");
            }
            else if (e.MatchCommand("test", "t"))
            {
                if (!e.HasNextArgument || e.MatchArgument("help", "h", "?"))
                {
                    await e.Respond("**Команда `test`:**",
                                    "**`test <ID>`** - показывает инфу о тесте с этим ID.",
									"**`test <ID> text`** - отправляет файл с ответами на тест. Позже сделаю в MD и HTML форматах.");
                }
                if (!e.MatchNumberArgument(out int testId))
                {
                    await e.Respond($"Не удалось распознать `{e.PeekArgument().Limit(20)}` как число.",
                                    "Предлагаю следующее решение: введите число. 😉");
                    return;
                }
                if (!DiSpace.TryGetTest(testId, out DiSpaceTest? test))
                {
                    await e.Respond($"Не удалось найти тест с ID {testId} в срезе"
                                  + $"[{DiSpace.GetFirstAttempt().StartedAt:yyyy/MM/dd}-{DiSpace.GetLastAttempt().StartedAt:yyyy/MM/dd}].",
                                    "Что может быть не так:",
									"- Вы ввели что-то не то; *(позже сделаю дополнительную проверку тут)*",
                                    "- Ещё никто не проходил этот тест, либо результаты были стёрты;",
                                    "- Тесту больше 13 лет, или же он очень свежий;",
                                    $"Последняя запись в БД датируется: **{DiSpace.GetLastAttempt().StartedAt:yyyy/MM/dd hh:mm:ss}**.");
                    return;
                }

                if (!e.HasNextArgument)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append($"Тест (ID: {test.Id}):");
                    foreach (DiSpaceUnit unit in test.Units)
                    {
                        sb.Append($"\n--- Раздел \"{unit.Name ?? "*(без названия)*"}\" (Hash: {unit.Hash}");
                        if (unit.IsShuffled) sb.Append(", вперемешку");
                        sb.Append("):");
                        if (!string.IsNullOrWhiteSpace(unit.Description)) sb.Append($"\n--- {CleanString(unit.Description).Limit(100)}.");

                        foreach (DiSpaceTheme theme in unit.Themes)
                        {
                            sb.Append($"\n--- --- Тема \"{theme.Name ?? "*(без названия)*"}\" (Hash: {theme.Hash}");
                            if (theme.IsShuffled) sb.Append(", вперемешку");
                            sb.Append("):");
                            if (!string.IsNullOrWhiteSpace(theme.Description)) sb.Append($"\n--- --- {CleanString(theme.Description).Limit(100)}.");

                            sb.Append($"\n--- --- --- {theme.Questions.Count} вопросов.");
                        }
                    }
                    await e.Respond(sb.ToString().Limit(2000));
                }
				else if (e.MatchArgument("text", "txt"))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("Извлечено с помощью DiPeek: https://discord.gg/tphsh9vsty");
                    string title = $"|========== ТЕСТ (ID: {test.Id}) ==========|";
                    sb.Append("\n" + new string('=', title.Length));
                    sb.Append("\n" + title);
                    sb.Append("\n" + new string('=', title.Length));

                    foreach (DiSpaceUnit unit in test.Units)
                    {
                        sb.Append($"\n\n========== РАЗДЕЛ ");
                        if (unit.Name is not null) sb.Append($"\"{unit.Name}\" ");
                        sb.Append($"(Хэш: {unit.Hash}");
                        if (unit.IsShuffled) sb.Append(", вперемешку");
                        sb.Append(") ==========");
                        if (unit.Description is not null) sb.Append($"\n--- {unit.Description}.");

                        foreach (DiSpaceTheme theme in unit.Themes)
                        {
                            sb.Append($"\n\n===== Тема ");
                            if (theme.Name is not null) sb.Append($"\"{theme.Name}\" ");
                            sb.Append($"(Хэш: {theme.Hash}");
                            if (theme.IsShuffled) sb.Append(", вперемешку");
                            sb.Append(") =====");
                            if (theme.Description is not null) sb.Append($"\n--- {theme.Description}.");

                            if (theme.Questions.Count > 0) sb.Append('\n');
                            int i = 0;
                            foreach (DiSpaceQuestion question in theme.Questions)
                            {
                                string maxStr = question.MaxScore.HasValue ? question.MaxScore.GetValueOrDefault().ToString("N2") : "???";
                                sb.Append($"\n\n{++i}. \"{question.Title}\" (макс. {maxStr} б.): {CleanString(question.Prompt)}");

                                if (question is DiSpaceSimpleQuestion simple)
                                {
                                    sb.Append($"\nОтвет: {string.Join("; ", simple.Correct.Select(static o => CleanString(o.Text)))}");
                                }
								else if (question is DiSpacePairQuestion pair)
                                {
                                    sb.Append($"\nОтвет: {string.Join("; ", pair.Correct.Select(static c => $"{CleanString(c.A.Text)} <> {CleanString(c.B.Text)}"))}");
                                }
								else if (question is DiSpaceAssociativeQuestion associative)
                                {
                                    sb.Append($"\nОтвет: {string.Join("; ", associative.Correct.Select(static c => $"{CleanString(c.Row.Text)} : {CleanString(c.Column.Text)}"))}");
                                }
								else if (question is DiSpaceOrderQuestion order)
                                {
                                    sb.Append($"\nОтвет: {string.Join("; ", order.Options.Select(static o => CleanString(o.Text)))}");
                                }
								else if (question is DiSpaceCustomInputQuestion customInput)
                                {
                                    sb.Append($"\nОтвет: {string.Join("; ", customInput.Correct.Select(static p => $"`{p.Pattern}`"))}");
                                }
								else if (question is DiSpaceOpenQuestion open)
                                {
                                    // sb.Append($"\nУдачи с этим пока что. Функция просмотра чужих ответов будет добавлена позже.");
                                    DiSpaceOpenQuestionAnswer[] answers = DiSpace.GetAnswersByQuestion(question.Id)
                                                                                 .OfType<DiSpaceOpenQuestionAnswer>().ToArray();
                                    sb.Append($"\nОтвет: используйте /question {open.Id} для просмотра {answers.Length} ответов.");
                                    sb.Append($"\n{answers.Count(a => a.Score > 0f)}/{answers.Length} оцененных.");
                                    sb.Append($"\n{answers.Count(a => !string.IsNullOrWhiteSpace(a.Response))}/{answers.Length} непустых.");
                                }

                            }

                        }
                    }

                    sb.Append("\n\nИзвлечено с помощью DiPeek: https://discord.gg/tphsh9vsty");

                    string text = sb.ToString();

                    DiscordMessageBuilder dmb = new DiscordMessageBuilder();

                    MemoryStream stream = new MemoryStream();
                    stream.Write(Encoding.UTF8.GetBytes(text));
                    stream.Seek(0, SeekOrigin.Begin);
                    dmb.WithFile($"test_{test.Id}.txt", stream);
                    await e.Channel.SendMessageAsync(dmb);
                    await stream.DisposeAsync();

                }

            }
			else if (e.MatchCommand("question", "qu"))
            {
                if (!e.HasNextArgument)
                {
                    await e.Respond("Используйте эту команду с айди открытых вопросов.");
                    return;
                }
                if (e.MatchArgument("search", "s"))
                {
                    if (!e.HasNextArgument)
                    {
                        await e.Respond("Вы ничего не ввели. Введите запрос для поиска.");
                        return;
                    }
                    string term = e.NextArgument()!;
                    if (term.Length < 5)
                    {
                        await e.Respond("Слишком короткий запрос. Введите как минимум 5 символов.");
                        return;
                    }
                    DiSpaceQuestion[] questions = DiSpace.SearchQuestionsByThemeName($"%{term}%");
                    if (questions.Length == 0)
                    {
                        await e.Respond("Не удалось ничего найти.");
                        return;
                    }

                    StringBuilder sb = new StringBuilder();
                    sb.Append("Извлечено с помощью DiPeek: https://discord.gg/tphsh9vsty");
                    string title = $"|========== ПОИСК ПО ВОПРОСАМ С ТЕМАМИ '{term}' ==========|";
                    sb.Append("\n" + new string('=', title.Length));
                    sb.Append("\n" + title);
                    sb.Append("\n" + new string('=', title.Length));
                    foreach (DiSpaceQuestion question in questions)
                    {
							string maxStr = question.MaxScore.HasValue ? question.MaxScore.GetValueOrDefault().ToString("N2") : "???";
                                sb.Append($"\n\n\"{question.Title}\" (макс. {maxStr} б.):\n{CleanString(question.Prompt)}");

                                if (question is DiSpaceSimpleQuestion simple)
                                {
                                    sb.Append($"\nОтвет: {string.Join("; ", simple.Correct.Select(static o => CleanString(o.Text)))}");
                                }
								else if (question is DiSpacePairQuestion pair)
                                {
                                    sb.Append($"\nОтвет: {string.Join("; ", pair.Correct.Select(static c => $"{CleanString(c.A.Text)} <> {CleanString(c.B.Text)}"))}");
                                }
								else if (question is DiSpaceAssociativeQuestion associative)
                                {
                                    sb.Append($"\nОтвет: {string.Join("; ", associative.Correct.Select(static c => $"{CleanString(c.Row.Text)} : {CleanString(c.Column.Text)}"))}");
                                }
								else if (question is DiSpaceOrderQuestion order)
                                {
                                    sb.Append($"\nОтвет: {string.Join("; ", order.Options.Select(static o => CleanString(o.Text)))}");
                                }
								else if (question is DiSpaceCustomInputQuestion customInput)
                                {
                                    sb.Append($"\nОтвет: {string.Join("; ", customInput.Correct.Select(static p => $"`{p.Pattern}`"))}");
                                }
								else if (question is DiSpaceOpenQuestion open)
                                {
                                    // sb.Append($"\nУдачи с этим пока что. Функция просмотра чужих ответов будет добавлена позже.");
                                    DiSpaceOpenQuestionAnswer[] answers = DiSpace.GetAnswersByQuestion(question.Id)
                                                                                 .OfType<DiSpaceOpenQuestionAnswer>().ToArray();
                                    sb.Append($"\nОтвет: используйте /question {open.Id} для просмотра {answers.Length} ответов.");
                                    sb.Append($"\n{answers.Count(a => a.Score > 0f)}/{answers.Length} оцененных.");
                                    sb.Append($"\n{answers.Count(a => !string.IsNullOrWhiteSpace(a.Response))}/{answers.Length} непустых.");
                                }
                    }

                    sb.Append("\n\nИзвлечено с помощью DiPeek: https://discord.gg/tphsh9vsty");
                    string text2 = sb.ToString();
                    await e.RespondFile("question_search.txt", text2);

                    return;
                }
                else
                {
                    if (!e.MatchNumberArgument(out int questionId))
                    {
                        await e.Respond("Не удалось распознать то что вы ввели как число.");
                        return;
                    }
                    if (!DiSpace.TryGetQuestion(questionId, out DiSpaceQuestion? qu))
                    {
                        await e.Respond("Не удалось найти вопрос с введённым айди.");
                        return;
                    }

                    if (qu.Type != DiSpaceQuestionType.OpenQuestion)
                    {
                        StringBuilder sb = new StringBuilder();
						string maxStr = qu.MaxScore.HasValue ? qu.MaxScore.GetValueOrDefault().ToString("N2") : "???";
                        sb.Append($"**\"{qu.Title}\" (макс. {maxStr} б.)**:\n{CleanString(qu.Prompt)}");

                        if (qu is DiSpaceSimpleQuestion simple)
                        {
                            foreach (DiSpaceSimpleOption option in simple.Options)
                            {
                                string opt = $"- {CleanString(option.Text)} - ({option.Score:N2} б.)";
                                opt = option.IsCorrect ? $"**{opt} - правильный ответ**" : $"*{opt}*";
                                sb.Append("\n" + opt);
                            }
                        }
                        else if (qu is DiSpacePairQuestion pair)
                        {
                            foreach (DiSpacePairOption pairOption in pair.Options)
                                sb.Append($"\n- {CleanString(pairOption.Text)}");
                            sb.Append("\n\n**Правильные соотношения:**");
                            foreach (Pair<DiSpacePairOption> p in pair.Correct)
                                sb.Append($"\n**- {CleanString(p.A.Text)} <=> {CleanString(p.B.Text)}**;");
                        }
                        else if (qu is DiSpaceAssociativeQuestion associative)
                        {
                            sb.Append("\nСтроки:");
                            foreach (DiSpaceAssociativeRow row in associative.Rows)
                                sb.Append($"\n- {CleanString(row.Text)}");
                            sb.Append("\nСтолбцы:");
                            foreach (DiSpaceAssociativeColumn column in associative.Columns)
                                sb.Append($"\n- {CleanString(column.Text)}");
                            sb.Append("\n\n**Правильные соотношения:**");
                            foreach (DiSpaceAssociativeChoice p in associative.Correct)
                                sb.Append($"\n**- {CleanString(p.Row.Text)} <=> {CleanString(p.Column.Text)}**;");
                        }
                        else if (qu is DiSpaceOrderQuestion order)
                        {
                            sb.Append($"**\n\nПравильный порядок:**");
                            foreach (DiSpaceOrderOption option in order.Options)
                                sb.Append($"\n**{CleanString(option.Text)}**;");
                        }
                        else if (qu is DiSpaceCustomInputQuestion customInput)
                        {
                            sb.Append("**\n\nШаблоны правильных ответов:**");
                            foreach (DiSpaceCustomInputPattern pattern in customInput.Correct)
                                sb.Append($"\n**- `{pattern.Pattern}` ({pattern.Score:N2} б.)**;");
                        }

                        string text = sb.ToString();
                        if (text.Length < 2000) await e.Respond(text);
                        else await e.RespondFile($"answer_{questionId}.txt", text);

                    }
                    else
                    {
                        DiSpaceOpenQuestionAnswer[] answers = DiSpace.GetAnswersByQuestion(questionId).OfType<DiSpaceOpenQuestionAnswer>().ToArray();
                        if (answers.Length == 0)
                        {
                            await e.Respond("Не удалось ничего найти.");
                            return;
                        }
                        DiSpaceOpenQuestion question = answers[0].Question;

                        StringBuilder sb = new StringBuilder();
                        sb.Append("Извлечено с помощью DiPeek: https://discord.gg/tphsh9vsty");
                        string title = $"|========== ВОПРОС (ID: {questionId}) ==========|";
                        sb.Append("\n" + new string('=', title.Length));
                        sb.Append("\n" + title);
                        sb.Append("\n" + new string('=', title.Length));

                        foreach (DiSpaceOpenQuestionAnswer answer in answers.OrderByDescending(static a => a.Score))
                        {
                            string response = answer.Response;
                            if (string.IsNullOrWhiteSpace(response)) continue;
                            string maxStr = question.MaxScore.HasValue ? question.MaxScore.GetValueOrDefault().ToString("N2") : "???";
                            sb.Append($"\n\n===== {answer.Score:N2}/{maxStr} | Ответ из попытки с ID: {answer.AttemptId} ====\n\n");

                            sb.Append(response.Replace("\n\n", "\n"));
                        }

                        sb.Append("\n\nИзвлечено с помощью DiPeek: https://discord.gg/tphsh9vsty");

                        string text = sb.ToString();

                        DiscordMessageBuilder dmb = new DiscordMessageBuilder();

                        MemoryStream stream = new MemoryStream();
                        stream.Write(Encoding.UTF8.GetBytes(text));
                        stream.Seek(0, SeekOrigin.Begin);
                        dmb.WithFile($"question_{questionId}.txt", stream);
                        await e.Channel.SendMessageAsync(dmb);
                        await stream.DisposeAsync();
                        return;
                    }
                }


            }
			else if (e.MatchCommand("theme", "th"))
            {
                if (!e.HasNextArgument)
                {
                    await e.Respond("Используйте это для поиска тем. Иногда айди у тестов меняются, и приходится искать по названиям тем. Не у всех тем стоят какие-либо названия, так что результатов может быть мало.");
                    return;
                }
                if (e.MatchArgument("search", "s"))
                {
                    if (!e.HasNextArgument)
                    {
                        await e.Respond("Вы ничего не ввели. Введите запрос для поиска.");
                        return;
                    }
                    string term = e.NextArgument()!;
                    if (term.Length < 3)
                    {
                        await e.Respond("Слишком короткий запрос. Введите как минимум 3 символа.");
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

        public static string CleanString(string str)
        {
            RemoveFromString(ref str, "<div>", "</div>");
            RemoveFromString(ref str, "<p>", "</p>");
            RemoveFromString(ref str, "<span>", "</span>");
            RemoveFromString(ref str, "<em>", "</em>");
            RemoveFromString(ref str, "<strong>", "</strong>");
            RemoveFromString(ref str, "<br>", "<br/>");
            str = str.Replace("&nbsp;", " ");
            str = str.Replace("\n\n", "\n");
            return str.Trim();
        }
        private static void RemoveFromString(ref string str, params string[] remove)
        {
            foreach (string part in remove)
                str = str.Replace(part, string.Empty);
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
    }
}