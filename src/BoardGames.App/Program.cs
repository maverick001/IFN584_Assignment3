using BoardGames.App;
using BoardGames.Core;
using BoardGames.Persistence;

var catalog = CatalogSetup.Build();

// Arguments given = CLI test mode (no menu). No arguments = the interactive menu.
if (args.Length > 0)
    return CliRunner.Run(args, catalog, Console.Out);

new MainMenu(catalog, new ConsoleGameIO(), new JsonGameRepository()).Run();
return 0;
