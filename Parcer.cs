using System;
using System.Collections.Generic;
using WpfApp_3;

namespace WpfApp_3
{
    public class SyntaxError
    {
        public string Fragment { get; set; }
        public int Line { get; set; }
        public int Position { get; set; }
        public string Description { get; set; }
        public string Location => $"строка {Line}, позиция {Position}";

        public SyntaxError(string fragment, int line, int position, string description)
        {
            Fragment = fragment;
            Line = line;
            Position = position;
            Description = description;
        }
    }

    public class Parser
    {
        private List<Token> tokens;
        private int currentPos;
        private List<SyntaxError> errors;
        private Token currentToken;

        private const int CODE_STRING = 1;
        private const int CODE_NUMBER = 2;
        private const int CODE_IDENTIFIER = 3;
        private const int CODE_KEYWORD = 4;
        private const int CODE_ASSIGN = 5;
        private const int CODE_SEMICOLON = 6;
        private const int CODE_SPACE = 7;
        private const int CODE_ERROR = 14;

        public Parser()
        {
            errors = new List<SyntaxError>();
        }

        public List<SyntaxError> Parse(List<Token> tokens)
        {
            this.tokens = tokens;
            this.currentPos = 0;
            this.errors = new List<SyntaxError>();

            if (tokens == null || tokens.Count == 0)
            {
                AddError("(пустая строка)", 1, 1, "Пустая строка");
                return errors;
            }

            SkipSpaces();

            try
            {
                ParseStringDeclaration();
            }
            catch (Exception ex)
            {
                AddError(currentToken?.Value ?? "конец строки",
                    currentToken?.Line ?? 1,
                    currentToken?.StartPos ?? 1,
                    $"Ошибка: {ex.Message}");
            }

            return errors;
        }

        private void SkipSpaces()
        {
            while (currentPos < tokens.Count)
            {
                currentToken = tokens[currentPos];
                if (currentToken.Code == CODE_SPACE)
                {
                    currentPos++;
                }
                else
                {
                    break;
                }
            }

            if (currentPos >= tokens.Count)
            {
                currentToken = null;
            }
            else
            {
                currentToken = tokens[currentPos];
            }
        }

        private void NextToken()
        {
            currentPos++;
            SkipSpaces();
        }

        private void ParseStringDeclaration()
        {
            // 1. Проверка ключевого слова "String"
            if (currentToken == null)
            {
                AddError("(конец файла)", 1, 1, "Ожидалось ключевое слово 'String'");
                return;
            }

            // Если встретили ошибку (недопустимые символы)
            if (currentToken.Code == CODE_ERROR)
            {
                AddError(currentToken.Value, currentToken.Line, currentToken.StartPos,
                    currentToken.ErrorMessage ?? "Недопустимые символы");
                NextToken();
                // После пропуска недопустимых символов, продолжаем анализ
                ParseAfterError();
                return;
            }

            if (currentToken.Code != CODE_KEYWORD || currentToken.Value != "String")
            {
                AddError(currentToken.Value, currentToken.Line, currentToken.StartPos,
                    "Ожидалось ключевое слово 'String'");
                SkipToNextString();
                return;
            }

            NextToken();

            // 2. Проверка идентификатора
            if (currentToken == null)
            {
                AddError("(конец файла)", 1, 1, "Ожидался идентификатор после 'String'");
                return;
            }

            if (currentToken.Code == CODE_ERROR)
            {
                AddError(currentToken.Value, currentToken.Line, currentToken.StartPos,
                    currentToken.ErrorMessage ?? "Недопустимые символы в идентификаторе");
                NextToken();
            }

            if (currentToken.Code != CODE_IDENTIFIER)
            {
                AddError(currentToken?.Value ?? "(конец файла)",
                    currentToken?.Line ?? 1,
                    currentToken?.StartPos ?? 1,
                    "Ожидался идентификатор");
                SkipToAssign();
                return;
            }

            NextToken();

            // 3. Проверка оператора =
            if (currentToken == null)
            {
                AddError("(конец файла)", 1, 1, "Ожидался оператор '='");
                return;
            }

            if (currentToken.Code == CODE_ERROR)
            {
                AddError(currentToken.Value, currentToken.Line, currentToken.StartPos,
                    currentToken.ErrorMessage ?? "Недопустимые символы перед '='");
                NextToken();
            }

            if (currentToken.Code != CODE_ASSIGN)
            {
                AddError(currentToken?.Value ?? "(конец файла)",
                    currentToken?.Line ?? 1,
                    currentToken?.StartPos ?? 1,
                    "Ожидался оператор присваивания '='");
                SkipToString();
                return;
            }

            NextToken();

            // 4. Проверка строковой константы
            if (currentToken == null)
            {
                AddError("(конец файла)", 1, 1, "Ожидалась строковая константа");
                return;
            }

            bool isUnclosedString = false;

            if (currentToken.Code == CODE_STRING)
            {
                if (!currentToken.Value.EndsWith("\""))
                {
                    isUnclosedString = true;
                    AddError(currentToken.Value, currentToken.Line, currentToken.StartPos,
                        "Незакрытая строковая константа");
                }
                NextToken();
            }
            else if (currentToken.Code == CODE_ERROR && currentToken.ErrorMessage?.Contains("строковая") == true)
            {
                isUnclosedString = true;
                AddError(currentToken.Value, currentToken.Line, currentToken.StartPos,
                    "Незакрытая строковая константа");
                NextToken();
            }
            else
            {
                AddError(currentToken.Value, currentToken.Line, currentToken.StartPos,
                    "Ожидалась строковая константа в двойных кавычках");
                SkipToSemicolon();
                return;
            }

            // 5. Проверка точки с запятой
            CheckSemicolon();
        }

