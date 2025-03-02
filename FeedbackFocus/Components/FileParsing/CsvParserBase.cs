using Microsoft.VisualBasic.FileIO;

namespace FeedbackFocus.Components.FileParsing
{
    public abstract class CsvParserBase
    {
        protected char Separator { get; private set; }

        public CsvParserBase(char separator)
        {
            Separator = separator;
        }

        public async Task<List<string[]>> ParseFile(string fileContent)
        {
            List<string[]> parsedRows = new();
            TextFieldParser parser = new TextFieldParser(new StringReader(fileContent));
            parser.HasFieldsEnclosedInQuotes = true;
            parser.SetDelimiters(Separator.ToString());

            while (!parser.EndOfData)
            {
                parsedRows.Add(parser.ReadFields());
            }
            parser.Close();

            return parsedRows;
        }

        public abstract Task ProcessRows(List<string[]> rows);
    }
}
