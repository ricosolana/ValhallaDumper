using Jotunn.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValhallaDumper
{
    public class AvlDumpCommand : ConsoleCommand
    {
        public override string Name => "avl_dump";

        public override string Help => "dump data for use with Avledet server";

        public override void Run(string[] args)
        {
            // dump all

            if (args.Length == 0)
            {
                Chat.print("This will lagg your game a lot during dumping");
                Chat.print("Dumping will also destroy the current world.");
                Chat.print("USE WITH CAUTION!");

                Chat.print("run this command again with 'confirm'");
                return;
            }

            Chat.print("Dumping...");

            Dumper.DumpDocs();
            Dumper.DumpPackages();
        }
    }
}