        /// <summary>
        /// Обработка ситуации после ошибки - продолжаем анализ
        /// </summary>
        private void ParseAfterError()
        {
            // Ищем ключевое слово String
            while (currentToken != null)
            {
                if (currentToken.Code == CODE_KEYWORD && currentToken.Value == "String")
                {
                    ParseStringDeclaration();
                    return;
                }
                // Ищем оператор = для проверки выражения
                if (currentToken.Code == CODE_ASSIGN)
                {
                    NextToken();
                    // Ищем строковую константу
                    if (currentToken != null && (currentToken.Code == CODE_STRING ||
                        (currentToken.Code == CODE_ERROR && currentToken.ErrorMessage?.Contains("строковая") == true)))
                    {
                        // Добавляем ошибку о незакрытой строке, если нужно
                        if (currentToken.Code == CODE_STRING && !currentToken.Value.EndsWith("\""))
                        {
                            AddError(currentToken.Value, currentToken.Line, currentToken.StartPos,
                                "Незакрытая строковая константа");
                        }
                        else if (currentToken.Code == CODE_ERROR && currentToken.ErrorMessage?.Contains("строковая") == true)
                        {
                            AddError(currentToken.Value, currentToken.Line, currentToken.StartPos,
                                "Незакрытая строковая константа");
                        }
                        NextToken();

                        // Проверяем точку с запятой
                        CheckSemicolon();
                        return;
                    }
                    else if (currentToken != null)
                    {
                        AddError(currentToken.Value, currentToken.Line, currentToken.StartPos,
                            "Ожидалась строковая константа");
                        CheckSemicolon();
                        return;
                    }
                    break;
                }
                NextToken();
            }

            CheckSemicolon();
        }

        /// <summary>
        /// Проверка наличия точки с запятой в конце
        /// </summary>
        private void CheckSemicolon()
        {
            if (currentToken == null)
            {
                AddError("(конец строки)", 1, 1, "Ожидалась точка с запятой ';' в конце оператора");
                return;
            }

            if (currentToken.Code == CODE_SEMICOLON)
            {
                NextToken();
            }
            else
            {
                AddError(currentToken.Value, currentToken.Line, currentToken.StartPos,
                    "Ожидалась точка с запятой ';' в конце оператора");
            }
        }

        private void SkipToNextString()
        {
            while (currentToken != null)
            {
                if (currentToken.Code == CODE_KEYWORD && currentToken.Value == "String")
                    break;
                if (currentToken.Code == CODE_ERROR)
                {
                    AddError(currentToken.Value, currentToken.Line, currentToken.StartPos,
                        currentToken.ErrorMessage ?? "Недопустимые символы, пропущены");
                }
                NextToken();
            }

            CheckSemicolon();
        }

        private void SkipToAssign()
        {
            while (currentToken != null && currentToken.Code != CODE_ASSIGN)
            {
                if (currentToken.Code == CODE_ERROR)
                {
                    AddError(currentToken.Value, currentToken.Line, currentToken.StartPos,
                        currentToken.ErrorMessage ?? "Недопустимые символы, пропущены");
                }
                NextToken();
            }
        }

        private void SkipToString()
        {
            while (currentToken != null && currentToken.Code != CODE_STRING &&
                   !(currentToken.Code == CODE_ERROR && currentToken.ErrorMessage?.Contains("строковая") == true))
            {
                if (currentToken.Code == CODE_ERROR && !currentToken.ErrorMessage.Contains("строковая"))
                {
                    AddError(currentToken.Value, currentToken.Line, currentToken.StartPos,
                        currentToken.ErrorMessage ?? "Недопустимые символы, пропущены");
                }
                NextToken();
            }
        }

        private void SkipToSemicolon()
        {
            while (currentToken != null && currentToken.Code != CODE_SEMICOLON)
            {
                if (currentToken.Code == CODE_ERROR)
                {
                    AddError(currentToken.Value, currentToken.Line, currentToken.StartPos,
                        currentToken.ErrorMessage ?? "Недопустимые символы, пропущены");
                }
                NextToken();
            }
            if (currentToken != null && currentToken.Code == CODE_SEMICOLON)
            {
                NextToken();
            }
        }

        private void AddError(string fragment, int line, int position, string description)
        {
            // Проверяем на дублирование
            foreach (var err in errors)
            {
                if (err.Line == line && err.Position == position && err.Description == description)
                    return;
            }
            errors.Add(new SyntaxError(fragment, line, position, description));
        }

        public int ErrorCount => errors.Count;
    }
}