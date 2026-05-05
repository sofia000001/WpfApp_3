using System;
using System.Collections.Generic;

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
            ParseStringDeclaration();

            return errors;
        }

        private void SkipSpaces()
        {
            while (currentPos < tokens.Count && tokens[currentPos].Code == CODE_SPACE)
            {
                currentPos++;
            }

            currentToken = (currentPos < tokens.Count) ? tokens[currentPos] : null;
        }

        private void NextToken()
        {
            currentPos++;
            SkipSpaces();
        }

        /// <summary>
        /// Автоматный разбор конструкции:
        /// String id = "text";
        /// </summary>
        private void ParseStringDeclaration()
        {
            int state = 0;

            while (currentToken != null)
            {
                switch (state)
                {
                    case 0: 

                        if (currentToken.Code == CODE_ERROR)
                        {
                            AddError(currentToken.Value, currentToken.Line, currentToken.StartPos,
                                "Недопустимые символы, ожидалось ключевое слово 'String'");

                            int line = currentToken.Line;
                            int pos = currentToken.StartPos + currentToken.Value.Length;

                            NextToken();

                           
                            AddError("(пропущено)", line, pos, "Ожидалось ключевое слово 'String'");
                            AddError("(пропущено)", line, pos, "Ожидался идентификатор");

                            state = 2; 
                            break;
                        }

                        if (currentToken.Code == CODE_SEMICOLON)
                        {
                            AddError(currentToken.Value, currentToken.Line, currentToken.StartPos,
                                "Ожидалось ключевое слово 'String'");
                            return;
                        }

                        if (currentToken.Code == CODE_KEYWORD && currentToken.Value == "String")
                        {
                            state = 1;
                            NextToken();
                        }
                        else
                        {
                            AddError(currentToken.Value, currentToken.Line, currentToken.StartPos,
                                "Ожидалось ключевое слово 'String'");
                            NextToken();
                        }
                        break;

                    case 1: 

                        if (currentToken.Code == CODE_IDENTIFIER)
                        {
                            state = 2;
                            NextToken();
                        }
                        else
                        {
                            AddError(currentToken?.Value ?? "(конец строки)",
                                currentToken?.Line ?? 1,
                                currentToken?.StartPos ?? 1,
                                "Ожидался идентификатор");
                            NextToken();
                        }
                        break;

                    case 2: 

                        if (currentToken == null)
                        {
                            AddError("(конец строки)", 1, 1,
                                "Незавершённая конструкция, ожидалось ;");
                            return;
                        }

                        if (currentToken.Code == CODE_ASSIGN)
                        {
                            state = 3;
                            NextToken();
                        }
                        else
                        {
                            AddError(currentToken.Value, currentToken.Line, currentToken.StartPos,
                                "Ожидался оператор '='");
                            NextToken();
                        }
                        break;

                    case 3: 

                        if (currentToken == null)
                        {
                            AddError("(конец строки)", 1, 1,
                                "Незавершённая конструкция, ожидалось ;");
                            return;
                        }

                        if (currentToken.Code == CODE_STRING)
                        {
                            if (!currentToken.Value.EndsWith("\""))
                            {
                                AddError(currentToken.Value, currentToken.Line, currentToken.StartPos,
                                    "Незакрытая строковая константа");
                            }

                            state = 4;
                            NextToken();
                        }
                        else if (currentToken.Code == CODE_ERROR &&
                                 currentToken.ErrorMessage?.Contains("строковая") == true)
                        {
                            AddError(currentToken.Value, currentToken.Line, currentToken.StartPos,
                                "Незакрытая строковая константа");

                            state = 4;
                            NextToken();
                        }
                        else
                        {
                            AddError(currentToken.Value, currentToken.Line, currentToken.StartPos,
                                "Ожидалась строковая константа");
                            NextToken();
                        }
                        break;

                    case 4: 

                        if (currentToken == null)
                        {
                            AddError("(конец строки)", 1, 1,
                                "Незавершённая конструкция, ожидалось ;");
                            return;
                        }

                        if (currentToken.Code == CODE_SEMICOLON)
                        {
                            return; 
                        }
                        else
                        {
                            AddError(currentToken.Value, currentToken.Line, currentToken.StartPos,
                                "Ожидалась точка с запятой ';'");
                            NextToken();
                        }
                        break;
                }
            }

         
            AddError("(конец строки)", 1, 1,
                "Незавершённая конструкция, ожидалось ;");
        }

        private void AddError(string fragment, int line, int position, string description)
        {
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
