using BoardGames.App;
using BoardGames.Core;

var catalog = CatalogSetup.Build();

// Arguments given = CLI test mode (no menu). No arguments = the interactive menu.
if (args.Length > 0)
    return CliRunner.Run(args, catalog, Console.Out);

// TODO B: pass `new JsonGameRepository()` as the third argument so save and load work.
new MainMenu(catalog, new ConsoleGameIO()).Run();
return 0;
