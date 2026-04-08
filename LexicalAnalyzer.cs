using System;
using System.Collections.Generic;
using System.Text;

namespace WpfApp_3
{
    public class Token
    {
        public int Code { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public int Line { get; set; }
        public int StartPos { get; set; }
        public int EndPos { get; set; }
        public bool IsError { get; set; }
        public int ErrorLine { get; set; }
        public string ErrorMessage { get; set; }

        public string Location
        {
            get
            {
                if (IsError)
                {
                    return $"строка {ErrorLine}, позиция {StartPos}";
                }
                else
                {
                    return $"строка {Line}, {StartPos}-{EndPos}";
                }
            }
        }
    }

    public class LexicalAnalyzer
    {
        private const int CODE_STRING = 1;
        private const int CODE_NUMBER = 2;
        private const int CODE_IDENTIFIER = 3;
        private const int CODE_KEYWORD = 4;
        private const int CODE_ASSIGN = 5;
        private const int CODE_SEMICOLON = 6;
        private const int CODE_SPACE = 7;
        private const int CODE_PLUS = 8;
        private const int CODE_MINUS = 9;
        private const int CODE_SLASH = 10;
        private const int CODE_STAR = 11;
        private const int CODE_LPAREN = 12;
        private const int CODE_RPAREN = 13;
        private const int CODE_ERROR = 14;

        private readonly HashSet<string> keywords = new HashSet<string>
        {
            "String"
        };

        private bool IsValidSeparator(char c)
        {
            return c == ' ' || c == '\t' || c == '\n' || c == '\r' ||
                   c == '=' || c == ';' || c == '"' || c == '+' ||
                   c == '-' || c == '/' || c == '*' || c == '(' || c == ')';
        }

        private bool IsValidNumber(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            foreach (char c in s)
            {
                if (!char.IsDigit(c)) return false;
            }
            return true;
        }

        private bool IsValidIdentifier(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            if (!char.IsLetter(s[0]) && s[0] != '_') return false;
            for (int i = 1; i < s.Length; i++)
            {
                if (!char.IsLetterOrDigit(s[i]) && s[i] != '_') return false;
            }
            return true;
        }

