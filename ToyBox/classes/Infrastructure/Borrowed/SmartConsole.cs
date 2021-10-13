using ModKit;
using System;
using System.IO;
using System.Media;

namespace ToyBox {
    public static class SmartConsoleCommands {
        public static void Register() {
            SmartConsole.RegisterCommand("beep", "", "Plays the 'beep' system sound.", new SmartConsole.ConsoleCommandFunction(Beep));
            SmartConsole.RegisterCommand("bat", "bat fileName", "Executes commands from a file in the Bag of Tricks folder.", new SmartConsole.ConsoleCommandFunction(CommandBatch));

        }

        public static void Beep(string parameters) => SystemSounds.Beep.Play();

        public static void CommandBatch(string parameters) {
            parameters = parameters.Remove(0, 4);
            if (File.Exists(Mod.modEntryPath + parameters)) {
                try {
                    var i = 0;
                    var commands = File.ReadAllLines(Mod.modEntryPath + parameters);
                    foreach (var s in commands) {
                        SmartConsole.WriteLine($"[{i}]: {s}");
                        SmartConsole.ExecuteLine(s);
                        i++;
                    }
                }
                catch (Exception e) {
                    Mod.Error(e);
                }
            }
            else {
                SmartConsole.WriteLine($"'{parameters}' Not Found");
            }
        }
    }
}
