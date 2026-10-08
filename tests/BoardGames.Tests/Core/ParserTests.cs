using BoardGames.Core;

namespace BoardGames.Tests.Core;

public class ParserTests
{
    private readonly StandardInputParser _parser = new();

    [Theory]
    [InlineData("O5:3", "O", 5, 3)]
    [InlineData("H3:3", "H", 3, 3)]
    [InlineData("e3:3", "E", 3, 3)]           // lower case
    [InlineData("  p 3 : 4  ", "P", 3, 4)]    // extra spaces
    public void Moves_parse_to_a_PlaceInput(string text, string code, int row, int col)
    {
        Assert.Equal(new PlaceInput(code, new Position(row, col)), _parser.Parse(text, out string error));
        Assert.Equal("", error);
    }

    [Theory]
    [InlineData("PASS")]
    [InlineData("pass")]
    public void PASS_parses_to_a_PassInput_not_a_P_move(string text)
    {
        Assert.IsType<PassInput>(_parser.Parse(text, out _));
    }

    [Theory]
    [InlineData("undo", ControlKind.Undo, null)]
    [InlineData("REDO", ControlKind.Redo, null)]
    [InlineData("help", ControlKind.Help, null)]
    [InlineData("quit", ControlKind.Quit, null)]
    [InlineData("save x.json", ControlKind.Save, "x.json")]
    [InlineData("load  my game.json", ControlKind.Load, "my game.json")]
    public void Commands_parse_to_a_ControlInput(string text, ControlKind kind, string? file)
    {
        Assert.Equal(new ControlInput(kind, file), _parser.Parse(text, out _));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("garbage")]
    [InlineData("O5")]               // missing colon and column
    [InlineData("O0:3")]             // rows start at 1
    [InlineData("O5:0")]
    [InlineData("P")]                // a bare P is not PASS
    [InlineData("undo now")]
    [InlineData("save")]             // file name missing
    [InlineData("O99999999999:3")]   // too big for an int
    public void Bad_text_gives_a_message_and_never_throws(string? text)
    {
        ParsedInput? input = _parser.Parse(text!, out string error);

        Assert.Null(input);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }
}