        public List<Token> Analyze(string text)
        {
            var tokens = new List<Token>();
            int lineNumber = 1;
            int currentPos = 1;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (c == '\n')
                {
                    lineNumber++;
                    currentPos = 1;
                    continue;
                }

                if (c == '\r')
                {
                    currentPos++;
                    continue;
                }

                if (c == ' ' || c == '\t')
                {
                    tokens.Add(CreateToken(CODE_SPACE, "пробел", " ", lineNumber, currentPos));
                    currentPos++;
                    continue;
                }

                // Комментарии
                if (c == '/' && i + 1 < text.Length)
                {
                    char nextChar = text[i + 1];
                    if (nextChar == '/')
                    {
                        while (i < text.Length && text[i] != '\n') i++;
                        if (i < text.Length && text[i] == '\n')
                        {
                            lineNumber++;
                            currentPos = 1;
                        }
                        continue;
                    }
                    else if (nextChar == '*')
                    {
                        i += 2;
                        while (i + 1 < text.Length && !(text[i] == '*' && text[i + 1] == '/'))
                        {
                            if (text[i] == '\n')
                            {
                                lineNumber++;
                                currentPos = 1;
                            }
                            i++;
                        }
                        i += 2;
                        continue;
                    }
                }

                // Строковые константы
                if (c == '"')
                {
                    int startPosition = currentPos;
                    int startLine = lineNumber;
                    StringBuilder sb = new StringBuilder();
                    sb.Append(c);
                    i++;
                    currentPos++;

                    bool closed = false;

                    while (i < text.Length)
                    {
                        char currentChar = text[i];

                        if (currentChar == '"')
                        {
                            sb.Append(currentChar);
                            i++;
                            currentPos++;
                            closed = true;
                            break;
                        }
                        else if (currentChar == '\n')
                        {
                            break;
                        }
                        else if (currentChar == '\\')
                        {
                            sb.Append(currentChar);
                            i++;
                            currentPos++;
                            if (i < text.Length)
                            {
                                sb.Append(text[i]);
                                i++;
                                currentPos++;
                            }
                        }
                        else
                        {
                            sb.Append(currentChar);
                            i++;
                            currentPos++;
                        }
                    }

                    if (!closed)
                    {
                        tokens.Add(new Token
                        {
                            Code = CODE_ERROR,
                            Type = "ОШИБКА",
                            Value = sb.ToString(),
                            Line = startLine,
                            StartPos = startPosition,
                            EndPos = currentPos - 1,
                            IsError = true,
                            ErrorLine = startLine,
                            ErrorMessage = "Незакрытая строковая константа"
                        });
                    }
                    else
                    {
                        tokens.Add(new Token
                        {
                            Code = CODE_STRING,
                            Type = "строковая константа",
                            Value = sb.ToString(),
                            Line = startLine,
                            StartPos = startPosition,
                            EndPos = currentPos - 1
                        });
                    }

                    i--;
                    continue;
                }

                // Односимвольные операторы
                if (c == '=')
                {
                    tokens.Add(CreateToken(CODE_ASSIGN, "оператор присваивания", "=", lineNumber, currentPos));
                    currentPos++;
                    continue;
                }

                if (c == ';')
                {
                    tokens.Add(CreateToken(CODE_SEMICOLON, "конец оператора", ";", lineNumber, currentPos));
                    currentPos++;
                    continue;
                }

                if (c == '+')
                {
                    tokens.Add(CreateToken(CODE_PLUS, "оператор +", "+", lineNumber, currentPos));
                    currentPos++;
                    continue;
                }

                if (c == '-')
                {
                    tokens.Add(CreateToken(CODE_MINUS, "оператор -", "-", lineNumber, currentPos));
                    currentPos++;
                    continue;
                }

                if (c == '/')
                {
                    tokens.Add(CreateToken(CODE_SLASH, "оператор /", "/", lineNumber, currentPos));
                    currentPos++;
                    continue;
                }

                if (c == '*')
                {
                    tokens.Add(CreateToken(CODE_STAR, "оператор *", "*", lineNumber, currentPos));
                    currentPos++;
                    continue;
                }

                if (c == '(')
                {
                    tokens.Add(CreateToken(CODE_LPAREN, "открывающая скобка", "(", lineNumber, currentPos));
                    currentPos++;
                    continue;
                }

                if (c == ')')
                {
                    tokens.Add(CreateToken(CODE_RPAREN, "закрывающая скобка", ")", lineNumber, currentPos));
                    currentPos++;
                    continue;
                }

                // Обработка последовательностей символов (идентификаторы, числа, ошибки)
                int seqStartPos = currentPos;
                int seqStartLine = lineNumber;
                StringBuilder sequence = new StringBuilder();

                while (i < text.Length)
                {
                    char currentChar = text[i];
                    if (IsValidSeparator(currentChar))
                    {
                        break;
                    }
                    sequence.Append(currentChar);
                    i++;
                    currentPos++;
                }

                string seqValue = sequence.ToString();

                bool isKeyword = keywords.Contains(seqValue);
                bool isNumber = IsValidNumber(seqValue);
                bool isIdentifier = IsValidIdentifier(seqValue);

                if (isKeyword)
                {
                    tokens.Add(CreateToken(CODE_KEYWORD, "ключевое слово", seqValue, seqStartLine, seqStartPos));
                }
                else if (isNumber)
                {
                    tokens.Add(CreateToken(CODE_NUMBER, "целое без знака", seqValue, seqStartLine, seqStartPos));
                }
                else if (isIdentifier)
                {
                    tokens.Add(CreateToken(CODE_IDENTIFIER, "идентификатор", seqValue, seqStartLine, seqStartPos));
                }
                else
                {
                    tokens.Add(new Token
                    {
                        Code = CODE_ERROR,
                        Type = "ОШИБКА",
                        Value = seqValue,
                        Line = seqStartLine,
                        StartPos = seqStartPos,
                        EndPos = currentPos - 1,
                        IsError = true,
                        ErrorLine = seqStartLine,
                        ErrorMessage = $"Недопустимые символы: {seqValue}"
                    });
                }

                i--;
            }

            return tokens;
        }

        private Token CreateToken(int code, string type, string value, int line, int pos)
        {
            return new Token
            {
                Code = code,
                Type = type,
                Value = value,
                Line = line,
                StartPos = pos,
                EndPos = pos + value.Length - 1
            };
        }
    }
}